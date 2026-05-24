using UnityEngine;
using System.Collections;

public class CameraFollowScript : MonoBehaviour 
{
	[SerializeField]
	GameObject PlayerCar;
	bool Follow;

	[SerializeField]
	float OffSetX;
	[SerializeField]
	float OffSetY;

	void Start () 
	{
		Follow = true;
	}

	void FixedUpdate () 
	{
		if(Follow)
		transform.position = new Vector3 (PlayerCar.transform.position.x + OffSetX, PlayerCar.transform.position.y + OffSetY, -10.0f);	
	}
}
