using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Herbivore_001 : AIEntity
{
	public float initialSize = 1f;
	public float midSize = 2f;
	public float finalSizeMin = 3f;
	public float finalSizeMax = 3.5f;

	private float finalSize;
	private Vector3 finalScale;
	private Vector3 midScale;
	private Vector3 initialHeightScale;
	private Renderer rend;

	[SyncVar] public Vector3 scale;

	// Use this for initialization
	override public void Start ()
	{
		base.Start ();
		if (isServer) {
			dayclock = (DayClock) FindObjectOfType (typeof(DayClock));
			finalSize = Random.Range (finalSizeMin, finalSizeMax);
			rend = GetComponent<Renderer> ();
			if (!rend) {
				//no renderer, hmm
			} else {
				float currentSize = rend.bounds.size.y;
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

	public void eat(GameObject nodule) {
		this.doDamage (-30);
		Destroy (nodule);
	}

	public void Grow(string size) {
		if (size == "mid") {
			scale = midScale;
		} else if (size == "final") {
			scale = finalScale;
		}
	}
}

