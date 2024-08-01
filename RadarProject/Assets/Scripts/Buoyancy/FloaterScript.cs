using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class FloaterScript : MonoBehaviour {

    [SerializeField] List<Transform> floaters;
    [SerializeField] new Rigidbody rigidbody;
    [SerializeField] float depthBeforeSubmerged = 1f;
    [SerializeField] float displacementAmount = 3f;
    [SerializeField] float waterDrag = 0.99f;
    [SerializeField] float waterAngularDrag = 0.5f;
    [SerializeField] WaterSurface water;

    private void FixedUpdate() {

		if (water == null) return;
		
        for (int i = 0; i < floaters.Count; i++) {
			
            Vector3 gravity = Physics.gravity / floaters.Count;
            rigidbody.AddForceAtPosition(gravity, floaters[i].position, ForceMode.Acceleration);

            WaterSearchParameters searchParams = new()
            {
                startPosition = floaters[i].position
            };

            water.FindWaterSurfaceHeight(searchParams, out WaterSearchResult searchResult);

            if (floaters[i].position.y < searchResult.height) {
                float displacementMultiplier = Mathf.Clamp01(searchResult.height - floaters[i].position.y / depthBeforeSubmerged) * displacementAmount;

                // Apply buoyancy force
                Vector3 buoyancyForce = new(0f, Mathf.Abs(Physics.gravity.y) * displacementMultiplier, 0f);
                rigidbody.AddForceAtPosition(buoyancyForce, floaters[i].position, ForceMode.Acceleration);

                // Apply drag and angular drag
                rigidbody.AddForce(-rigidbody.velocity * waterDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
                rigidbody.AddTorque(-rigidbody.angularVelocity * waterAngularDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }
        }
    }
}
