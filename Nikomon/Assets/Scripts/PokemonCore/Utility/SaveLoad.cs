﻿using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PokemonCore.Utility
{
    public static class SaveLoad
    {
        public static string path=>Game.DataPath ;
        public static T Load<T>(string fileName,string dataFilePath="")
        {
            if (!fileName.Contains(".")) fileName += @".json";
            string filePath = "";
            if (string.IsNullOrEmpty(dataFilePath))
                filePath = path + fileName;
            else
                filePath = dataFilePath + fileName;
            
            
            if (!File.Exists(filePath)) return default(T);
            StreamReader sr = File.OpenText(filePath);
            string data = sr.ReadToEnd();
            sr.Close();
            T obj = JsonConvert.DeserializeObject<T>(data);
            return obj;
        }

        public static async Task SaveAsync<T>(string fileName, T data, string dataFilePath = "")
        {
            
        }
        
        public static void Save<T>(string fileName,T data,string dataFilePath="")
        {
            if (!fileName.Contains(".")) fileName += ".json";
            string filePath = "";
            if (string.IsNullOrEmpty(dataFilePath))
                filePath = path + fileName;
            else
                filePath = dataFilePath + fileName;
            FileStream fs;
            if (!File.Exists(filePath))
            {
                fs =File.Create(filePath);
            }
            else
            {
                File.Delete(filePath);
                fs =File.Open(filePath,FileMode.CreateNew,FileAccess.Write);
            }

            string jsonData =JsonConvert.SerializeObject(data);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(jsonData);
            sw.Flush();
            sw.Close();
            fs.Close();
            UnityEngine.Debug.Log($"{filePath} has saved!");
        }
    }
}