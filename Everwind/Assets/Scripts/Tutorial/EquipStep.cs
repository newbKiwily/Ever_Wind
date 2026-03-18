using System;


public class EquipStep : ITutorialStep
{
    private Action _deleteAction;
    private InputManager InnputManager => SingletonManager.Instance.GetSingleton<InputManager>();

    public void EnterStep(TutorialGuide step, TextRenderManager textRenderManager)
    {
        // Craft 성공 시 ClearEvent 실행
        _deleteAction += () => ClearEvent(step, textRenderManager);
        EquipmentUI.EquipTClear += _deleteAction;

        // 튜토리얼 텍스트 시작
        textRenderManager.StartShow("EquipT");
        textRenderManager.AutoShow(0, 2);
    }

    public void UpdateStep(TutorialGuide step, TextRenderManager textRenderManager, InputManager inputManager)
    {
        // 아직 이벤트가 살아있으면 기다림
        if (_deleteAction != null)
            return;

        // 텍스트가 모두 끝나지 않은 경우
        if (!textRenderManager.IsDoneShowingText())
            return;

        // 아무 키 입력 시 다음 튜토리얼로
        if (inputManager.AnyKeyDownExcludeMouse())
        {
            // Craft 튜토리얼이 마지막이면 종료 or 다음 단계 이동
            // 여기서는 예시로 Interact 이후 Combat → Craft 순서라면 끝으로 간주
            // 필요하면 TutorialStep 다음 단계로 변경
            // step.TransitionStep(TutorialStep.SomeNextStep);
        }
    }

    public void ExitStep(TutorialGuide step)
    {
        // 이벤트 정리
        if (_deleteAction != null)
            EquipmentUI.EquipTClear -= _deleteAction;
        _deleteAction = null;
    }

    public void ClearEvent(TutorialGuide step, TextRenderManager textRenderManager)
    {
        // 성공 메시지 출력
        textRenderManager.AutoShow(3, 5);

        // 이벤트 해제
        if (_deleteAction != null)
            EquipmentUI.EquipTClear -= _deleteAction;
        _deleteAction = null;
    }
}