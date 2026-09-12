using UnityEngine;

public class TorchController : MonoBehaviour
{
    public Light torchLight;
    public ParticleSystem glowParticle;

    bool isOn = false;


    void Start()
    {
        TurnOff();
    }


    public void ToggleTorch()
    {
        if (isOn)
            TurnOff();
        else
            TurnOn();
    }


    void TurnOn()
    {
        isOn = true;

        torchLight.enabled = true;

        if (glowParticle != null)
            glowParticle.Play();
    }


    void TurnOff()
    {
        isOn = false;

        torchLight.enabled = false;

        if (glowParticle != null)
            glowParticle.Stop();
    }
}