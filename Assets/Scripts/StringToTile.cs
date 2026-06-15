using UnityEngine;
using UnityEngine.Tilemaps;

public class StringToTile : MonoBehaviour
{ 
    public GameObject[] tiles;
    
    public GameObject StringTile(string str)
    {
        int index = char.ToUpperInvariant(str[0]) - 'A';
        return tiles[index];
    }
}
