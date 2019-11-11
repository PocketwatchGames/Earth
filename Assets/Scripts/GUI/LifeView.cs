using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeView : MonoBehaviour
{
	public WorldComponent World;
	public HerdInfoPanel HerdInfo;

    // Start is called before the first frame update
    void Start()
    {
		World.HerdSelectedEvent += OnHerdSelected;
		HerdInfo.gameObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
    {
        
    }

	public void OnHerdSelected()
	{
		HerdInfo.gameObject.SetActive(World.HerdSelected >= 0);
		if (World.HerdSelected >= 0)
		{
			GetComponent<ToolTerritory>().OnSelect();
		} else
		{
			GetComponent<ToolTerritory>().OnDeselect();
		}
	}

	public void OnEnable()
	{
		OnHerdSelected();
	}


}
