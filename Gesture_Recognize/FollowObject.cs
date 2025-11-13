using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 rotationOffset; // 인스펙터에서 오프셋 조정
    [SerializeField] private bool freezeYRotation = false;

    private void Update()
    {
        if (cameraTransform == null) return;

        // 카메라 방향 벡터 계산
        Vector3 direction = cameraTransform.position - transform.position;

        // Y축 고정 회전 옵션
        if (freezeYRotation)
        {
            direction.y = 0;
        }

        // 기본 회전 방향
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = lookRotation * Quaternion.Euler(rotationOffset);
        }
    }
}
