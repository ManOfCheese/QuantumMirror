using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Source : MonoBehaviour
{

	public Property sourceOf;
	public float valueAtCentre;
	public AnimationCurve fallOff;

	[HideInInspector] public SphereCollider sphereCollider;

	private void Awake()
	{
		sphereCollider = GetComponent<SphereCollider>();
	}

	private void OnTriggerEnter( Collider other )
	{
		if ( other.gameObject.GetComponentInChildren<Detector>() )
		{
			if ( sourceOf == other.gameObject.GetComponentInChildren<Detector>().propertyToDetect )
				other.gameObject.GetComponentInChildren<Detector>().sources.Add( this );
		}
	}

	private void OnTriggerExit( Collider other )
	{
		if ( other.gameObject.GetComponentInChildren<Detector>() )
		{
			if ( sourceOf == other.gameObject.GetComponentInChildren<Detector>().propertyToDetect )
				other.gameObject.GetComponentInChildren<Detector>().sources.Remove( this );
		}
	}
}
