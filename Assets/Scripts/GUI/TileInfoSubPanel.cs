using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileInfoSubPanel : MonoBehaviour
{
	public UnityEngine.UI.Text Text;
	public UnityEngine.UI.Toggle Toggle;

    // Start is called before the first frame update
    void Start()
    {
		Text.gameObject.SetActive(Toggle.isOn);
	}

	// Update is called once per frame
	void Update()
    {
        
    }

	public void OnPanelToggled()
	{
		Text.gameObject.SetActive(Toggle.isOn);
	}

	public void SetText(string t)
	{
		Text.text = t;
	}
}
