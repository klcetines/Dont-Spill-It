using UnityEngine;
using System.Collections.Generic;

public class PathNode : MonoBehaviour
{
    public enum NodeType
    {
        [Tooltip("Node that gives resources")]
        Giver,
        [Tooltip("Node that drains resources")]
        Drainer,
        [Tooltip("Special node for resource collection")]
        Well
    }

    // Lista de nodos siguientes (para soportar bifurcaciones)
    public List<PathNode> nextNodes = new List<PathNode>();
    [SerializeField] private NodeType nodeType;

    // Nodo anterior (opcional, útil para recorrer el camino en reversa)
    public PathNode previousNode;

    // Método para agregar un nodo siguiente
    public void AddNextNode(PathNode node)
    {
        if (node != null && !nextNodes.Contains(node))
        {
            nextNodes.Add(node);
            node.previousNode = this; // Establecer este nodo como el anterior del siguiente
        }
    }

    // Método para eliminar un nodo siguiente
    public void RemoveNextNode(PathNode node)
    {
        if (nextNodes.Contains(node))
        {
            nextNodes.Remove(node);
            node.previousNode = null; // Eliminar la referencia al nodo anterior
        }
    }

    // Dibujar Gizmos en el editor para visualizar las conexiones
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.1f); // Dibujar una esfera en la posición del nodo

        // Dibujar líneas hacia los nodos siguientes
        Gizmos.color = Color.blue;
        foreach (PathNode nextNode in nextNodes)
        {
            if (nextNode != null)
            {
                Gizmos.DrawLine(transform.position, nextNode.transform.position);
            }
        }
    }

    public NodeType GetNodeType()
    {
        return nodeType;
    }

}