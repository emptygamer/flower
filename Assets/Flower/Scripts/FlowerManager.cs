using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Flower{
    public class FlowerManager
    {
        public static FlowerManager Instance{
            get{
                if(_instance == null){
                    lock(_lock){
                        if(_instance == null){
                            _instance = new FlowerManager();
                        }
                    }
                }
                return _instance;
            }
        }
        private static readonly object _lock = new object();
        private static FlowerManager _instance = null;
        private Dictionary<string ,FlowerSystem> flowerSystemMap = new Dictionary<string, FlowerSystem>();
        public FlowerSystem GetFlowerSystem(string key){
            if(flowerSystemMap.ContainsKey(key) && flowerSystemMap[key] != null){
                return flowerSystemMap[key];
            }else{
                throw new Exception($"Get FlowerSystem failed. key - {key} : FlowerSystem not exists.");
            }
        }
        public FlowerSystem CreateFlowerSystem(string key, bool elementsDestroyOnLoad=true){
            if(flowerSystemMap.ContainsKey(key)){
                if(flowerSystemMap[key] != null){
                    throw new Exception($"Create FlowerSystem failed. key - {key} already exists.");
                }
            }
            GameObject obj = new GameObject("FlowerSystem");
            FlowerSystem flowerSys = obj.AddComponent<FlowerSystem>();
            if(!elementsDestroyOnLoad){
                UnityEngine.Object.DontDestroyOnLoad(obj);
                flowerSys.elementsDestroyOnLoad=false;
            }
            flowerSystemMap[key] = flowerSys;
            return flowerSys;
        }
    }
}
