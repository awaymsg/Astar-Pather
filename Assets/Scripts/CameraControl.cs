using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [System.Serializable]
    public class Control
    {
        public float movementSpeed;
        public float rotationSpeed;
        public float zoomSpeed;
    }
    public Control control;

    #region zoom
    [System.Serializable]
    public class Distance
    {
        public float start;
        public float min;
        public float max;
    };
    public Distance distanceInfo;

    private float targetDistance;
    private float distance;

    public float zoomSmoothSpeed;

    [System.Serializable]
    public class AngleOfAttack {
        [Range(0f, 90f)]
        public float start;
        [Range(0f, 90f)]
        public float min;
        [Range(0f, 90f)]
        public float max;
    };
    public AngleOfAttack angleOfAttackControl;
    private float angleOfAttack;

    public GameObject boom;
    [Range(0f, 1f)]
    public float boomDistance;
    #endregion
    #region auto movement
    public enum SmoothingType
    {
        Instant,
        Linear,
        EaseOut
    }

    private Vector3 location = Vector3.zero;
    private Vector3 startLocation, targetLocation = Vector3.zero;
    private SmoothingType locationSmoothing;

    private float rotation = 0.0f;
    private float startRotation, targetRotation = 0.0f;
    private SmoothingType rotationSmoothing;

    private int lockedFlags = 0;
    private const int kTranslationLocked = 0x1;
    private const int kRotationLocked = 0x2;
    public bool Locked {
        get {
            return lockedFlags != 0;
        }
    }

    private float translateT, translateTInterval;
    private float rotateT, rotateTInterval;

    [Range(0.0f, 1.0f)]
    public float cubicSmoothing;

    public void Translate( Vector3 position, SmoothingType smoothing = SmoothingType.Instant, int timeMs = 1000)
    {
        startLocation = location;
        targetLocation = position;
        if (smoothing == SmoothingType.Instant)
        {
            location = position;
        }
        else
        {
            translateT = 0.0f;
            translateTInterval = 1.0f / ((float)timeMs * 0.001f);
            locationSmoothing = smoothing;
            lockedFlags |= kTranslationLocked;
        }
    }

    public void Rotate( float angle, SmoothingType smoothing = SmoothingType.Instant, int timeMs = 1000)
    {
        startRotation = rotation;
        targetRotation = angle;
        if (smoothing == SmoothingType.Instant)
        {
            rotation = angle;
        }
        else
        {
            rotateT = 0.0f;
            rotateTInterval = 1.0f / ((float)timeMs * 0.001f);
            rotationSmoothing = smoothing;
            lockedFlags |= kRotationLocked;
        }
    }

    void AutoTranslate()
    {
        switch (locationSmoothing)
        {
            case SmoothingType.Linear:
                location = V3Curve.Lerp(startLocation, targetLocation, translateT);
                break;
            case SmoothingType.EaseOut:
                var ctrlPt = V3Curve.Lerp(startLocation, targetLocation, cubicSmoothing);
                location = V3Curve.CubicInterp(startLocation, targetLocation, ctrlPt, translateT);
                break;
        }
        if (translateT < 1.0f)
        {
            translateT += translateTInterval * Time.smoothDeltaTime;
        }
        else if ((lockedFlags & kTranslationLocked) > 0)
        {
            lockedFlags &= ~kTranslationLocked;
        }
    }

    void AutoRotate()
    {
        switch (rotationSmoothing)
        {
            case SmoothingType.Linear:
                rotation = Mathf.Lerp(startRotation, targetRotation, rotateT);
                break;
            case SmoothingType.EaseOut:
                rotation = Float.CubicInterp(
                    startRotation,
                    targetRotation,
                    Mathf.Lerp(
                        startRotation,
                        targetRotation,
                        cubicSmoothing
                    ), 
                    rotateT
                );
                break;
        }
        if (rotateT < 1.0f)
        {
            rotateT += rotateTInterval * Time.smoothDeltaTime;
        }
        else if ((lockedFlags & kRotationLocked) > 0)
        {
            lockedFlags &= ~kRotationLocked;
        }
    }
    #endregion

    private void Start()
    {
        angleOfAttack = angleOfAttackControl.start;
        distance = distanceInfo.start;
        targetDistance = distance;
        Instantiate(boom);

        var cam = GetComponent<Camera>();
        cam.orthographicSize = distance;
    }

    private void Update()
    {
        if (!Locked)
        {
            TranslationControl();
            RotationControl();
            ZoomControl();
        }
        else
        {
            AutoTranslate();
            AutoRotate();
        }
        LazyZoom();
    }

    void LateUpdate()
    {
        var cam = GetComponent<Camera>();

        Vector3 camBoom = new Vector3(0, 0, cam.orthographic ? -25 : -distance);
        Vector3 micBoom = boomDistance * camBoom;
        Quaternion rotationQ = Quaternion.Euler(angleOfAttack, rotation, 0);
        transform.position = location + rotationQ * camBoom;
        transform.LookAt(location);

        boom.transform.position = location + rotationQ * micBoom;
        boom.transform.LookAt(location);
    }

    #region controls
    void TranslationControl()
    {
        Vector3 movement = new Vector3(
        Input.GetAxis("Horizontal"),
            0,
            Input.GetAxis("Vertical")
        ).normalized;

        movement *= Time.deltaTime * control.movementSpeed;

        Quaternion rotationQ = Quaternion.Euler(0, rotation, 0);
        movement = rotationQ * movement;
        location += movement;
    }

    void RotationControl()
    {
        rotation -= Input.GetAxis("Rotate Camera") * Time.deltaTime * control.rotationSpeed;
    }

    void ZoomControl()
    {
        float scrollAmount = Input.GetAxis("Mouse ScrollWheel") * control.zoomSpeed;

        if (scrollAmount != 0)
        {
            if (targetDistance - scrollAmount <= distanceInfo.min)
            {
                targetDistance = distanceInfo.min;
            }
            else if (targetDistance - scrollAmount >= distanceInfo.max)
            {
                targetDistance = distanceInfo.max;
            }
            else
            {
                targetDistance -= scrollAmount;
            }
        }
    }

    void LazyZoom()
    {
        LazyInterp(ref distance, targetDistance, zoomSmoothSpeed);
        var t = (distance - distanceInfo.min) / (distanceInfo.max - distanceInfo.min);
        angleOfAttack = Mathf.Lerp(angleOfAttackControl.min, angleOfAttackControl.max, t);

        var cam = GetComponent<Camera>();
        cam.orthographicSize = distance;
    }

    void LazyInterp(ref float value, float targetValue, float smoothAmt)
    {
        if (value != targetValue)
        {
            var diff = targetValue - value;
            var smoothedDelta = smoothAmt * Time.deltaTime * Mathf.Sign(diff);
            if (Mathf.Abs(diff) < Mathf.Abs(smoothedDelta) && Mathf.Sign(diff) == Mathf.Sign(smoothedDelta))
            {
                value = targetValue;
            }
            else
            {
                value += smoothedDelta;
            }
        }
    }
    #endregion
}
