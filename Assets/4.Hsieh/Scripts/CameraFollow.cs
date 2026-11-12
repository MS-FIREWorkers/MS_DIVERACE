using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    [SerializeField]
    private Vector3 offset = new Vector3(0.0f, 300.0f, -30.0f);

    private void LateUpdate()
    {
        if (target == null)
            return;

        transform.position = target.position + offset;

        // ‘Š‹@ŒÅ’è‰ +Z ŠÅ
        transform.rotation = Quaternion.identity;
    }
}