using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HerdIcon : MonoBehaviour
{
	public Image SpeciesImage;

	[HideInInspector]
	public WorldComponent World;
	[HideInInspector]
	public int HerdIndex;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void OnSelected()
	{
		World.SelectHerd(HerdIndex);
	}
}
