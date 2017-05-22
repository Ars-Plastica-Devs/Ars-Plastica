using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SculptureOptionsController : MonoBehaviour
{
    private readonly Dictionary<SculptureType, GameObject> m_SculpturePanelMap = new Dictionary<SculptureType, GameObject>(4);
    private Dictionary<string, Func<string>> m_CurrentDataMap; 

    private float m_UpdateRate = 1f;
    private float m_Updatecounter;

    private string m_NetIDString;
    private GameObject m_ActivePanel;
    private SculptureInteractionHandler m_CurrentHandler;

    public GameObject TransparentCubesPanel;
    public GameObject AvoidCubesPanel;
    public GameObject ShrinkCubesPanel;
    public GameObject PixelBoardPanel;
    public GameObject TextureZoomScrollWallPanel;
    public GameObject PointingSwarmPanel;
    public GameObject SeatingPanel;

    private void Awake()
    {
        m_SculpturePanelMap[SculptureType.TransparentCubes] = TransparentCubesPanel;
        m_SculpturePanelMap[SculptureType.AvoidCubes] = AvoidCubesPanel;
        m_SculpturePanelMap[SculptureType.ShrinkCubes] = ShrinkCubesPanel;
        m_SculpturePanelMap[SculptureType.PixelBoard] = PixelBoardPanel;
        m_SculpturePanelMap[SculptureType.TextureZoomScrollWall] = TextureZoomScrollWallPanel;
        m_SculpturePanelMap[SculptureType.PointingSwarm] = PointingSwarmPanel;
        m_SculpturePanelMap[SculptureType.Seating] = SeatingPanel;
    }

    private void Update()
    {
        m_Updatecounter += Time.deltaTime;
        if (m_Updatecounter > m_UpdateRate)
        {
            m_Updatecounter = 0f;

            if (m_CurrentDataMap == null)
                return;

            foreach (var kvp in m_CurrentDataMap)
            {
                SetCurrentText(m_ActivePanel.transform, kvp.Key, kvp.Value());
            }
        }
    }

    public void SetHandler(SculptureInteractionHandler handler)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (m_ActivePanel != null)
            m_ActivePanel.SetActive(false);

        m_ActivePanel = m_SculpturePanelMap[handler.Type];
        m_ActivePanel.SetActive(true);

        m_CurrentHandler = handler;
        m_NetIDString = m_CurrentHandler.GetIdentifier();

        m_CurrentDataMap = m_CurrentHandler.GetData();
        
        foreach (var kvp in m_CurrentDataMap)
        {
            SetCurrentText(m_ActivePanel.transform, kvp.Key, kvp.Value());
        }
    }

    public void SetCubeInteractRadius(string value)
    {
        SendCommand("CubeInteractRadius", value);
    }

    public void SetGapFactor(string value)
    {
        SendCommand("GapFactor", value);
    }

    public void SetSideLength(string value)
    {
        SendCommand("SideLength", value);
    }

    public void SetColor(string value)
    {
        SendCommand("Color", value);
    }

    public void SetPositiveColor(string value)
    {
        SendCommand("PositiveColor", value);
    }

    public void SetNegativeColor(string value)
    {
        SendCommand("NegativeColor", value);
    }

    public void SetCubeSize(string value)
    {
        SendCommand("CubeSize", value);
    }

    public void SetDistanceToTravel(string value)
    {
        SendCommand("DistanceToTravel", value);
    }

    public void SetDistanceVariation(string value)
    {
        SendCommand("DistanceVariation", value);
    }

    public void SetSpeed(string value)
    {
        SendCommand("Speed", value);
    }

    public void SetSpeedVariation(string value)
    {
        SendCommand("SpeedVariation", value);
    }

    public void SetOneSpeed(string value)
    {
        SendCommand("OneSpeed", value);
    }

    public void SetTwoSpeed(string value)
    {
        SendCommand("TwoSpeed", value);
    }

    public void SetThreeSpeed(string value)
    {
        SendCommand("ThreeSpeed", value);
    }

    public void SetOneScaleSpeed(string value)
    {
        SendCommand("OneScaleSpeed", value);
    }

    public void SetTwoScaleSpeed(string value)
    {
        SendCommand("TwoScaleSpeed", value);
    }

    public void SetThreeScaleSpeed(string value)
    {
        SendCommand("ThreeScaleSpeed", value);
    }

    public void SetXScale(string value)
    {
        SendCommand("XScale", value);
    }

    public void SetYScale(string value)
    {
        SendCommand("YScale", value);
    }

    public void SetDistanceBetweenPrimitives(string value)
    {
        SendCommand("DistanceBetweenPrimitives", value);
    }

    public void SetWidth(string value)
    {
        SendCommand("Width", value);
    }

    public void SetHeight(string value)
    {
        SendCommand("Height", value);
    }

    public void SetPrimitiveScale(string value)
    {
        SendCommand("PrimitiveScale", value);
    }

    public void SetInteractRange(string value)
    {
        SendCommand("InteractRange", value);
    }

    public void SetDistanceFromTarget(string value)
    {
        SendCommand("DistanceFromTarget", value);
    }

    public void SetDistanceBetweenSeats(string value)
    {
        SendCommand("DistanceBetweenSeats", value);
    }

    public void SetMinScale(string value)
    {
        SendCommand("MinScale", value);
    }

    public void SetMaxScale(string value)
    {
        SendCommand("MaxScale", value);
    }

    public void DeleteSculpture()
    {
        SendCommand("delete", string.Empty);

        gameObject.SetActive(false);
    }

    public void RotateSculpture(string rot)
    {
        SendCommand("rotate", rot);
    }

    public void TranslateSculpture(string t)
    {
        SendCommand("translate", t);
    }

    private void SendCommand(string dataKey, string value)
    {
        var cmd = "/" + m_NetIDString + " " + dataKey + (string.IsNullOrEmpty(value) ? "" : " " + value);
        HUDManager.Singleton.ForwardCommand(cmd);
    }

    private void SetCurrentText(Transform parent, string childName, string text)
    {
        var c = parent.Find(childName);
        var inputField = c.GetComponentInChildren<InputField>();

        if (!inputField.isFocused)
            inputField.text = text;
    }
}
