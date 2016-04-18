using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AIEntity_Plant : AIEntity
{
	//Grow State Vars
	public float totalGrowDays = 3f;
	public float finalGrowSizeMin = 25f;
	public float finalGrowSizeMax = 55f;
	public float lifeSpan = 20f;
	public float totalShrinkTimeSeconds = 10f;
	public bool scaleFromBottom = true; //rather than scale from the middle, grow 'up' rather than 'out'
	public bool finishedGrowing = false;
	internal Renderer rend;

	private float totalGrowTime;
	private float currentGrowTime;
	private float growFinalSize;
	private Vector3 initialHeightScale;
	private Vector3 finalHeightScale;

	[SyncVar] public Vector3 scale;

	// Use this for initialization
	override public void Start ()
	{
		base.Start ();
		//!isServer, because when we test, the client is a Host
		if(isServer) {
			growFinalSize = Random.Range (finalGrowSizeMin, finalGrowSizeMax);
			rend = GetComponent<Renderer> ();
			if (!rend) {
				//	Hopefully this plant actually has a mesh to render
			}
			//calculate height at beginning for scaling to final scale
			float currentHeight = rend.bounds.size.y;
			float initialHeight = growFinalSize / (2 * totalGrowDays);
			initialHeightScale = new Vector3 (1, initialHeight / currentHeight, 1);
			finalHeightScale = new Vector3 (1, growFinalSize, 1);
			this.transform.localScale = initialHeightScale;
			currentGrowTime = 0f;
			totalGrowTime = dayclock.DaysToSeconds (totalGrowDays);
			scale = this.transform.localScale;
		}
	
	}
	
	// Update is called once per frame
	override public void Update ()
	{
		base.Update ();

		this.transform.localScale = scale;
	}

	//returns true if still growing, else false
	public bool growLerp() {
		currentGrowTime += Time.deltaTime;
		if (currentGrowTime > totalGrowTime) {
			currentGrowTime = totalGrowTime;
			return false;
		}

		float lerpProgress = currentGrowTime / totalGrowTime;
		scale = Vector3.Lerp (initialHeightScale, finalHeightScale, lerpProgress);
		return true;
	}

	override public bool isDead() {
		return lifeSpan < dayclock.secondsToDays (Time.time - spawnTime);
	}
}

