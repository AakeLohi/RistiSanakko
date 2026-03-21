using System.Drawing;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    [SerializeField] private float lerpSpeed = 5f;

    private Vector3 targetPos;

    public float targetSize = 6f;

    void Update()
    {
        if (transform.position != targetPos + offset)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos + offset, Time.deltaTime * lerpSpeed);
        }
        if (Camera.main.orthographicSize != targetSize)
        {
            Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, targetSize, Time.deltaTime * lerpSpeed);
        }
    }

    public void SetOrthoSize(float size)
    {
        targetSize = size;
    }

    public void MoveTo(Vector3 pos)
    {
        targetPos = pos;
    }
}
