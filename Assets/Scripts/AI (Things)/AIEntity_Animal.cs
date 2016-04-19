using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AIEntity_Animal : AIEntity
{
	//'sizes' are in Unity meters.  Start method scales the mesh to correct scale.
	public float initialSize = 1f;
	public float midSize = 2f;
	public float finalSizeMin = 3f;
	public float finalSizeMax = 3.5f;
	public float numDaysWithoutFood = 1f;
	public float damageWithoutFood = 30f;
	public float lifeSpanInDays = 100f;
	public Renderer modelRenderer;

	internal float lastTimeEaten;
	internal float finalSize;
	internal Vector3 finalScale;
	internal Vector3 midScale;
	internal Vector3 initialHeightScale;

	[SyncVar] public Vector3 scale;


	override public void Start ()
	{
		base.Start ();
		if (isServer) {
			finalSize = Random.Range (finalSizeMin, finalSizeMax);
//			modelRenderer = GetComponent<Renderer> ();
			if (!modelRenderer) {
				//no renderer, hmm
			} else {
				float currentSize = modelRenderer.bounds.size.y;
				float initialScale = initialSize / currentSize;
				initialHeightScale = transform.localScale * initialScale;
				midScale = Vector3.one * midSize;
				finalScale = Vector3.one * finalSize;
				this.transform.localScale = initialHeightScale;
				scale = this.transform.localScale;
			}
		}
	}
	
	// Update is called once per frame
	override public void Update ()
	{
		base.Update ();
		this.transform.localScale = scale;
	}

	virtual public void eat(GameObject obj) {
		lastTimeEaten = Time.time;
		Destroy (obj);
	}

	virtual public void Grow(string size) {
		if (size == "mid") {
			scale = midScale;
		} else if (size == "final") {
			scale = finalScale;
		} else {
			Debug.Log ("Not a size");
		}
	}
}

