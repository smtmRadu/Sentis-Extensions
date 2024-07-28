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
    public class ONNXRuntime : MonoBehaviour
    {
        [Header("This script runs one auto-disposable worker.")]
        public ModelAsset model;
        public Device device = Device.CPU;

        private Model model_runtime;
        private IWorker worker;

        private void Start()
        {
            if (model == null)
            {
                Debug.LogError("Please load a model for the ONNXRuntime script.");
            }

            model_runtime = ModelLoader.Load(model);
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
    [CustomEditor(typeof(ONNXRuntime), true), CanEditMultipleObjects]
    sealed class ONNXRuntimeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            List<string> dontDrawMe = new List<string> { "m_Script" };
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, dontDrawMe.ToArray());
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}

