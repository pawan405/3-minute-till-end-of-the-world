using UnityEngine;

public class LightController : MonoBehaviour
{
    public Light[] lights;

    void Start()
    {
        TurnOffLights();
    }


    public void TurnOnLights()
    {
        foreach (Light light in lights)
        {
            light.enabled = true;
        }
    }


    public void TurnOffLights()
    {
        foreach (Light light in lights)
        {
            light.enabled = false;
        }
    }
}