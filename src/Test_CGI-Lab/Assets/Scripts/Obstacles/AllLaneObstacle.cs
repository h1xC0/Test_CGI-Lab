using System.Collections;
using Tracks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Obstacles
{
	public class AllLaneObstacle: Obstacle
	{
		public override IEnumerator Spawn(TrackSegment segment, float t)
		{
			Vector3 position;
			Quaternion rotation;
			segment.GetPointAt(t, out position, out rotation);
			AsyncOperationHandle op = Addressables.InstantiateAsync(gameObject.name, position, rotation);
			yield return op;
			if (op.Result == null || !(op.Result is GameObject))
			{
				Debug.LogWarning(string.Format("Unable to load obstacle {0}.", gameObject.name));
				yield break;
			}
			GameObject obj = op.Result as GameObject;
			obj.transform.SetParent(segment.objectRoot, true);

			//TODO : remove that hack related to #issue7
			Vector3 oldPos = obj.transform.position;
			obj.transform.position += Vector3.back;
			obj.transform.position = oldPos;
		}
	}
}
