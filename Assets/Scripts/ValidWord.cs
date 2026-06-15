using UnityEngine;
using System.Collections.Generic;


public class ValidWord : MonoBehaviour
{
    
    [SerializeField] public static HashSet<string> Words;    

    void Awake()
    {
        LoadDictionary();
    }

    void LoadDictionary()
    {
        Words = new HashSet<string>();

        TextAsset wordFile = Resources.Load<TextAsset>("words");
        string[] lines = wordFile.text.Split('\n');

        foreach (string line in lines)
        {
            Words.Add(line.Trim().ToLower());
        }
    }

    public static bool IsValidWord(string word)
    {
        return Words.Contains(word.ToLower());
    }
}

