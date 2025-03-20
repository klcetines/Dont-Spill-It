using UnityEngine;
using System.Collections.Generic;

public class MapPath : MonoBehaviour
{
    // Lista para almacenar los nodos hijos
    private List<PathNode> childNodes = new List<PathNode>();

    // Start is called before the first frame update
    void Start()
    {
        // Obtener todos los hijos de este objeto
        GetChildNodes();

        // Conectar automáticamente los nodos
        ConnectNodes();
    }

    // Método para obtener los hijos como nodos del camino
    private void GetChildNodes()
    {
        // Limpiar la lista de nodos hijos
        childNodes.Clear();

        // Recorrer todos los hijos del objeto actual
        foreach (Transform child in transform)
        {
            // Obtener el componente PathNode del hijo
            PathNode node = child.GetComponent<PathNode>();

            // Si el hijo tiene el componente PathNode, agregarlo a la lista
            if (node != null)
            {
                childNodes.Add(node);
            }
        }

        // Opcional: Imprimir la cantidad de nodos hijos encontrados
        Debug.Log($"Nodos hijos encontrados: {childNodes.Count}");
    }

    // Método para conectar los nodos entre sí
    private void ConnectNodes()
    {
        for (int i = 0; i < childNodes.Count - 1; i++)
        {
            // Conectar el nodo actual con el siguiente
            childNodes[i].AddNextNode(childNodes[i + 1]);
        }
    }

    // Método para obtener la lista de nodos hijos
    public List<PathNode> GetChildNodesList()
    {
        return childNodes;
    }

    // Método para obtener el primer nodo del camino
    public PathNode GetFirstNode()
    {
        return childNodes.Count > 0 ? childNodes[0] : null;
    }

    // Método para obtener el último nodo del camino
    public PathNode GetLastNode()
    {
        return childNodes.Count > 0 ? childNodes[childNodes.Count - 1] : null;
    }
}