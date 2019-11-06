using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameTool : MonoBehaviour
{
	public bool Active;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	virtual public void OnSelect()
	{
		Active = true;
	}
	virtual public void OnDeselect()
	{
		Active = false;
	}
}

