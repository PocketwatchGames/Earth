using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HerdIcon : MonoBehaviour
{
	public Button SpeciesButton;
	public Image SpeciesImage;
	public Text SpeciesName;

	[HideInInspector]
	public WorldComponent World;
	[HideInInspector]
	public int HerdIndex;

    // Start is called before the first frame update
    void Start()
    {
		SpeciesName.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
		SpeciesButton.GetComponent<Image>().color = World.HerdSelected == HerdIndex ? Color.magenta : Color.gray;
    }

	public void OnSelected()
	{
		World.SelectHerd(HerdIndex);
	}

	public void OnHoverEnter()
	{
		int speciesIndex = World.World.States[World.World.CurRenderStateIndex].Herds[HerdIndex].SpeciesIndex;
		SpeciesName.text = World.World.SpeciesDisplay[speciesIndex].Name;
		SpeciesName.gameObject.SetActive(true);
	}

	public void OnHoverExit()
	{
		SpeciesName.gameObject.SetActive(false);
	}
}
