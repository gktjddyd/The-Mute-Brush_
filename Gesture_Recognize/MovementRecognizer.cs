using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using PDollarGestureRecognizer;
using System.IO;
using UnityEngine.Events;
using System.Xml.Serialization;

public class MovementRecognizer : MonoBehaviour
{
    public XRNode inputSource;
    public UnityEngine.XR.Interaction.Toolkit.InputHelpers.Button inputButton;

    public XRNode inputSource2;
    public UnityEngine.XR.Interaction.Toolkit.InputHelpers.Button inputButton2;
    public float inputThreshold = 0.1f;
    public Transform movementSource;

    public float newPositionThresholdDistance = 0.05f;
    public GameObject debugCubePrefab;
    public bool creationMode = true;
    public string newGestureName;
    public float recognitionThreshold = 0.9f;

    [System.Serializable]
    public class UnityStringEvent : UnityEvent<string> { }
    public UnityStringEvent OnRecognized;

    private List<Gesture> trainingSet = new List<Gesture>();
    private bool isMoving = false;
    private List<Vector3> positionList = new List<Vector3>();
    public Result result;

    void Start()
    {
#if UNITY_EDITOR
        // 에디터 환경: GestureData 폴더에서 불러오기
        string gesturePath = Application.dataPath + "/GestureData";
        if (!Directory.Exists(gesturePath))
        {
            Directory.CreateDirectory(gesturePath);
        }

        string[] gestureFiles = Directory.GetFiles(gesturePath, "*.xml");
        foreach (var item in gestureFiles)
        {
            trainingSet.Add(GestureIO.ReadGestureFromFile(item));
        }

        Debug.Log($"에디터에서 불러온 제스처 수: {trainingSet.Count}");
#else
        // 빌드 환경: Resources/GestureData에서 불러오기
        TextAsset[] files = Resources.LoadAll<TextAsset>("GestureData");
        foreach (var ta in files)
        {
            Gesture g = GestureIO.ReadGestureFromXML(ta.text);
            if (g != null && g.Points != null && g.Points.Length > 0)
                trainingSet.Add(g);
        }

        Debug.Log($"빌드에서 불러온 제스처 수: {trainingSet.Count}");
#endif
    }

    void Update()
    {
        UnityEngine.XR.Interaction.Toolkit.InputHelpers.IsPressed(
            InputDevices.GetDeviceAtXRNode(inputSource),
            inputButton,
            out bool isPressed,
            inputThreshold
        );

         UnityEngine.XR.Interaction.Toolkit.InputHelpers.IsPressed(
            InputDevices.GetDeviceAtXRNode(inputSource2),
            inputButton2,
            out bool isPressed2,
            inputThreshold
         );

        if (!isMoving && (isPressed||isPressed2))
        {
            StartMovement();
        }
        else if (isMoving && !(isPressed||isPressed2))
        {
            EndMovement();
        }
        else if (isMoving && (isPressed||isPressed2))
        {
            UpdateMovement();
        }
    }

    void StartMovement()
    {
        isMoving = true;
        positionList.Clear();
        positionList.Add(movementSource.position);

        if (debugCubePrefab)
        {
            Destroy(Instantiate(debugCubePrefab, movementSource.position, Quaternion.identity), 3);
        }
    }

    void EndMovement()
    {
        isMoving = false;

        Point[] pointArray = new Point[positionList.Count];
        for (int i = 0; i < positionList.Count; i++)
        {
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(positionList[i]);
            pointArray[i] = new Point(screenPoint.x, screenPoint.y, 0);
        }

        Gesture newGesture = new Gesture(pointArray);

#if UNITY_EDITOR
        if (creationMode)
        {
            newGesture.Name = newGestureName;
            trainingSet.Add(newGesture);

            string devPath = Application.dataPath + "/GestureData/" + newGestureName + ".xml";
            GestureIO.WriteGesture(pointArray, newGestureName, devPath);
            Debug.Log("저장됨: " + devPath);

            string resPath = Application.dataPath + "/Resources/GestureData/" + newGestureName + ".xml";
            GestureIO.WriteGesture(pointArray, newGestureName, resPath);
            Debug.Log("Resources 복사됨: " + resPath);

            return;
        }
#endif

        result = PointCloudRecognizer.Classify(newGesture, trainingSet.ToArray());

        if (result.Score > recognitionThreshold)
        {
            Debug.Log("인식됨: " + result.GestureClass + " (" + result.Score + ")");
            OnRecognized.Invoke(result.GestureClass);
        }
        else
        {
            Debug.Log("인식 실패: " + result.GestureClass + " (" + result.Score + ")");
        }
    }

    void UpdateMovement()
    {
        Vector3 lastPosition = positionList[positionList.Count - 1];
        if (Vector3.Distance(movementSource.position, lastPosition) > newPositionThresholdDistance)
        {
            positionList.Add(movementSource.position);
            if (debugCubePrefab)
            {
                Destroy(Instantiate(debugCubePrefab, movementSource.position, Quaternion.identity), 3);
            }
        }
    }

    
    public static Gesture ReadGestureFromTextAsset(string xmlContent)
    {
        using (StringReader reader = new StringReader(xmlContent))
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Gesture));
            return (Gesture)serializer.Deserialize(reader);
        }
    }
}
