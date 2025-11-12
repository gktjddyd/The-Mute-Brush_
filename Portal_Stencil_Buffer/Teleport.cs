using System.Collections;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    [Header("이동할 오브젝트 (플레이어 등)")]
    public GameObject objectToMove;

    [Header("도착 지점")]
    public Transform outPortal;

    [Header("이동 시간 (초)")]
    public float moveDuration = 1.0f;

    [Header("회전 오프셋 옵션")]
    public bool addYRotationOffset = false; // Y축 회전 90도 추가 여부

    private bool isMoving = false;

    public void MoveThroughPortal()
    {
        Debug.Log("텔레포트 실행");
        if (!isMoving && outPortal != null && objectToMove != null)
        {
            StartCoroutine(SmoothMove());
        }
    }

    private IEnumerator SmoothMove()
    {
        isMoving = true;
        float elapsedTime = 0f;

        Vector3 startPos = objectToMove.transform.position;
        Quaternion startRot = objectToMove.transform.rotation;

        Vector3 targetPos = outPortal.position;

        // 회전값 설정
        Quaternion targetRot = startRot;

        if (addYRotationOffset)
        {
            // startRot 기준으로 Y축 회전 +90도
            targetRot = Quaternion.Euler(0, 180f, 0);
        }

        if (objectToMove.GetComponent<Camera>() != null ||
            objectToMove.GetComponentInChildren<Camera>() != null)
        {
            objectToMove.transform.position = targetPos;
            objectToMove.transform.rotation = targetRot;
            isMoving = false;
            yield break;
        }

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / moveDuration);

            objectToMove.transform.position = Vector3.Lerp(startPos, targetPos, t);
            objectToMove.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        objectToMove.transform.position = targetPos;
        objectToMove.transform.rotation = targetRot;

        isMoving = false;
    }
}
