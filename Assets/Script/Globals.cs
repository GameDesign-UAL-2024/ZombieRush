using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class Globals : MonoBehaviour
{
    public static Globals Instance { get; private set; }
    
    public class Datas 
    {
        public static string gridPrefabAddress = "Prefabs/Grid";
        public static string gridObjectAddress = "Prefabs/Objects";
        public int seed;
        public Datas()
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string timestampStr = timestamp.ToString();
            string lastFourDigits = timestampStr.Length >= 4 ? timestampStr.Substring(timestampStr.Length - 4) : timestampStr;

            // 转换为整数
            seed = int.Parse(lastFourDigits);
        }
    }

    public class Events 
    {
        public void GameStart()
        {
            Addressables.LoadAssetAsync<GameObject>(Globals.Datas.gridPrefabAddress).Completed += OnGridPrefabLoaded;
        }
        private void OnGridPrefabLoaded(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject gridPrefab = handle.Result;
                if (gridPrefab != null)
                {
                    // 实例化 Prefab 到场景中
                    Instantiate(gridPrefab, new Vector3(0,0,10), Quaternion.identity);
                }
                else
                {
                    Debug.LogError("Grid Prefab is null.");
                }
            }
            else
            {
                Debug.LogError("Failed to load the Grid Prefab.");
            }
        }
    }
    public Datas Data { get; private set; }
    public Events Event { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Data = new Datas();
            Event = new Events();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
