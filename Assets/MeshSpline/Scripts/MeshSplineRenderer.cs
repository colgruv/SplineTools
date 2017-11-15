using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MeshSplineRenderer : MonoBehaviour
{
    public const int MAX_NUM_SECTIONS = 100;

    [Header("Settings")]

    [Range(0, MAX_NUM_SECTIONS)]
    public int NumSections;

    [Header("Anchors")]
    public Transform P0;
    private Vector3 m_LastP0;
    private Quaternion m_LastP0Rotation;
    public Transform P1;
    private Vector3 m_LastP1;
    private Quaternion m_LastP1Rotation;
    public Transform C0;
    private Vector3 m_LastC0;
    public Transform C1;
    private Vector3 m_LastC1;

    [Header("Pointers")]
    public MeshSplineSection SectionPrefab;
    public Transform SectionParent;

    private MeshSplineSection[] m_SplineSections;

    // Update is called once per frame: Play also in edit mode
    void Update ()
    {
        // Draw lines to represent control point influences
        renderDebug();

        // Refresh the section array if necessary
        if (m_SplineSections == null)
        {
            createSplineSectionArray();
        }
        else if (m_SplineSections.Length != NumSections || SectionParent.transform.childCount != NumSections)
        {
            createSplineSectionArray();
        }

        // Iterate through the section array and configure each point
        if (checkAnchorPositionsChanged())
            configureSections();

        // Capture current anchor positions so we know whether to reconfigure next frame.
        captureAnchorPositions();
    }

    /// <summary>
    /// Clear the current section array, if necessary, and repopulate a new one.
    /// </summary>
    private void createSplineSectionArray()
    {
        List<GameObject> sectionChildren = new List<GameObject>();
        for (int i = 0; i < SectionParent.transform.childCount; i++)
        {
            sectionChildren.Add(SectionParent.transform.GetChild(i).gameObject);
        }
        foreach (GameObject go in sectionChildren)
        {
            if (Application.isPlaying)
            {
                GameObject.Destroy(go);
            }
            else if (Application.isEditor)
            {
                GameObject.DestroyImmediate(go);
            }
        }

        m_SplineSections = new MeshSplineSection[NumSections];

        for (int i = 0; i < m_SplineSections.Length; i++)
        {
            MeshSplineSection newSection = GameObject.Instantiate(SectionPrefab);
            newSection.transform.SetParent(SectionParent);
            m_SplineSections[i] = newSection;
        }
    }

    /// <summary>
    /// Iterate through the section array and configure each point
    /// </summary>
    private void configureSections()
    {
        // Set up reference point variables
        Vector3 lastPoint = P0.position;
        m_SplineSections[0].Start.position = lastPoint;
        Vector3 currentPoint;
        Quaternion startRotation = P0.rotation;
        Quaternion endRotation = P1.rotation;

        // Iterate through the section array and configure each Start and End point's position and up vector
        for (int i = 0; i < m_SplineSections.Length; i++)
        {
            float t = (float)i / (float)NumSections;
            currentPoint = (((1 - t) * (1 - t) * (1 - t)) * P0.position)
                + (3 * ((1 - t) * (1 - t)) * t * C0.position)
                + (3 * (1 - t) * (t * t) * C1.position)
                + ((t * t * t) * P1.position);

            // Set positions of [i-1].End and [i].Start to currentPoint
            if (i > 0)
                m_SplineSections[i - 1].End.position = currentPoint;
            m_SplineSections[i].Start.position = currentPoint;

            // Interpolate bone rotation based on start and end anchors
            t = (float)i / (float)m_SplineSections.Length;
            m_SplineSections[i].Start.rotation = Quaternion.Lerp(startRotation, endRotation, t);
            t = (float)(i + 1) / (float)m_SplineSections.Length;
            m_SplineSections[i].End.rotation = Quaternion.Lerp(startRotation, endRotation, t);

            lastPoint = currentPoint;
        }

        // Configure the endpoint of the last section
        MeshSplineSection lastSection = m_SplineSections[m_SplineSections.Length - 1];
        lastSection.End.position = P1.position;
    }

    private void renderDebug()
    {
        Debug.DrawLine(P0.position, C0.position);
        Debug.DrawLine(P1.position, C1.position);
    }


    private void captureAnchorPositions()
    {
        m_LastP0 = P0.position;
        m_LastP0Rotation = P0.rotation;
        m_LastP1 = P1.position;
        m_LastP1Rotation = P1.rotation;
        m_LastC0 = C0.position;
        m_LastC1 = C1.position;
    }

    private bool checkAnchorPositionsChanged()
    {
        return (P0.position != m_LastP0 || P1.position != m_LastP1 || C0.position != m_LastC0 || C1.position != m_LastC1 || P0.rotation != m_LastP0Rotation || P1.rotation != m_LastP1Rotation);
    }
}
