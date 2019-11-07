using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD : MonoBehaviour
{
	public GameObject HudGeo;
	public GameObject HudEnv;
	public GameObject HudLife;


	public enum HUDMode {
		Geology,
		Environment,
		Life
	}

    // Start is called before the first frame update
    void Start()
    {
		SetMode(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void SetMode(int mode)
	{
		HudGeo.SetActive((HUDMode)mode == HUDMode.Geology);
		HudEnv.SetActive((HUDMode)mode == HUDMode.Environment);
		HudLife.SetActive((HUDMode)mode == HUDMode.Life);
	}
}
