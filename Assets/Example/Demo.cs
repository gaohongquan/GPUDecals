using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Demo : MonoBehaviour
{
    public Text fpsText;
    public Camera camera;
    public Button leftButton;
    public Button rightButton;

    int iFrameCount;
    float fDeltaTimeSum;

    // Start is called before the first frame update
    void Start()
    {
        leftButton.onClick.AddListener(() =>
        {
            camera.transform.rotation *= Quaternion.AngleAxis(5, Vector3.up);
        });
        rightButton.onClick.AddListener(() =>
        {
            camera.transform.rotation *= Quaternion.AngleAxis(-5, Vector3.up);
        });
        iFrameCount = 0;
        fDeltaTimeSum = 0;
    }

    // Update is called once per frame
    void Update()
    {
        fDeltaTimeSum += Time.unscaledDeltaTime;
        iFrameCount++;
        if (fDeltaTimeSum > 1.0f)
        {
            fpsText.text = string.Format("FPS:{0}", iFrameCount);
            iFrameCount = 0;
            fDeltaTimeSum = fDeltaTimeSum - 1.0f;
        }
    }

}
