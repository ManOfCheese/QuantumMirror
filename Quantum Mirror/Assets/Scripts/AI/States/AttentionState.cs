using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StateMachine;

public class AttentionState : State<AlienManager>
{
	#region singleton
	//Create a single instance of this state for all state machines.
	private static AttentionState _instance;

	private AttentionState()
	{
		if ( _instance != null )
		{
			return;
		}

		_instance = this;
	}

	public static AttentionState Instance
	{
		get
		{
			if ( _instance == null )
			{
				new AttentionState();
			}

			return _instance;
		}
	}
	#endregion

	public override void EnterState( AlienManager _owner )
	{
		_owner.mc.agent.destination = _owner.transform.position;
		_owner.gc.repositioning = true;
		_owner.gc.repositionedLegs.Clear();

		for ( int i = 0; i < _owner.gc.hands.Length; i++ ) {
			_owner.gc.hands[ i ].enabled = false;
		}
	}

	public override void UpdateState( AlienManager _owner )
	{
		if ( _owner.gc.repositioning ) {
			for ( int i = 0; i < _owner.gc.hands.Length; i++ ) 
			{
				if ( !_owner.gc.repositionedLegs.Contains( i ) ) 
				{
					Transform handTransform = _owner.gc.hands[ i ].transform;
					handTransform.position = Vector3.MoveTowards( handTransform.position, _owner.gc.idleHandTargets[ i ].position, 
						_owner.mc.speed * Time.deltaTime );

					if ( handTransform.position == _owner.gc.idleHandTargets[ i ].position )
						_owner.gc.repositionedLegs.Add( i );
				}
			}

			if ( _owner.gc.repositionedLegs.Count == _owner.gc.hands.Length )
				_owner.gc.repositioning = false;
		}
		
		if ( _owner.gc.gesturing )
		{
			GameObject circle = _owner.gc.gestureCircles[ _owner.gc.handIndex ];
			List<Gesture> gestures = _owner.gc.responses.Items[ _owner.gc.sentenceIndex ].words;
			AlienIKHandler hand = _owner.gc.hands[ _owner.gc.handIndex ];

			if ( _owner.gc.startGesture )
			{
				circle.SetActive( true );
				_owner.gc.handTarget = circle.transform.position;
				_owner.gc.startGesture = false;
			}

			//Hold Gesture
			if ( _owner.gc.waiting )
			{
				if ( Time.time - _owner.gc.waitTimeStamp > _owner.gc.holdPosFor )
					_owner.gc.waiting = false;
			}
			//Check if we have reached our new hand target.
			else
			{
				float speed = _owner.gc.gestureSpeed * Time.deltaTime;
				//Debug.Log( speed + " > " + Vector3.Distance( hand.transform.position, _owner.gc.handTarget ) + " | " + _owner.gc.waiting );
				if ( speed > Vector3.Distance( hand.transform.position, _owner.gc.handTarget ) )
				{
					if ( _owner.gc.endGesture ) 
					{
						_owner.gc.gesturing = false;
						circle.SetActive( false );
						_owner.gc.waiting = false;
						_owner.gc.handIndex = -1;
					}
					//Set new hand target.
					else {
						hand.transform.position = _owner.gc.handTarget;
						_owner.gc.waitTimeStamp = Time.time;
						_owner.gc.waiting = true;

						_owner.gc.wordIndex++;
						//Set target as our previous position.
						if ( _owner.gc.wordIndex > _owner.gc.responses.Items[ _owner.gc.sentenceIndex ].words.Count - 1 ) {
							_owner.gc.handTarget = _owner.gc.idleHandTargets[ _owner.gc.handIndex ].position;
							_owner.gc.endGesture = true;
						}
						//Set target as the next word in the sentence.
						else {
							_owner.gc.handTarget = hand.subCircles[ gestures[ _owner.gc.wordIndex ].circle ].transform.position;
							Debug.Log( Vector3.Distance( hand.transform.position, _owner.gc.handTarget ) + " | " + _owner.gc.waiting );
						}
					}
				}
				//Move towards hand target.
				else
				{
					hand.transform.position = Vector3.MoveTowards( hand.transform.position, _owner.gc.handTarget, speed );
				}
			}
		}
	}

	public override void ExitState( AlienManager _owner )
	{
		for ( int i = 0; i < _owner.gc.hands.Length; i++ )
			_owner.gc.hands[ i ].enabled = true;
		for ( int i = 0; i < _owner.gc.gestureCircles.Length; i++ )
			_owner.gc.gestureCircles[ i ].SetActive( false );
		_owner.gc.gesturing = false;
		_owner.gc.waiting = false;
		_owner.gc.handIndex = -1;
		_owner.gc.repositioning = false;
	}
}
