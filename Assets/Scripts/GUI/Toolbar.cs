using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toolbar : MonoBehaviour
{
	public int ActiveToolIndex = 0;
	public List<UnityEngine.UI.Button> Buttons;

	// Start is called before the first frame update
	virtual public void Start()
    {
		Buttons[ActiveToolIndex].interactable = false;
		int i = 0;
		foreach (var b in Buttons)
		{
			int index = i;
			b?.onClick.AddListener(delegate { OnClick(index); });
			i++;
		}
	}


	virtual public void OnClick(int index)
	{
		Buttons[ActiveToolIndex].interactable = true;
		Buttons[index].interactable = false;
		ActiveToolIndex = index;
	}
}
