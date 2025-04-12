using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public static class FileStorage
{
    private static string filePath = Path.Combine(Application.persistentDataPath, "userData.json");

    public static UserData LoadData()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            Debug.Log(filePath);
            return JsonUtility.FromJson<UserData>(json);
        }
        else
        {
            // First time user: initialize default data
            UserData newUserData = new UserData();
            newUserData.userId = GenerateUserId();
            newUserData.starAmt = 0;
            newUserData.tileStates = GenerateDefaultTileStates();
            SaveData(newUserData);
            return newUserData;
        }
    }

    public static void SaveData(UserData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
    }

    public static void UpdateStarsInJSON(int newStarAmt)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            UserData userData = JsonUtility.FromJson<UserData>(json);

            userData.starAmt = newStarAmt;

            string updatedJson = JsonUtility.ToJson(userData, true);
            File.WriteAllText(filePath, updatedJson);
        }
    }

    public static string GenerateUserId()
    {
        return "user_" + Guid.NewGuid().ToString();
    }

    private static List<TileData> GenerateDefaultTileStates()
    {
        List<TileData> defaults = new List<TileData>();

        for (int x = -2; x <= 1; x++)
        {
            for (int y = -2; y <= 1; y++)
            {
                defaults.Add(new TileData
                {
                    x = x,
                    y = y,
                    state = GameManager.TileState.Empty
                });
            }
        }

        return defaults;
    }
}
