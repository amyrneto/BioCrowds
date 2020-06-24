/// ---------------------------------------------
/// Contact: Henry Braun
/// Brief: Defines an Auxin
/// Thanks to VHLab for original implementation
/// Date: November 2017 
/// ---------------------------------------------

using UnityEngine;
using System.Collections;

namespace Biocrowds.Core
{
    public class Auxin : MonoBehaviour
    {
		public const float DEFAULT_MIN_DISTANCE_TO_AGENT = 10000.0f;
		//is auxin taken?
		public bool IsTaken;

        //position
        private Vector3 _position;
        public Vector3 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                transform.position = _position;
            }
        }

        //min distance from a taken agent
        //when a new agent find it in his personal space, test the distance with this value to see which one is smaller
		public float MinDistanceToAgent = DEFAULT_MIN_DISTANCE_TO_AGENT;

		//agent who took this auxin
		public Agent Agent;

		//cell who has this auxin
		public Cell Cell;

        //Reset auxin to his default state, for each update
        public void ResetAuxin()
        {
			//just draw the lines to each auxin
			if (Agent != null)
			{
				Debug.DrawLine(Position, Agent.transform.position, Color.green);
			}

			MinDistanceToAgent = DEFAULT_MIN_DISTANCE_TO_AGENT;
			Agent = null;
			IsTaken = false;
        }

		public Vector3 DistanceTo(Vector3 other)
		{
			return Position - other;
		}
    }
}