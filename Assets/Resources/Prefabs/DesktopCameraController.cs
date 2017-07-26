using UnityEngine;
using System.Collections;

public class DesktopCameraController : MonoBehaviour
{
    static float xSpeed = 2.0f, ySpeed = 2.0f;

    void Update()
    {
        Quaternion rx = Quaternion.AngleAxis(xSpeed * Input.GetAxis("Mouse X"), Vector3.up);
        Quaternion ry = Quaternion.AngleAxis(-ySpeed * Input.GetAxis("Mouse Y"), Camera.main.transform.right);
        Camera.main.transform.rotation = rx * ry * Camera.main.transform.rotation;

        Vector3 forwardProj = Camera.main.transform.forward;
        forwardProj.y = 0.0f;
        forwardProj.Normalize();
        Vector3 rightProj = Camera.main.transform.right;
        rightProj.y = 0.0f;
        rightProj.Normalize();

        if (Input.GetKey(KeyCode.UpArrow))
        {
            Camera.main.transform.position += forwardProj * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            Camera.main.transform.position -= forwardProj * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            Camera.main.transform.position -= rightProj * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            Camera.main.transform.position += rightProj * Time.deltaTime;
        }
    }
}
