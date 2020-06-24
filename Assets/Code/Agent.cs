/// ---------------------------------------------
/// Contact: Henry Braun
/// Brief: Defines an Agent
/// Thanks to VHLab for original implementation
/// Date: November 2017 
/// ---------------------------------------------

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

namespace Biocrowds.Core
{
	public class Agent : MonoBehaviour
	{
		private const float UPDATE_NAVMESH_INTERVAL = 1.0f;

		[SerializeField]
		private Transform spacingRadiusGizmo;

		//agent radius
		private float _agentRadius;
		public float AgentRadius {
			get { return _agentRadius; }
			set {
				_agentRadius = value;
				AgentRadiusSquare = _agentRadius * _agentRadius;
			}
		}
		public float AgentRadiusSquare;
		//agent speed
		public Vector3 _velocity;
		//social distancing radius
		private float _distancingRadiusSqr;
		private float _distancingRadius;
		public float DistancingRadius {
			get { return _distancingRadius; }
			set {
				_distancingRadius = value;
				_distancingRadiusSqr = _distancingRadius * _distancingRadius;
				//Debug.Log(_distancingRadiusSqr);
				float r = _distancingRadius / transform.localScale.x;
				spacingRadiusGizmo.localScale = new Vector3(r, r, r);
			}
		}
		//max speed
		[SerializeField]
		private float _maxSpeed = 1.5f;

		//goal
		public Transform Goal;
		public float GoalReachDistance;

		//list with all auxins in his personal space
		public List<Auxin> Auxins = new List<Auxin>();

		//agent cell
		public Cell CurrentCell;

		public World World;

		private int _totalX;
		private int _totalZ;

		private NavMeshPath _navMeshPath;

		//time elapsed (to calculate path just between an interval of time)
		private float _elapsedTime;
		//auxins distance vector from agent
		public List<Vector3> _auxinDistances;

		/*-----------Paravisis' model-----------*/
		private bool _isDenW = false; //  avoid recalculation
		private float _denW;    //  avoid recalculation
		private Vector3 _rotation; //orientation vector (movement)
		private Vector3 _goalPosition; //goal position
		private Vector3 _dirAgentGoal; //diff between goal and agent

		// Debug
		float maxDistanceToAuxin = 0.0f;

		void Start()
		{
			_navMeshPath = new NavMeshPath();

			UpdatePath();

			//cache world info
			_totalX = Mathf.FloorToInt(World.Dimension.x / World.CellWidth);
			_totalZ = Mathf.FloorToInt(World.Dimension.y / World.CellLength);
		}

		void Update()
		{
			//clear agent´s information
			ClearAgent();

			// Update the way to the goal every second.
			_elapsedTime += Time.deltaTime;

			if (_elapsedTime > UPDATE_NAVMESH_INTERVAL)
			{
				_elapsedTime = 0.0f;

				UpdatePath();
			}

			//draw line to goal
			for (int i = 0; i < _navMeshPath.corners.Length - 1; i++)
				Debug.DrawLine(_navMeshPath.corners[i], _navMeshPath.corners[i + 1], Color.red);
		}

		void UpdatePath()
		{
			//calculate agent path
			bool foundPath = NavMesh.CalculatePath(transform.position, Goal.position, NavMesh.AllAreas, _navMeshPath);

			//update its goal if path is found
			if (foundPath)
			{
				_goalPosition = new Vector3(_navMeshPath.corners[1].x, 0f, _navMeshPath.corners[1].z);
				_dirAgentGoal = _goalPosition - transform.position;
			}
		}

		//clear agent´s informations
		void ClearAgent()
		{
			//re-set inicial values
			_denW = 0;
			_auxinDistances.Clear();
			_isDenW = false;
			_rotation = new Vector3(0f, 0f, 0f);
			_dirAgentGoal = _goalPosition - transform.position;
		}

		//walk
		public void Step()
		{
			if (_velocity.sqrMagnitude > 0.0f)
				transform.Translate(_velocity * Time.deltaTime, Space.World);
			//Debug.Log(Vector3.Distance(transform.position, Goal.position));
			if (Vector3.Distance(transform.position, Goal.position) < GoalReachDistance)
				GoalReached();
		}

		void GoalReached()
		{
			// Get another goal randomly
			Goal = World.GetNewGoal();
		}

		//The calculation formula starts here
		//the ideia is to find m=SUM[k=1 to n](Wk*Dk)
		//where k iterates between 1 and n (number of auxins), Dk is the vector to the k auxin and Wk is the weight of k auxin
		//the weight (Wk) is based on the degree resulting between the goal vector and the auxin vector (Dk), and the
		//distance of the auxin from the agent
		public void CalculateDirection()
		{
			//for each agent´s auxin
			for (int k = 0; k < _auxinDistances.Count; k++)
			{
				//calculate W
				float valorW = CalculaW(k);
				if (_denW < 0.0001f)
					valorW = 0.0f;

				//sum the resulting vector * weight (Wk*Dk)
				_rotation += valorW * _auxinDistances[k] * _maxSpeed;
			}
		}

		//calculate W
		float CalculaW(int indiceRelacao)
		{
			//calculate F (F is part of weight formula)
			float fVal = GetF(indiceRelacao);

			if (!_isDenW)
			{
				_denW = 0f;

				//for each agent´s auxin
				for (int k = 0; k < _auxinDistances.Count; k++)
				{
					//calculate F for this k index, and sum up
					_denW += GetF(k);
				}
				_isDenW = true;
			}

			return fVal / _denW;
		}

		//calculate F (F is part of weight formula)
		float GetF(int pRelationIndex)
		{
			//distance between auxin´s distance and origin 
			//float Ymodule = Vector3.Distance(_distAuxin[pRelationIndex], Vector3.zero);
			float Ymodule = _auxinDistances[pRelationIndex].magnitude;
			//Debug.Log(Ymodule.ToString() + " : " + _distAuxin[pRelationIndex].magnitude);
			//distance between goal vector and origin
			float Xmodule = _dirAgentGoal.normalized.magnitude;
			//float Xmodule = 1.0f;
			//float Xmodule = _dirAgentGoal.magnitude;
			//Debug.Log(Xmodule);
			float dot = Vector3.Dot(_auxinDistances[pRelationIndex], _dirAgentGoal.normalized);

			if (Ymodule < 0.00001f)
				return 0.0f;

			//return the formula, defined in thesis
			return (float)((1.0f / (1.0f + Ymodule)) * (1.0f + ((dot) / (Xmodule * Ymodule))));
		}

		//calculate speed vector    
		public void CalculateVelocity()
		{
			//distance between movement vector and origin
			float moduleM = Vector3.Distance(_rotation, Vector3.zero);

			//multiply for PI
			float s = moduleM * Mathf.PI;

			//if it is bigger than maxSpeed, use maxSpeed instead
			if (s > _maxSpeed)
				s = _maxSpeed;

			//Debug.Log("vetor M: " + m + " -- modulo M: " + s);
			if (moduleM > 0.0001f)
			{
				//calculate speed vector
				_velocity = s * (_rotation / moduleM);
			}
			else
			{
				//else, go idle
				_velocity = Vector3.zero;
			}
		}

		//find all auxins near him (Voronoi Diagram)
		//call this method from game controller, to make it sequential for each agent
		public void FindAuxinsInRange()
		{
			//clear them all, for obvious reasons -> not so obvious....
			Auxins.Clear();

			List<Cell> neighboringCells = World.GetCells(transform.position, Mathf.FloorToInt(AgentRadius));
			float distanceToCellSqr = (transform.position - CurrentCell.transform.position).sqrMagnitude; //Vector3.Distance(transform.position, _currentCell.transform.position);
			foreach (Cell cell in World.Cells)
			{
				if (neighboringCells.Contains(cell))
				{
					//cell.HighlightCell();
					CheckAuxins(ref distanceToCellSqr, cell);
				}/*
				else
				{
					cell.UnlitCell();
				}*/
			}
		}

		private void CheckAuxins(ref float pDistToCellSqr, Cell pCell)
		{
			//iterate all cell auxins to check distance between auxins and agent
			foreach (Auxin auxin in pCell.Auxins)
			{
				//see if the distance between this agent and this auxin is smaller than the actual value, and smaller than agent radius
				float distanceSqr = (transform.position - auxin.Position).sqrMagnitude;
				//Debug.Log(auxin.MinDistanceToAgent);

				if (distanceSqr < auxin.MinDistanceToAgent && distanceSqr <= AgentRadiusSquare)
				{
					//take the auxin
					TakeAuxin(auxin, distanceSqr);
				}
			}

			//see distance to this cell
			float distanceToNeighbourCell = (transform.position - pCell.transform.position).sqrMagnitude;
			if (distanceToNeighbourCell < pDistToCellSqr)
			{
				pDistToCellSqr = distanceToNeighbourCell;
				CurrentCell = pCell;
			}
		}

		private void TakeAuxin(Auxin auxin, float distanceSquare)
		{
			//if this auxin already was taken, need to remove it from the agent who had it
			if (auxin.IsTaken)
			{
				if (auxin.MinDistanceToAgent > _distancingRadius)
				{
					auxin.Agent.Auxins.Remove(auxin);
				}
				else { return; }
			}

			//auxin is taken
			auxin.IsTaken = true;

			//auxin has agent
			auxin.Agent = this;
			//update min distance
			auxin.MinDistanceToAgent = distanceSquare;
			//update my auxins
			Auxins.Add(auxin);
			//add the distance vector between it and the agent
			Vector3 displacement = auxin.Position - transform.position;
			_auxinDistances.Add(displacement);
			//_auxinDistances.Add(auxin.DistanceTo(transform.position));
		}
	}
}