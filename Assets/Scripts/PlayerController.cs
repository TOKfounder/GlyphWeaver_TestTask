using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
	private Rigidbody rb;
	private Vector3 movement = Vector3.zero;
	[SerializeField] private float speed = 10f;

	void Start()
	{
		rb = GetComponent<Rigidbody>();
	}

	public void OnMove(InputValue value)
	{
		Vector2 input = value.Get<Vector2>();
		movement = new Vector3(input.x, 0f, input.y).normalized;
	}

	void FixedUpdate()
	{
		rb.AddForce(movement * speed);
	}
}