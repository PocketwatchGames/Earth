using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HerdInfoPanel : MonoBehaviour
{
	public WorldComponent World;

	public Text NameText;
	public Text PopulationText;
	public Slider MutationProgress;
	public TargetSlider MutationSizeProgress;
	public TargetSlider MutationReproductionProgress;
	public TargetSlider MutationHealthProgress;
	public Button EvolveButton;

    // Start is called before the first frame update
    void Start()
    {
		World.HerdSelectedEvent += OnHerdSelected;
    }

	// Update is called once per frame
	void Update()
	{
		if (World.HerdSelected >= 0)
		{
			var state = World.World.States[World.World.CurRenderStateIndex];
			PopulationText.text = "Population: " + state.Herds[World.HerdSelected].Status.Population;
			MutationProgress.value = state.Herds[World.HerdSelected].EvolutionProgress;
			MutationSizeProgress.CurStateSlider.value = state.Herds[World.HerdSelected].MutationSize;
			MutationReproductionProgress.CurStateSlider.value = state.Herds[World.HerdSelected].MutationReproduction;
			MutationHealthProgress.CurStateSlider.value = state.Herds[World.HerdSelected].MutationHealth;
			EvolveButton.interactable = state.Herds[World.HerdSelected].EvolutionProgress >= 1;


		}
	}

	private void OnHerdSelected()
	{
		if (World.HerdSelected < 0)
		{
			return;
		}
		var state = World.World.States[World.World.CurRenderStateIndex];
		NameText.text = World.World.SpeciesDisplay[state.Herds[World.HerdSelected].SpeciesIndex].Name;
		MutationSizeProgress.DesiredStateSlider.value = state.Herds[World.HerdSelected].DesiredMutationSize;
		MutationReproductionProgress.DesiredStateSlider.value = state.Herds[World.HerdSelected].DesiredMutationReproduction;
		MutationHealthProgress.DesiredStateSlider.value = state.Herds[World.HerdSelected].DesiredMutationHealth;
	}


}
