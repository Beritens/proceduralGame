using UnityEngine;
using System.Collections;

[System.Serializable]
public class TileData 
{
    [System.Serializable]
    public struct rowData
    {
        public int[] row;
    }

    public rowData[] rows = new rowData[0];
}