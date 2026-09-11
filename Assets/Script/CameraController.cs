using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject leftCamera;
    [SerializeField] private GameObject rightCamera;
    [SerializeField] private GameObject myCamera;


    public void GoToMainCamera()
    {
        GameManager.Inst.currentCamPos = GameManager.CameraPosition.Mid;
        ActivateCamera(myCamera);
    }

    // 왼쪽 컵 뷰로 이동하는 전용 메서드
    public void GoToLeftCamera()
    {
        GameManager.Inst.currentCamPos = GameManager.CameraPosition.Left;
        ActivateCamera(leftCamera);
    }

    // 오른쪽 컵 뷰로 이동하는 전용 메서드
    public void GoToRightCamera()
    {
        GameManager.Inst.currentCamPos = GameManager.CameraPosition.Right;
        ActivateCamera(rightCamera);
    }


    private void ActivateCamera(GameObject targetCam)
    {
        leftCamera.SetActive(targetCam == leftCamera);
        rightCamera.SetActive(targetCam == rightCamera);
        myCamera.SetActive(targetCam == myCamera);

        leftCamera.tag = (targetCam == leftCamera) ? "MainCamera" : "Untagged";
        rightCamera.tag = (targetCam == rightCamera) ? "MainCamera" : "Untagged";
        myCamera.tag = (targetCam == myCamera) ? "MainCamera" : "Untagged";

        SpawnManager.Inst.currentCamera = targetCam.GetComponent<Camera>();
    }



}