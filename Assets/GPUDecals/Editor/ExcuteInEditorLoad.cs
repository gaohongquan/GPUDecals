using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yunchang
{
    [InitializeOnLoad]
    public class ExcuteInEditorLoad
    {
        static ExcuteInEditorLoad()
        {
            EditorApplication.delayCall += NoEmulation;
            EditorApplication.update += EditorUpdate;
        }

        static void NoEmulation()
        {
            EditorApplication.ExecuteMenuItem("Edit/Graphics Emulation/No Emulation");
            EditorApplication.delayCall -= NoEmulation;
        }

        static void EditorUpdate()
        {
            var cams = UnityEditor.SceneView.GetAllSceneCameras();
            foreach (var cam in cams)
            {
                var ycamera = cam.gameObject.GetComponent<RendererFeatures>();
                if (ycamera == null)
                    cam.gameObject.AddComponent<RendererFeatures>();
            }
        }
    }

}
