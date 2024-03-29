using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

public abstract class ModelLoaderBase : MonoBehaviour {
  public string ModelPath;
  public string ModelsDir;
  public Action<LlamaConfig, WeightsGpu, Tokenizer> OnLoaded;
  
  public Tokenizer Tokenizer { get; protected set; }

  public bool IsLoaded { get; private set; }
  private Task<(LlamaConfig, WeightsGpu, Tokenizer)> _task;
  
  public void RequestLoad() {
    Debug.Assert(!IsLoaded && _task == null);
    _task = LoadModelImpl();
  }
  
  IEnumerator ExploreDirectory(Action<string> callback)
  {
    // Path to the directory you want to explore
    // Check if the directory exists
    if (Application.platform == RuntimePlatform.Android)
    {
      ModelsDir = "jar:file://" + Application.dataPath + "!/assets";
      Debug.Log("Middle BEGIN");

      // Android needs special handling due to APK packaging
      Debug.Log("ANDROID1: " + ModelsDir);
      // Use Unity's WWW class for local file access
      using (UnityWebRequest www = new UnityWebRequest(ModelsDir))
      {
        yield return www;
        Debug.Log("ANDROID2: " + www.result);
        Debug.Log("ANDROID3: " + www.error);
        if (string.IsNullOrEmpty(www.error))
        {
          string[] files = www.result.ToString().Split('\n');

          // Output file names to the console
          foreach (string file in files)
          {
            Debug.Log("File: " + file);
          }
          Debug.Log("Middle END");

          // Invoke the callback with the result
          callback(Path.Combine(ModelsDir,ModelPath));
        }
        else
        {
          Debug.Log("Failed to read directory: " + ModelsDir);
          Debug.Log("Failed to read directory: " + www.error);
          // Invoke the callback with an empty string to indicate failure
          callback("");
        }
      }
    }
    else
    {
      ModelsDir = Application.streamingAssetsPath;
      Debug.Log("Middle BEGIN");

      // For other platforms, you can use Directory.Exists and Directory.GetFiles
      if (Directory.Exists(ModelsDir))
      {
        // Get all files in the directory
        string[] files = Directory.GetFiles(ModelsDir);

        // Output file names to the console
        foreach (string file in files)
        {
          Debug.Log("File: " + file);
        }
        Debug.Log("Middle END");

        // Invoke the callback with the result
        callback(Path.Combine(ModelsDir,ModelPath));
      }
      else
      {
        Debug.Log("Directory not found: " + ModelsDir);
        // Invoke the callback with an empty string to indicate failure
        callback("");
      }
    }
  }

  public void GetFullModelPath(Action<string> callback) {
    StartCoroutine(ExploreDirectory(callback));

  }

  public string staticStreamingAssetsPath()
  {
    if (Application.platform == RuntimePlatform.Android)
    {
      
      ModelsDir = "jar:file://" + Application.dataPath + "!/assets";
      return Path.Combine(ModelsDir, ModelPath);
    }
    else
    {
      ModelsDir = Application.streamingAssetsPath;
      return Path.Combine(ModelsDir, ModelPath);
    }

    
  }

  private void Awake()
  {
  }

  void Update() {
    // TODO: Just make this a continuation on Unity main thread when we add that.
    if (_task != null && _task.IsCompleted) {
      IsLoaded = true;
      (var config, var weights, var tokenizer) = _task.Result;
      Tokenizer = tokenizer;
      OnLoaded?.Invoke(config, weights, tokenizer);
      _task = null;
    }
  }

  protected abstract Task<(LlamaConfig, WeightsGpu, Tokenizer)> LoadModelImpl();
}
