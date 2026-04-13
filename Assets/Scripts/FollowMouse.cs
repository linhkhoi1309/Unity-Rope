using UnityEngine;

public class FollowMouse : MonoBehaviour
{
    [SerializeField] private GameObject _objectToFollow;
    void Start()
    {
        
    }

    void Update()
    {
        if (_objectToFollow != null)
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = 10f; // Set a fixed distance from the camera
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            _objectToFollow.transform.position = worldPosition;
        }
    }
}
