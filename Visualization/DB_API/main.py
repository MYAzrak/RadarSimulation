#pip install fastapi uvicorn sqlalchemy psycopg2-binary
from fastapi import FastAPI, HTTPException, Depends
from sqlalchemy import create_engine, Column, Integer, String, Float, DateTime, ForeignKey, JSON
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, Session
from datetime import datetime
from typing import List, Optional
from pydantic import BaseModel, validator
import uvicorn
from datetime import timedelta

# Database Configuration
DATABASE_URL = "postgresql://postgres:postgres@localhost/radar_tracking"
engine = create_engine(DATABASE_URL)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()

# SQLAlchemy Models
class Radar(Base):
    __tablename__ = "radars"
    
    radar_id = Column(Integer, primary_key=True, index=True)
    latitude = Column(Float)
    longitude = Column(Float)
    range_km = Column(Float)
    azimuth_resolution = Column(Float)  # Changed from azimuth_coverage
    last_active = Column(DateTime, default=datetime.utcnow)
    
    @property
    def number_of_angles(self):
        """Calculate number of angles based on resolution"""
        return int(360 / self.azimuth_resolution)

class Detection(Base):
    __tablename__ = "detections"
    
    detection_id = Column(Integer, primary_key=True, index=True)
    radar_id = Column(Integer, ForeignKey("radars.radar_id"))
    latitude = Column(Float)
    longitude = Column(Float)
    confidence = Column(Float)
    detection_time = Column(DateTime, default=datetime.utcnow)
    vessel_type = Column(String, nullable=True)

# Pydantic Models for Request/Response
class RadarBase(BaseModel):
    latitude: float
    longitude: float
    range_km: float
    azimuth_resolution: float  # Changed from azimuth_coverage

    @validator('azimuth_resolution')
    def validate_azimuth_resolution(cls, v):
        if v <= 0:
            raise ValueError('Azimuth resolution must be positive')
        if 360 % v != 0:
            raise ValueError('360 must be divisible by azimuth resolution')
        return v

    @property
    def number_of_angles(self) -> int:
        return int(360 / self.azimuth_resolution)

class RadarCreate(RadarBase):
    radar_id: int

class RadarResponse(RadarBase):
    radar_id: int
    last_active: datetime
    number_of_angles: int
    
    class Config:
        orm_mode = True

class DetectionBase(BaseModel):
    radar_id: int
    latitude: float
    longitude: float
    confidence: float
    vessel_type: Optional[str] = None
    raw_ml_output: Optional[dict] = None

class DetectionCreate(DetectionBase):
    pass

class DetectionResponse(DetectionBase):
    detection_id: int
    detection_time: datetime
    
    class Config:
        orm_mode = True

class RadarLocationUpdate(BaseModel):
    latitude: float
    longitude: float
    range_km: float 
    azimuth_resolution: float

    @validator('latitude')
    def validate_latitude(cls, v):
        if v < -90 or v > 90:
            raise ValueError('Latitude must be between -90 and 90 degrees')
        return v

    @validator('longitude')
    def validate_longitude(cls, v):
        if v < -180 or v > 180:
            raise ValueError('Longitude must be between -180 and 180 degrees')
        return v


# Database Dependency
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

# Initialize FastAPI app
app = FastAPI(title="Radar Tracking System API")

# API Endpoints
@app.post("/radars/", response_model=RadarResponse)
def create_radar(radar: RadarCreate, db: Session = Depends(get_db)):
    # Check if radar with same ID exists
    existing_radar = db.query(Radar).filter(Radar.radar_id == radar.radar_id).first()
    
    if existing_radar:
        # Delete all detections associated with this radar
        db.query(Detection).filter(Detection.radar_id == existing_radar.radar_id).delete()
        
        # Update existing radar with new data
        for key, value in radar.dict().items():
            setattr(existing_radar, key, value)
        db_radar = existing_radar
    else:
        # Create new radar
        db_radar = Radar(**radar.dict())
    
    db.add(db_radar)
    db.commit()
    db.refresh(db_radar)
    return db_radar

@app.get("/radars/", response_model=List[RadarResponse])
def get_radars(db: Session = Depends(get_db)):
    return db.query(Radar).all()

@app.patch("/radars/{radar_id}/location", response_model=RadarResponse)
def update_radar_location(
    radar_id: int, 
    location: RadarLocationUpdate, 
    db: Session = Depends(get_db)
):
    # Query the radar
    radar = db.query(Radar).filter(Radar.radar_id == radar_id).first()
    if not radar:
        raise HTTPException(status_code=404, detail="Radar not found")
    
    # Update location
    radar.latitude = location.latitude
    radar.longitude = location.longitude
    radar.azimuth_resolution = location.azimuth_resolution
    radar.range_km = location.range_km
    
    # Update last_active timestamp
    radar.last_active = datetime.utcnow()
    
    # Commit changes
    db.commit()
    db.refresh(radar)
    
    return radar


@app.get("/radars/{radar_id}", response_model=RadarResponse)
def get_radar(radar_id: int, db: Session = Depends(get_db)):
    radar = db.query(Radar).filter(Radar.radar_id == radar_id).first()
    if not radar:
        raise HTTPException(status_code=404, detail="Radar not found")
    return radar

@app.put("/radars/{radar_id}/last-active", response_model=RadarResponse)
def update_radar_last_active(radar_id: int, db: Session = Depends(get_db)):
    radar = db.query(Radar).filter(Radar.radar_id == radar_id).first()
    if not radar:
        raise HTTPException(status_code=404, detail="Radar not found")
    radar.last_active = datetime.utcnow()
    db.commit()
    db.refresh(radar)
    return radar

@app.post("/detections/", response_model=DetectionResponse)
def create_detection(detection: DetectionCreate, db: Session = Depends(get_db)):
    # Verify radar exists
    radar = db.query(Radar).filter(Radar.radar_id == detection.radar_id).first()
    if not radar:
        raise HTTPException(status_code=404, detail="Radar not found")
    
    db_detection = Detection(**detection.dict())
    db.add(db_detection)
    db.commit()
    db.refresh(db_detection)
    return db_detection

@app.get("/detections/recent/", response_model=List[DetectionResponse])
def get_recent_detections(
    minutes: int = 30,
    radar_id: Optional[int] = None,
    db: Session = Depends(get_db)
):
    query = db.query(Detection)
    if radar_id:
        query = query.filter(Detection.radar_id == radar_id)
    
    cutoff_time = datetime.utcnow() - timedelta(minutes=minutes)
    query = query.filter(Detection.detection_time >= cutoff_time)
    return query.all()

@app.get("/detections/by_area/", response_model=List[DetectionResponse])
def get_detections_by_area(
    min_lat: float,
    max_lat: float,
    min_lon: float,
    max_lon: float,
    minutes: int = 30,
    db: Session = Depends(get_db)
):
    cutoff_time = datetime.utcnow() - timedelta(minutes=minutes)
    return db.query(Detection).filter(
        Detection.latitude >= min_lat,
        Detection.latitude <= max_lat,
        Detection.longitude >= min_lon,
        Detection.longitude <= max_lon,
        Detection.detection_time >= cutoff_time
    ).all()

# Create database tables
def init_db():
    Base.metadata.create_all(bind=engine)

if __name__ == "__main__":
    # Initialize database
    init_db()
    
    # Start the API server
    uvicorn.run(app, host="0.0.0.0", port=7777)
