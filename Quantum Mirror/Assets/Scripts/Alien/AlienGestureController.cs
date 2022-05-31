using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GestureState
{
	Repositioning,
	Pointing,
	Gesturing,
	HoldingGesture,
	StartGesture,
	EndGesture,
	Ended
}

public class AlienGestureController : MonoBehaviour
{

	[Header( "References" )]
    public HandInfo[] hands;
	public Transform[] idleHandTargets;
	public GestureSequence_Set gestureLibrary;
	public GestureSequence_Set responses;

	[Header( "Settings" )]
	public GestureSequence standardResponse;
	[Tooltip( "Should the alien hold at the centre of the circle before starting with the gestures?" )]
	public bool holdStart;
	[Tooltip( "How fast in units per second should the alien gestures." )]
	public float gestureSpeed = 1f;
	[Tooltip( "How long should the alien hold a gestures before moving on to the next one." )]
	public float holdGestureFor = 1f;
	[Tooltip( "How fast in units per second should the alien point." )]
	public float pointSpeed = 1f;
	[Tooltip( "How far away from the alien's center should the point target be allowed to be in units, if set low the arm may not full extend." )]
	public float maxPointDistance = 5f;
	[Tooltip( "How long should the alien hold a point before lowering it's arm again." )]
	public float holdPointFor = 3f;

	[Header( "Runtime" )]
	[ReadOnly] public GestureState gestureState;
	[ReadOnly] public bool standardGesture = false;
	[ReadOnly] public float gestureHoldTimeStamp = 0f;
	[ReadOnly] public float pointHoldTimeStamp = 0f;
	[ReadOnly] public int gestureHandIndex = -1;
	[ReadOnly] public int pointHandIndex = -1;
	[ReadOnly] public int sentenceIndex = 0;
	[ReadOnly] public int wordIndex = 0;
	[Space( 10 )]
	[ReadOnly] public Vector3 preGestureHandPos;
	[Space( 10 )]
	[ReadOnly] public Vector3 handTarget;

	[HideInInspector] public AlienManager alienManager;
	[HideInInspector] public List<Gesture> playerGestures = new List<Gesture>();
	[HideInInspector] public List<int> respondTo = new List<int>();

	public int FindClosestHand( Transform respondTo )
	{
		int closestHand = 0;
		float shortestDist = 0f;

		for ( int i = 0; i < hands.Length; i++ )
		{
			float dist = Vector3.Distance( alienManager.gestureCircle.transform.position, hands[ i ].handTransform.position );
			if ( dist < shortestDist || i == 0 )
			{
				closestHand = i;
				shortestDist = dist;
			}
		}
		return closestHand;
	}

	public Transform FindClosetObjectInList( RunTimeSet<Transform> targetObjects )
	{
		float shortestDist = 0f;
		int closestObjectIndex = 0;
		for ( int i = 0; i < targetObjects.Items.Count; i++ )
		{
			if ( i == 0 )
			{
				shortestDist = Vector3.Distance( targetObjects.Items[ i ].transform.position, alienManager.transform.position );
				closestObjectIndex = 0;
			}
			else
			{
				float dist = Vector3.Distance( targetObjects.Items[ i ].transform.position, alienManager.transform.position );
				if ( dist < shortestDist )
				{
					shortestDist = dist;
					closestObjectIndex = i;
				}
			}
		}
		return targetObjects.Items[ closestObjectIndex ].transform;
	}

	public TheKiwiCoder.BTNode.State Point()
	{
		AlienIKHandler hand = hands[ pointHandIndex ].ikHandler;

		if ( gestureState == GestureState.HoldingGesture )
		{
			if ( Time.time - pointHoldTimeStamp > holdPointFor )
				gestureState = GestureState.Gesturing;
		}
		else if ( gestureState == GestureState.Gesturing )
		{
			float speed = pointSpeed * Time.deltaTime;
			if ( speed > Vector3.Distance( hand.transform.position, handTarget ) )
			{
				hand.transform.position = handTarget;

				if ( handTarget != idleHandTargets[ pointHandIndex ].position )
				{
					pointHoldTimeStamp = Time.time;
					gestureState = GestureState.HoldingGesture;
					handTarget = idleHandTargets[ pointHandIndex ].position;
				}
				else
					return TheKiwiCoder.BTNode.State.Success;
			}
			else
				hand.transform.position = Vector3.MoveTowards( hand.transform.position, handTarget, speed );
		}
		return TheKiwiCoder.BTNode.State.Running;
	}

	public void FindGesture()
	{
		//Find the player's sentence in the library and save the id.
		bool sentenceFound = false;
		for ( int i = 0; i < gestureLibrary.Items.Count; i++ )
		{
			//Check if the gesture codes match.
			for ( int j = 0; j < gestureLibrary.Items[ i ].gestureCode.Length; j++ )
			{
				if ( gestureLibrary.Items[ i ].gCode == alienManager.gestureCircle.sentence )
				{
					sentenceIndex = i;
					sentenceFound = true;
					break;
				}
			}
		}

		Debug.Log( "Known Sentence?: " + sentenceFound + " ( " + alienManager.gestureCircle.sentence + " = " + gestureLibrary.Items[ sentenceIndex ].gCode + " )" );
		if ( sentenceFound && responses.Items[ sentenceIndex ] != null )
		{
			ResetGestureSettings();
		}
		else
		{
			ResetGestureSettings();
			standardGesture = true;
		}
	}

	public void SetGesture( GestureSequence sentence )
	{
		responses.Items.Add( sentence );
		sentenceIndex = responses.Items.Count - 1;
		ResetGestureSettings();
	}

	private void ResetGestureSettings()
	{
		int closestHand = FindClosestHand( alienManager.player );

		gestureHandIndex = closestHand;
		wordIndex = -1;
		gestureState = GestureState.StartGesture;
		preGestureHandPos = hands[ gestureHandIndex ].ikHandler.transform.position;
	}

	public TheKiwiCoder.BTNode.State Gesture()
	{
		HandInfo hand = hands[ gestureHandIndex ];
		GestureCircle gestureCircle = alienManager.gestureCircle;
		Animator[] fingerAnimators = hands[ gestureHandIndex ].fingerAnimators;
		Debug.DrawLine( hand.handTransform.transform.position, hand.handTransform.transform.position + gestureCircle.transform.up * 5f );

		List<Gesture> gestures;
		if ( standardGesture )
		{
			gestures = new List<Gesture>();
			for ( int i = 0; i < standardResponse.words.Count; i++ )
				gestures.Add( standardResponse.words[ i ] );
		}
		else
		{
			gestures = new List<Gesture>();
			for ( int i = 0; i < responses.Items[ sentenceIndex ].words.Count; i++ )
			{
				gestures.Add( responses.Items[ sentenceIndex ].words[ i ] );
			}
		}
		gestures.Add( new Gesture( 0, new bool[ 3 ] { false, false, false } ) );

		//Hold Gesture
		if ( gestureState == GestureState.HoldingGesture )
		{
			if ( Time.time - gestureHoldTimeStamp > holdGestureFor )
			{
				gestureState = GestureState.Gesturing;
				for ( int i = 0; i < fingerAnimators.Length; i++ )
					fingerAnimators[ i ].SetBool( "FingerOpen", false );
			}
		}
		else
		{
			if ( gestureState == GestureState.StartGesture )
			{
				handTarget = alienManager.gestureCircle.transform.position;
				gestureState = GestureState.Gesturing;

				gestureCircle.Clear();
				if ( gestureCircle.twoWayCircle )
					gestureCircle.otherCircle.Clear();

				for ( int i = 0; i < fingerAnimators.Length; i++ )
				{
					if ( fingerAnimators[ i ].GetBool( "FingerOpen" ) )
						fingerAnimators[ i ].SetBool( "FingerOpen", false );
				}
			}
			//Check if we have reached our new hand target.
			else
			{
				float speed = gestureSpeed * Time.deltaTime;
				//Debug.Log( speed + " > " + Vector3.Distance( hand.transform.position, _owner.gc.handTarget ) + " | " + _owner.gc.waiting );
				if ( speed > Vector3.Distance( hand.ikHandler.transform.position, handTarget ) )
				{
					if ( gestureState == GestureState.EndGesture )
					{
						gestureHandIndex = -1;
						if ( standardGesture ) standardGesture = false;
						gestureState = GestureState.Ended;
						return TheKiwiCoder.BTNode.State.Success;
					}
					//Set new hand target.
					else if ( gestureState == GestureState.Gesturing )
					{
						hand.ikHandler.transform.position = handTarget;
						if ( holdStart || wordIndex >= 0 && !holdStart )
						{
							//Manipulate hand to make the gesture.
							hand.handTransform.transform.LookAt( hand.handTransform.transform.position + -gestureCircle.transform.up );
							for ( int i = 0; i < fingerAnimators.Length; i++ )
							{
								if ( gestures[ wordIndex ].fingers[ i ] && !fingerAnimators[ i ].GetBool( "FingerOpen" ) )
									fingerAnimators[ i ].SetBool( "FingerOpen", true );
							}

							//Input the gesture into the circle.
							Gesture gesture = gestures[ wordIndex ];
							gestureCircle.subCircles[ gesture.circle ].ConfirmGestureTwoWay( gestures[ wordIndex ].circle, gestures[ wordIndex ].fingers );

							gestureHoldTimeStamp = Time.time;
							gestureState = GestureState.HoldingGesture;
						}

						wordIndex++;
						//Set target as our start position.
						if ( wordIndex > gestures.Count - 1 )
						{
							handTarget = idleHandTargets[ gestureHandIndex ].position;
							gestureState = GestureState.EndGesture;
						}
						//Set target as the next word in the sentence.
						else
						{
							handTarget = gestureCircle.subCircles[ gestures[ wordIndex ].circle ].transform.position;
							//Debug.Log( Vector3.Distance( hand.transform.position, _owner.gc.handTarget ) + " | " + _owner.gc.waiting );
						}
					}
				}
				//Move towards hand target.
				else
					hand.ikHandler.transform.position = Vector3.MoveTowards( hand.ikHandler.transform.position, handTarget, speed );
			}
		}
		return TheKiwiCoder.BTNode.State.Running;
	}

	private void OnDrawGizmos() {
		Gizmos.DrawCube( handTarget, Vector3.one );
	}
}

[System.Serializable]
public class HandInfo
{
	public Transform handTransform;
	public AlienIKHandler ikHandler;
	public Animator[] fingerAnimators;
}