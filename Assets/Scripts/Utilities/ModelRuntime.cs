using System.Collections.Generic;
using Unity.Sentis;
using UnityEditor;
using UnityEngine;

namespace kbradu
{
    public enum Device
    {
        CPU,
        GPU
    }
    public enum ModelAssetType
    {
        ONNX,
        Sentis
    }
    public class ModelRuntime : MonoBehaviour
    {
        [Header("This script runs one auto-disposable worker.")]
        [Tooltip("What extension has the model asset file. Serialize them to .sentis for faster load times.")]
        public ModelAssetType extension = ModelAssetType.ONNX;
        public DefaultAsset modelSentis;
        public ModelAsset modelONNX;
        public Device device = Device.CPU;

        private Model model_runtime;
        private IWorker worker;

        private void Start()
        {
            if (extension == ModelAssetType.ONNX && modelONNX == null)
            {
                Debug.LogError("Please load an onnx model script.");
            }
            else if (extension == ModelAssetType.Sentis && modelSentis == null)
            {
                Debug.LogError("Please load a sentis model script.");
            }

            Debug.Log(Application.streamingAssetsPath + $"/{modelSentis.name}.sentis");

            if (extension == ModelAssetType.ONNX)
                model_runtime = ModelLoader.Load(modelONNX);
            else if (extension == ModelAssetType.Sentis)
                model_runtime = ModelLoader.Load(Application.streamingAssetsPath + $"/{modelSentis.name}.sentis");
          
            worker = WorkerFactory.CreateWorker(device == Device.GPU ? BackendType.GPUCompute : BackendType.CPU, model_runtime);
        }

        public Tensor Forward(Tensor input)
        {
            if (worker == null)
                throw new System.Exception("Worker was disposed");

            worker.Execute(input);

            Tensor output = worker.PeekOutput();

            if (device == Device.GPU)
            {
                input.CompleteOperationsAndDownload();
                output.CompleteOperationsAndDownload();
            }
                
            return output;
        }


        private void OnDestroy()
        {
            worker?.Dispose();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ModelRuntime), true), CanEditMultipleObjects]
    sealed class ONNXRuntimeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            List<string> dontDrawMe = new List<string> { "m_Script" };

            ModelRuntime script = target as ModelRuntime;
            if (script.extension == ModelAssetType.ONNX)
                dontDrawMe.Add("modelSentis");
            else
                dontDrawMe.Add("modelONNX");

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, dontDrawMe.ToArray());
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}

