/// ---------------------------------------------
/// Contact: Henry Braun
/// Brief: Defines an Cell
/// Thanks to VHLab for original implementation
/// Date: November 2017 
/// ---------------------------------------------

using UnityEngine;
using System.Collections.Generic;

namespace Biocrowds.Core
{
	public class Cell : MonoBehaviour
	{
		[SerializeField]
		private Material normal;
		[SerializeField]
		private Material highlight;
		
		public int X;
		public int Z;

		public Vector3 scale;

		public List<Auxin> Auxins = new List<Auxin>();

		public void ResetAuxins()
		{
			foreach(Auxin auxin in Auxins)
			{
				auxin.ResetAuxin();
			}
		}
		public void HighlightCell()
		{
			//Debug.Log("Cell[" + X + "][" + Z + "].Highlight");
			gameObject.GetComponent<Renderer>().material = highlight;
		}

		public void UnlitCell()
		{
			//Debug.Log("Cell[" + X + "][" + Z + "].Unlit");
			gameObject.GetComponent<Renderer>().material = normal;
		}

		public bool InRange(Cell other, int raySqr)
		{
			int deltaX = Mathf.Abs(this.X - other.X);
			deltaX *= deltaX;
			int deltaZ = Mathf.Abs(this.Z - other.Z);
			deltaZ *= deltaZ;
			return ((deltaX + deltaZ) < raySqr);
		}

	}
}