using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetSlider : MonoBehaviour
{
	public WorldComponent World;
	public World.MutationType Mutation;
	public Slider CurStateSlider;
	public Slider DesiredStateSlider;

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void OnValueChanged()
	{
		World.SetDesiredMutation(World.HerdSelected, Mutation, DesiredStateSlider.value);
	}
}
