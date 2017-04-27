using UnityEngine;
using System.Collections;
using UnityEditor;

[ExecuteInEditMode]
public class NormalEditor : MonoBehaviour
{
    Mesh m_mesh;
    public Mesh Mesh { get { return m_mesh; } }

    [SerializeField]
    Vector3[] m_normals;

    void OnEnable()
    {
        m_mesh = GetComponent<MeshFilter>().mesh;
        m_normals = m_mesh.normals;
    }

    public void ApplyNewNormals()
    {
        Vector3[] fixedNormals = new Vector3[m_normals.Length];
        for (int i = 0; i < m_normals.Length; i++)
        {
            fixedNormals[i] = m_normals[i];
            fixedNormals[i].Normalize();
        }
        m_mesh.normals = fixedNormals;
    }
}

[CustomEditor(typeof(NormalEditor))]
public class NormalEditorEditor : Editor
{
    NormalEditor m_norm;
    void OnEnable()
    {
        m_norm = target as NormalEditor;
        Undo.undoRedoPerformed += ApplyNewNormals;
    }

    void OnSceneGUI()
    {
        if (m_norm == null || m_norm.Mesh == null)
            return;

        for (int i = 0; i < m_norm.Mesh.vertexCount; i++)
        {
            Handles.color = Color.blue;
            Handles.matrix = m_norm.transform.localToWorldMatrix;
            Handles.Label(m_norm.Mesh.vertices[i], i.ToString());

            Handles.color = Color.yellow;
            Handles.DrawLine(
                m_norm.Mesh.vertices[i],
                m_norm.Mesh.vertices[i] + m_norm.Mesh.normals[i]);
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            ApplyNewNormals();
        }
    }

    void ApplyNewNormals()
    {
        if (!Application.isPlaying)
        {
            m_norm.ApplyNewNormals();
        }
    }

    void OnDisable()
    {
        Undo.undoRedoPerformed -= ApplyNewNormals;
    }
}