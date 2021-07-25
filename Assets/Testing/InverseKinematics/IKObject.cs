using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


//*Idéa: Let IKObject rotate towards target as close as possible, if it determines it can't reach the target, let the parent know to start trying to rotate towards it as well
//*-- if child determines it can try to rotate again, restrict the parent again & repeat until child can reach the target
public class IKObject : MonoBehaviour
{
	[SerializeField] [Range(0, 1)] float rotationSpeed = 0.5f;
	[SerializeField] float maxRotationAngle = 160f;
	[SerializeField] Transform target, iKExtent;
	[SerializeField] IKObject child, parent;
	[SerializeField] float ikExtentOffset;

	public bool TargetReached { get { return targetReached; } }
	public bool RestrictParent { get { return restrictParent; } set { restrictParent = value; } }
	public bool RetractArm { get { return retractArm; } set { retractArm = value; } }
	private bool targetReached = false, restrictParent = false, retractArm = false;
	private Vector3 startRotation;

	void Awake()
	{
		if (iKExtent == null)
		{
			iKExtent = new GameObject("IKExtent").transform;
			iKExtent.SetParent(transform);
			iKExtent.position = transform.position + transform.forward * ikExtentOffset;
		}
		startRotation = transform.eulerAngles;
	}

	void Update()
	{
		CheckTarget();
		NotifyParent();
		RotateTowardsTarget();
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawLine(transform.position, (transform.position + transform.forward * ikExtentOffset));
		Gizmos.DrawWireSphere(transform.position + transform.forward * ikExtentOffset, .2f);
	}

	void RotateTowardsTarget()
	{
		if (child != null && child.TargetReached)
		{
			Debug.Log("Target reached by child, returning: " + name);
			return;
		}
		if (restrictParent)
		{
			Debug.Log("Restricted by child: " + name);
			return;
		}
		if (target != null)
		{
			if (RestrictRotation())
			{
				Debug.Log("Rotation restricted: " + name);
				return;
			}

			if (retractArm)
			{
				Debug.Log("Retracting Arm: " + name);
				Vector3 direction = (transform.position - target.position).normalized;
				Quaternion rotation = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
			}
			else
			{
				Vector3 direction = (target.position - transform.position).normalized;
				Quaternion rotation = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
			}
		}
	}

	void CheckTarget()
	{
		if (Vector3.Distance(iKExtent.position, target.position) <= .5f)
		{
			Debug.Log("Target reached by: " + name);
			targetReached = true;
		}
		else
		{
			targetReached = false;
		}
	}

	void NotifyParent()
	{
		if (parent != null)
		{
			if (child != null)
			{
				if (child.TargetReached || child.RestrictParent)
				{
					if (!child.RetractArm)
					{
						parent.RestrictParent = true;
						parent.RetractArm = false;
					}
					else
					{
						parent.RestrictParent = false;
						parent.RetractArm = true;
					}
				}
				else
				{
					parent.RestrictParent = false;
					parent.RetractArm = child.RetractArm;
				}
			}
			else
			{
				Vector3 dirToTarget = (target.position - transform.position).normalized;

				if (targetReached || !RestrictRotation() || Vector3.Dot(transform.forward, dirToTarget) < .97f)
				{
					parent.RestrictParent = true;
					if (MissedTarget())
						parent.RetractArm = true;
					else
						parent.RetractArm = false;
				}
				else if (!targetReached && (RestrictRotation() || Vector3.Dot(transform.forward, dirToTarget) >= .97f))
				{
					parent.RestrictParent = false;
					if (MissedTarget())
						parent.RetractArm = true;
					else
						parent.RetractArm = false;
				}
			}
		}
	}

	bool RestrictRotation()
	{
		Debug.Log("Angle between: " + name + " : " + (Vector3.Angle(startRotation, transform.eulerAngles) >= maxRotationAngle));
		if (Vector3.Angle(startRotation, transform.eulerAngles) >= maxRotationAngle)
		{
			return true;
		}
		return false;
	}

	bool MissedTarget()
	{
		if (child != null)
			return false;

		float boneToTargetDist = Vector3.Distance(transform.position, target.position);
		float boneToExtentDist = Vector3.Distance(transform.position, iKExtent.position);

		Vector3 boneToTargetDir = (target.position - transform.position).normalized;
		Vector3 boneToExtentDir = (iKExtent.position - transform.position).normalized;

		float dot = Vector3.Dot(boneToTargetDir, boneToExtentDir);

		if (dot >= .98f)
		{
			//* Missed target
			if (boneToTargetDist < boneToExtentDist)
			{
				Debug.Log("Missed target");
				return true;
			}
			Debug.Log("Not Missed target");
			return false;
		}
		Debug.Log("Not looking at target");
		return false;
	}
}