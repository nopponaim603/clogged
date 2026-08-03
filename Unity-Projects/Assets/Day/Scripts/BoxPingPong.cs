using UnityEngine;

public class BoxPingPong : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField] private float distance = 10f;
    [SerializeField] private Vector3 direction = Vector3.right;

    private Vector3 _startPosition;
    private float _currentDistance;
    private int _directionMultiplier = 1;

    private void Start()
    {
        _startPosition = transform.position;
    }

    private void Update()
    {
        _currentDistance += speed * _directionMultiplier * Time.deltaTime;

        if (_currentDistance >= distance)
        {
            _currentDistance = distance;
            _directionMultiplier = -1;
        }
        else if (_currentDistance <= 0f)
        {
            _currentDistance = 0f;
            _directionMultiplier = 1;
        }

        transform.position = _startPosition + direction.normalized * _currentDistance;
    }
}
