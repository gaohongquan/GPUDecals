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
    }

    // Update is called once per frame
    void Update()
    {
        fpsText.text = string.Format("FPS:{0:f2}", 1.0f / Time.unscaledDeltaTime);
    }
}
