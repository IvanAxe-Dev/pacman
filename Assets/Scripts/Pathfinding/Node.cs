using UnityEngine;

public class Node
{
    public Vector3Int position;
    public bool isWalkable;
    public Node parent; 

    public int gCost; 
    public int hCost;
    public int fCost => gCost + hCost; 

    public Node(Vector3Int position, bool isWalkable)
    {
        this.position = position;
        this.isWalkable = isWalkable;
    }
}
