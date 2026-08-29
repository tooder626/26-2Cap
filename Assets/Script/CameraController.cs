using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject leftCamera;
    [SerializeField] private GameObject rightCamera;
    [SerializeField] private GameObject myCamera;


    public void ToggleLeft()
    {
        // 2. 왼쪽이 켜져 있으면 메인으로, 꺼져 있으면 왼쪽으로!
        if (leftCamera.activeSelf)
        {
            GameManager.Inst.currentCamPos = GameManager.CameraPosition.Mid;
            ActivateCamera(myCamera);
        }
        else
        {
            ActivateCamera(leftCamera);
            GameManager.Inst.currentCamPos = GameManager.CameraPosition.Left;
        }
    }

    public void ToggleRight()
    {

        // 2. 오른쪽이 켜져 있으면 메인으로, 꺼져 있으면 오른쪽으로!
        if (rightCamera.activeSelf)
        {
            GameManager.Inst.currentCamPos = GameManager.CameraPosition.Mid;
            ActivateCamera(myCamera);
        }
        else
        {
            GameManager.Inst.currentCamPos = GameManager.CameraPosition.Right;
            ActivateCamera(rightCamera);
        }
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