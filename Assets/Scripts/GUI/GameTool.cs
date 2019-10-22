using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameTool : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public abstract void OnSelect();
	public abstract void OnDeselect();
}

