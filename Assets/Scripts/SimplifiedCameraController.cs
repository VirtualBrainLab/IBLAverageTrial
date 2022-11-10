using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SimplifiedCameraController : MonoBehaviour
{
    GraphicRaycaster m_Raycaster;
    PointerEventData m_PointerEventData;
    EventSystem m_EventSystem;

    [SerializeField] private GameObject brainUIGO;
    [SerializeField] private Transform _brainCameraRotatorT;
    [SerializeField] private Camera _brainCamera;

    [SerializeField] private RectTransform _positionPanelRT;

    [SerializeField] private EventAverageManager _eaManager;

    private const float SCROLL_SPEED = 3f;
    private const float minFoV = 30f;
    private const float maxFoV = 60f;

    private const float ROTATE_SPEED = 100f;

    private float totalPitch = 45f;
    private float totalSpin = -45f;

    private bool mouseDown;

    // Start is called before the first frame update
    void Start()
    {
        //Fetch the Raycaster from the GameObject (the Canvas)
        m_Raycaster = GetComponent<GraphicRaycaster>();
        //Fetch the Event System from the Scene
        m_EventSystem = GetComponent<EventSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        //Set up the new Pointer Event
        m_PointerEventData = new PointerEventData(m_EventSystem);
        //Set the Pointer Event Position to that of the mouse position
        m_PointerEventData.position = Input.mousePosition;

        //Create a list of Raycast Results
        List<RaycastResult> results = new List<RaycastResult>();

        //Raycast using the Graphics Raycaster and mouse click position
        m_Raycaster.Raycast(m_PointerEventData, results);

        //For every result returned, output the name of the GameObject on the Canvas hit by the Ray
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.CompareTag("MainBrain"))
            {
                float fov = _brainCamera.fieldOfView;

                float scroll = -Input.GetAxis("Mouse ScrollWheel");
                fov += scroll * SCROLL_SPEED;
                fov = Mathf.Clamp(fov, minFoV, maxFoV);

                _brainCamera.fieldOfView = fov;
                // Now check if the mouse wheel is being held down
                if (Input.GetMouseButtonDown(0))
                {
                    mouseDown = true;

                    // check for double click
                    //if ((Time.realtimeSinceStartup - lastClick) < 0.3f)
                    //    ResetRotation();
                    //lastClick = Time.realtimeSinceStartup;
                }
            }

            if (!mouseDown && result.gameObject.CompareTag("PositionPanel"))
            {
                // change time according to the position within the panel
                if (Input.GetMouseButton(0))
                {
                    Vector3 localPosition = _positionPanelRT.InverseTransformPoint(result.worldPosition);
                    _eaManager.UpdateIndex(1 - (Mathf.Abs(localPosition.x) / 1250));
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
            ResetRotation();

        RotateBrain();
    }

    private float lastClick;

    private void ResetRotation()
    {
        totalPitch = 45f;
        totalSpin = -45f;
        SetBrainRotation();
    }

    void RotateBrain()
    {
        if (Input.GetMouseButtonUp(0))
            mouseDown = false;

        if (mouseDown)
        {
            float xRot = Input.GetAxis("Mouse X") * ROTATE_SPEED * Time.deltaTime;
            float yRot = Input.GetAxis("Mouse Y") * ROTATE_SPEED * Time.deltaTime;

            if (xRot != 0 || yRot != 0)
            {
                totalPitch -= yRot;
                totalSpin += xRot;
                SetBrainRotation();
            }
        }
    }

    private void SetBrainRotation()
    {
        Quaternion curRotation = Quaternion.Euler(totalPitch, totalSpin, 0f);
        _brainCameraRotatorT.localRotation = curRotation;
    }
}
