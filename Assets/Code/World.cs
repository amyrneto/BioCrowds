/// ---------------------------------------------
/// Contact: Henry Braun
/// Brief: Defines the world environment
/// Thanks to VHLab for original implementation
/// Date: November 2017 
/// ---------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using UnityEditor.AI;
using System.Collections;
using UnityEngine.AI;

namespace Biocrowds.Core
{
	public class World : MonoBehaviour
	{
		//agent radius
		private const float AGENT_RADIUS = 3.0f;

		//radius for auxin collide
		private const float AUXIN_RADIUS = 0.1f;

		//density
		private const float AUXIN_DENSITY = 0.45f; //0.65f;

		//social distancing
		private const float SOCIAL_DISTANCING = 2.0f;

		[SerializeField]
		private Terrain _terrain;

		[SerializeField]
		private GameObject[] _goals;

		[SerializeField]
		private Vector2 _dimension = new Vector2(30.0f, 20.0f);
		public Vector2 Dimension {
			get { return _dimension; }
		}

		//number of agents in the scene
		[SerializeField]
		private int _maxAgents = 30;

		//agent prefab
		[SerializeField]
		private Agent _agentPrefab;

		[SerializeField]
		private Cell _cellPrefab;
		public float CellWidth {
			get { return _cellPrefab.transform.localScale.x; }
		}
		public float CellHeight {
			get { return _cellPrefab.transform.localScale.y; }
		}
		public float CellLength {
			get { return _cellPrefab.transform.localScale.z; }
		}

		[SerializeField]
		private Auxin _auxinPrefab;

		[SerializeField]
		private BoxCollider[] _obstaclesColliders;

		private List<Agent> _agents = new List<Agent>();
		public List<Agent> Agents {
			get { return _agents; }
		}

		[SerializeField]
		public List<Cell> _cells = new List<Cell>();
		public List<Cell> Cells {
			get { return _cells; }
		}
		private int _cellsInEachRow = -1;
		public int CellsInEachRow {
			get { return SetCellsInEachRow(); }
		}


		//max auxins on the ground
		private int _maxAuxins;
		private bool _isReady;

		// Use this for initialization
		IEnumerator Start()
		{
			//Application.runInBackground = true;

			//change terrain size according informed
			_terrain.terrainData.size = new Vector3(_dimension.x, _terrain.terrainData.size.y, _dimension.y);

			// Set Goals
			_goals = GameObject.FindGameObjectsWithTag("Goal");

			//create all cells based on dimension
			yield return StartCoroutine(CreateCells());

			//populate cells with auxins
			yield return StartCoroutine(DartThrowing());

			//create our agents
			yield return StartCoroutine(CreateAgents());

			//build the navmesh at runtime
			//NavMeshBuilder.BuildNavMesh();

			//wait a little bit to start moving
			yield return new WaitForSeconds(1.0f);
			_isReady = true;
		}

		private IEnumerator CreateCells()
		{
			Transform cellPool = new GameObject("Cells").transform;
			Vector3 cellDimension = _cellPrefab.transform.localScale;
			float offset = 1.0f;
			//Debug.Log(CellsInEachRow);
			for (int i = 0; i < _dimension.y / cellDimension.z; i++) //i + agentRadius * 2
			{
				for (int j = 0; j < _dimension.x / cellDimension.x; j++) // j + agentRadius * 2
				{
					//instantiante a new cell
					Vector3 pos = new Vector3(offset + (j * cellDimension.z), 0.0f, offset + (i * cellDimension.x));

					Cell newCell = Instantiate(_cellPrefab, pos, Quaternion.Euler(90.0f, 0.0f, 0.0f), cellPool);

					//metadata for optimization
					newCell.name = "Cell [" + i + "][" + j + "]";
					newCell.X = i;
					newCell.Z = j;
					newCell.scale = cellDimension;

					_cells.Add(newCell);
					yield return null;
				}
			}
		}

		private IEnumerator DartThrowing()
		{
			//lets set the qntAuxins for each cell according the density estimation

			Transform auxinPool = new GameObject("Auxins").transform;

			float densityToQnt = AUXIN_DENSITY / (AUXIN_RADIUS * AUXIN_RADIUS);
			//Debug.Log(densityToQnt);

			_maxAuxins = (int)Mathf.Floor(densityToQnt);
			float offset = (CellWidth - AUXIN_RADIUS) / 2;
			//Debug.Log(offset);

			//for each cell, we generate its auxins
			for (int c = 0; c < _cells.Count; c++)
			{
				//Dart throwing auxins
				//use this flag to break the loop if it is taking too long (maybe there is no more space)
				int flag = 0;
				for (int i = 0; i < _maxAuxins; i++)
				{
					float x = Random.Range(_cells[c].transform.position.x - offset, _cells[c].transform.position.x + offset);
					float z = Random.Range(_cells[c].transform.position.z - offset, _cells[c].transform.position.z + offset);

					//see if there are auxins in this radius. if not, instantiante
					List<Auxin> allAuxinsInCell = _cells[c].Auxins;
					bool createAuxin = true;
					for (int j = 0; j < allAuxinsInCell.Count; j++)
					{
						float distanceAASqr = (new Vector3(x, 0f, z) - allAuxinsInCell[j].Position).sqrMagnitude;

						//if it is too near no need to add another
						if (distanceAASqr < AUXIN_RADIUS * AUXIN_RADIUS)
						{
							createAuxin = false;
							break;
						}
					}

					//if i have found no auxin, i still need to check if is there obstacles on the way
					if (createAuxin)
					{
						//sphere collider to try to find the obstacles
						//NavMeshHit hit;
						//createAuxin = NavMesh.Raycast(new Vector3(x, 2f, z), new Vector3(x, -2f, z), out hit, 1 << NavMesh.GetAreaFromName("Walkable")); //NavMesh.GetAreaFromName("Walkable")); // NavMesh.AllAreas);
						//createAuxin = NavMesh.SamplePosition(new Vector3(x, 0.0f, z), out hit, 0.1f, 1 << NavMesh.GetAreaFromName("Walkable"));
						//bool isBlocked = _obstacleCollider.bounds.Contains(new Vector3(x, 0.0f, z));
						Collider[] hitColliders = Physics.OverlapSphere(new Vector3(x, 0f, z), AUXIN_RADIUS + 0.1f, 1 << LayerMask.NameToLayer("Obstacle"));
						createAuxin = (hitColliders.Length == 0);
					}

					//check if auxin can be created there
					if (createAuxin)
					{
						Auxin newAuxin = Instantiate(_auxinPrefab, new Vector3(x, 0.0f, z), Quaternion.identity, auxinPool);

						//change its name
						newAuxin.name = "Auxin [" + c + "][" + i + "]";
						//this auxin is from this cell
						newAuxin.Cell = _cells[c];
						//set position
						newAuxin.Position = new Vector3(x, 0f, z);

						//add this auxin to this cell
						_cells[c].Auxins.Add(newAuxin);

						//reset the flag
						flag = 0;

						//speed up the demonstration a little bit...
						if (i % 200 == 0)
							yield return null;
					}
					else
					{
						//else, try again
						flag++;
						i--;
					}

					//if flag is above qntAuxins (*2 to have some more), break;
					if (flag > _maxAuxins * 2)
					{
						//reset the flag
						flag = 0;
						break;
					}
				}
			}
		}

		private IEnumerator CreateAgents()
		{
			Transform agentPool = new GameObject("Agents").transform;
			const float initialXPos = 1.0f;
			const float initialZPos = 1.0f;

			float xPos = initialXPos;
			float zPos = initialZPos;

			//instantiate agents
			for (int i = 0; i < _maxAgents; i++)
			{
				Agent newAgent = Instantiate(_agentPrefab, new Vector3(xPos, 0.5f, zPos), Quaternion.identity, agentPool);

				newAgent.name = "Agent [" + i + "]";  //name
				newAgent.CurrentCell = _cells[i];  //agent cell
				newAgent.AgentRadius = AGENT_RADIUS;  //agent radius
				newAgent.Goal = GetNewGoal();  //agent goal
				newAgent.World = this;
				newAgent.DistancingRadius = SOCIAL_DISTANCING;


				_agents.Add(newAgent);

				xPos += 1.0f;
				if (xPos >= _dimension.x)
				{
					xPos = initialXPos;
					zPos += 1.0f;
				}

				yield return null;
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (!_isReady)
				return;

			ResetAuxins();


			/*
			 * to find where the agent must move, we need to get the vectors from the agent to each auxin he has, and compare with 
			 * the vector from agent to goal, generating an angle which must lie between 0 (best case) and 180 (worst case)
			 * The calculation formula was taken from the Bicho´s master thesis and from Paravisi OSG implementation.
			*/
			/*for each agent:
			1 - for each auxin near him, find the distance vector between it and the agent
			2 - calculate the movement vector 
			3 - calculate speed vector 
			4 - step
			*/
			foreach(Agent agent in _agents)
				agent.FindAuxinsInRange();

			for (int i = 0; i < _maxAgents; i++)
			{
				//calculate the movement vector
				_agents[i].CalculateDirection();
				//calculate speed vector
				_agents[i].CalculateVelocity();
				//step
				_agents[i].Step();
			}
		}

		public void ResetAuxins()
		{
			for (int i = 0; i < _cells.Count; i++)
				_cells[i].ResetAuxins();
		}

		public List<Cell> GetCells(Vector3 position, int ray)
		{
			int raySqr = ray * ray;

			List<Cell> neighbouringCells = new List<Cell>();
			Cell c = _cells[0];
			int currentX = Mathf.FloorToInt(position.x / c.scale.x);
			int currentZ = Mathf.FloorToInt(position.z / c.scale.z);
			int currentCell = GetCellIndex(currentX, currentZ);
			neighbouringCells.Add(_cells[currentCell]);

			foreach (Cell cell in _cells)
			{
				if (cell == _cells[currentCell])
				{
					//_cells[currentCell].HighlightCell();
					continue;
				}
				if (_cells[currentCell].InRange(cell, raySqr))
				{
					neighbouringCells.Add(cell);
				}
			}

			return neighbouringCells;
		}


		public int GetCellIndex(int X, int Z)
		{
			int index = Z * CellsInEachRow + X;
			return index;
		}

		public Transform GetNewGoal()
		{
			int goalIndex = Random.Range(0, _goals.Length);
			//Debug.Log(goalIndex);
			return _goals[goalIndex].transform;  //agent goal
		}

		private int SetCellsInEachRow()
		{
			if (_cellsInEachRow == -1)
			{ // Not initialized
			  // chache it
				_cellsInEachRow = Mathf.FloorToInt(_dimension.x / _cellPrefab.transform.localScale.x);
			}
			return _cellsInEachRow;
		}
	}
}