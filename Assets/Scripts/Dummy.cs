using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dummy : MonoBehaviour
{
    public GameObject camera;

    // Start is called before the first frame update
    void Start()
    {
        camera.GetComponent<CameraControl>().Translate(new Vector3(2, 0, 10), CameraControl.SmoothingType.EaseOut, 10000);
        camera.GetComponent<CameraControl>().Rotate(45f, CameraControl.SmoothingType.EaseOut, 10000);
    }
}
