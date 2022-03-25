using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detector : MonoBehaviour
{

    [Header( "Runtime" )]
    [ReadOnly] public float propertyValue;

	[HideInInspector] public List<OxygenSource> oxygenSources;

	private void Update()
	{
		oxygenLevels = 0f;

		for ( int i = 0; i < oxygenSources.Count; i++ )
		{
			float dist = Vector3.Distance( oxygenSources[ i ].transform.position, transform.position );
			float perc = dist / oxygenSources[ i ].sphereCollider.bounds.size.y;
			float oxygenLevel = oxygenSources[ i ].oxygenAtCentre * oxygenSources[ i ].oxygenFallOff.Evaluate( perc );
			oxygenLevels += oxygenLevel;
		}
	}

}
