using System.IO;

namespace HffArchipelagoClient
{
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class Barrier
    {
        private static GameObject barrierBase;
        public static Barrier[] barriers;

        private GameObject barrierObject;

        static Barrier()
        {
            // Code based on Permamiss's SpeedTools
            // https://github.com/Permamiss/HFF_SpeedTools/blob/master/HFF_SpeedTools/SpeedTools.cs
            barrierBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrierBase.name = "Barrier Base Object";
            barrierBase.layer = LayerMask.NameToLayer("NoGrab");
            barrierBase.tag = "NoGrab";
            Renderer barrierRenderer = barrierBase.GetComponent<Renderer>();
            StandardShaderUtils.ChangeRenderMode(barrierRenderer.material, StandardShaderUtils.BlendMode.Transparent);
            barrierRenderer.material.SetColor("_Color", new Color(0.31f, 0.91f, 1.0f, 0.33f));
            barrierRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            barrierRenderer.receiveShadows = false;
            barrierBase.transform.localScale = Vector3.one;
            barrierBase.GetComponent<Collider>().enabled = true;
            GameObject.DontDestroyOnLoad(barrierBase);
            barrierBase.SetActive(false);
        }

        public Barrier(string name, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            barrierObject = GameObject.Instantiate(barrierBase);
            barrierObject.name = name;
            barrierObject.transform.localPosition = position;
            barrierObject.transform.localEulerAngles = rotation;
            barrierObject.transform.localScale = scale;
            barrierObject.SetActive(true);
        }

        public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            using (FileStream stream = new FileStream(Path.Combine(BepInEx.Paths.PluginPath, "barriers.txt"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                StreamReader reader = new StreamReader(stream);

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] words = line.Trim().Split();
                    if (scene.path == words[0])
                        new Barrier(words[1], new Vector3(float.Parse(words[2]), float.Parse(words[3]), float.Parse(words[4])),
                                              new Vector3(float.Parse(words[5]), float.Parse(words[6]), float.Parse(words[7])),
                                              new Vector3(float.Parse(words[8]), float.Parse(words[9]), float.Parse(words[10])));
                }
            }
        }
    }
}
