using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;

[System.Serializable]
public class Dialogue {
    [TextArea]
    public string dialogue;
}

public class DialogueSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txt_Dialogue;
    [SerializeField] private Dialogue[] dialogue;

    [Header("XR 입력 설정")]
    public XRNode inputSource;
    public InputHelpers.Button inputButton;
    public float inputThreshold = 0.1f;

    [Header("페이드 효과 설정 (타자 효과 사용 안할 때)")]
    [SerializeField] private float fadeInDuration = 5f;     // 페이드 인 시간
    [SerializeField] private float stayDuration = 2f;      // 완전 보이는 시간
    [SerializeField] private float fadeOutDuration = 1.3f; // 페이드 아웃 시간

    [Header("타자 효과 설정 (한글자씩 나타내기)")]
    [SerializeField] private bool useTypewriterEffect = false; // true: 한글자씩, false: 전체 텍스트 페이드 효과
    [SerializeField] private float letterDelay = 0.05f;        // 각 글자 사이 딜레이

    [Header("대화 시작 전 지연")]
    [SerializeField] private float preDialogueDelay = 0f;      // 첫 대사가 나오기 전 대기 시간

    private bool isDialogue = false;
    private int count = 0;
    private bool wasPressed = false;

    // 현재 실행중인 코루틴
    private Coroutine currentDialogueCoroutine;

    // 대화 종료 여부를 외부에서 확인하기 위한 public 변수
    public bool dialogueFinished = false;
    

    // 대화를 시작하고 싶을 때 외부에서 호출할 수 있는 메서드
   
    public void ShowDialogue()
    {
        txt_Dialogue.gameObject.SetActive(true);
        count = 0;
        isDialogue = true;
        dialogueFinished = false; // 대화 시작 시 false로 초기화

        // 이미 돌아가고 있는 코루틴이 있으면 중단
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
        }

        // 첫 대사 나오기 전까지 대기하는 코루틴 시작
        currentDialogueCoroutine = StartCoroutine(StartDialogueAfterDelay());
    }


    /// 첫 대사 전 딜레이를 두고, 그 후 NextDialogue()를 호출하는 코루틴
    private IEnumerator StartDialogueAfterDelay()
    {
        // 첫 대사 시작 전 설정된 시간만큼 대기
        yield return new WaitForSeconds(preDialogueDelay);

        // 첫 대사를 표시
        NextDialogue();
    }


    /// 모든 대사를 마친 후 대화창 숨기기
    private void HideDialogue()
    {
        txt_Dialogue.gameObject.SetActive(false);
        isDialogue = false;
        dialogueFinished = true; // 모든 대화가 끝나면 true로 설정
    }

  
    /// 다음 대사를 실행 (기존 실행 중인 코루틴 있으면 중단)
    public void NextDialogue()
    {
        // 이전에 진행중이던 코루틴이 있다면 중단
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
            currentDialogueCoroutine = null;
        }

        // 아직 남은 대사가 있다면...
        if (count < dialogue.Length)
        {
            string dialogueText = dialogue[count].dialogue;
            count++;

            // 설정에 따라 타입라이터 효과 혹은 페이드 효과 사용
            if (useTypewriterEffect)
            {
                currentDialogueCoroutine = StartCoroutine(DisplayDialogueTypewriter(dialogueText));
            }
            else
            {
                currentDialogueCoroutine = StartCoroutine(DisplayDialogueFade(dialogueText));
            }
        }
        else
        {
            // 모든 대사가 끝났다면 대화창 숨김
            HideDialogue();
        }
    }

    // 전체 텍스트 페이드 효과 대사 표시 코루틴
    IEnumerator DisplayDialogueFade(string dialogueText)
    {
        // 초기 알파값 0 설정 및 텍스트 갱신
        SetAlpha(0f);
        txt_Dialogue.text = dialogueText;

        // 페이드 인 효과
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeInDuration);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(1f);

        // 텍스트가 완전히 보이는 시간 유지
        float stayTimer = 0f;
        while (stayTimer < stayDuration)
        {
            stayTimer += Time.deltaTime;
            yield return null;
        }

        // 페이드 아웃 효과
        timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(timer / fadeOutDuration);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(0f);

        currentDialogueCoroutine = null;
        NextDialogue(); // 자동으로 다음 대사 진행
    }

    // 한글자씩 대사를 표시하는 코루틴
    IEnumerator DisplayDialogueTypewriter(string dialogueText)
    {
        txt_Dialogue.text = "";
        SetAlpha(1f); // 바로 보이도록

        // 한 글자씩 출력
        foreach (char letter in dialogueText)
        {
            txt_Dialogue.text += letter;
            yield return new WaitForSeconds(letterDelay);
        }

        // 전체 텍스트가 출력된 후 지정 시간 대기
        float stayTimer = 0f;
        while (stayTimer < stayDuration)
        {
            stayTimer += Time.deltaTime;
            yield return null;
        }

        // 원한다면 출력된 텍스트를 제거하거나 다른 효과를 줄 수 있음
        txt_Dialogue.text = "";

        currentDialogueCoroutine = null;
        NextDialogue(); // 자동으로 다음 대사 진행
    }

    // 텍스트의 알파값을 설정하는 헬퍼 함수
    private void SetAlpha(float alpha)
    {
        Color c = txt_Dialogue.color;
        c.a = alpha;
        txt_Dialogue.color = c;
    }

    void Start()
    {
        ShowDialogue();
    }

    void Update()
    {
        // XR 디바이스 버튼 입력 확인
        bool isPressed = false;
        InputHelpers.IsPressed(
            InputDevices.GetDeviceAtXRNode(inputSource),
            inputButton,
            out isPressed,
            inputThreshold
        );

        if (isDialogue)
        {
            // 버튼 상승 에지(새로 눌렸을 때) 감지
            bool buttonDown = isPressed && !wasPressed;
            if (buttonDown || Input.GetKeyDown(KeyCode.Space))
            {
                NextDialogue();
            }
            wasPressed = isPressed;
        }
    }
}
