using UnityEngine;

namespace VladislavMang.Merging.Example._3D
{
	public class UnitSlot : MonoBehaviour, I3dMergingItemSlot<Unit, UnitData>
	{
		public Unit Unit { get; set; }
		public Unit Item { get => Unit; set => Unit = value; }
		public Vector3 Point => transform.position;
		public Vector3 ForwardDirection => transform.forward;
		public bool IsEmpty => Unit == null;
	}
}