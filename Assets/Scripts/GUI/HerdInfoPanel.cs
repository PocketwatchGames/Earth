using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HerdInfoPanel : MonoBehaviour
{
	public WorldComponent World;

	public Text NameText;
	public Text PopulationText;
	public Text FoodText;
	public Text WaterText;
	public Text ComfortText;
	public Text SocialText;
	public Slider MutationProgress;
	public TargetSlider MutationSizeProgress;
	public TargetSlider MutationReproductionProgress;
	public TargetSlider MutationHealthProgress;
	public Button EvolveButton;
	public GameObject UnitIconPrefab;
	public GameObject UnitDisplay;
	//private GameObject[] _unitIcons;

    // Start is called before the first frame update
    void Start()
    {
		World.HerdSelectedEvent += OnHerdSelected;

		//_unitIcons = new GameObject[Herd.MaxUnits];
		//for (int i = 0; i < Herd.MaxUnits; i++)
		//{
		//	var u = GameObject.Instantiate<GameObject>(UnitIconPrefab, UnitDisplay.transform);
		//	u.hideFlags = HideFlags.HideInHierarchy;
		//	u.transform.SetAsLastSibling();
		//	u.SetActive(false);
		//	_unitIcons[i] = u;
		//}
	}

	private void OnEnable()
	{
		OnHerdSelected();
	}

	// Update is called once per frame
	void Update()
	{
		if (World.HerdSelected >= 0)
		{
			var state = World.World.States[World.World.CurRenderStateIndex];
			var herd = state.Herds[World.HerdSelected];
			PopulationText.text = "Population: " + herd.Population;

			float food = 0;
			float water = 0;
			float comfort = 0;
			float social = 0;

			food += herd.Food / World.World.GetMaxFoodHeld();
			water += herd.Water / World.World.GetMaxWaterHeld();
			comfort += herd.Comfort / World.World.GetMaxComfortHeld();
			social += herd.Population;
			social = Mathf.Clamp01(social / World.World.GetMaxPopulationDensity() / herd.ActiveTileCount);


			FoodText.color = food > 0.5 ?
				Color.Lerp(Color.yellow, Color.green, (food - 0.5f) / 0.5f) :
				Color.Lerp(Color.red, Color.yellow, (food) / 0.5f);

			WaterText.color = water > 0.5 ?
				Color.Lerp(Color.yellow, Color.green, (water - 0.5f) / 0.5f) :
				Color.Lerp(Color.red, Color.yellow, (water) / 0.5f);

			ComfortText.color = comfort > 0.5 ?
				Color.Lerp(Color.yellow, Color.green, (comfort - 0.5f) / 0.5f) :
				Color.Lerp(Color.red, Color.yellow, (comfort) / 0.5f);

			SocialText.color = social > 0.5 ?
				Color.Lerp(Color.yellow, Color.green, (social - 0.5f) / 0.5f) :
				Color.Lerp(Color.red, Color.yellow, (social) / 0.5f);

			MutationProgress.value = herd.EvolutionProgress;
			MutationSizeProgress.CurStateSlider.value = herd.MutationSize;
			MutationReproductionProgress.CurStateSlider.value = herd.MutationReproduction;
			MutationHealthProgress.CurStateSlider.value = herd.MutationHealth;
			EvolveButton.interactable = herd.EvolutionProgress >= 1;

			//for (int i=0;i<Herd.MaxUnits;i++)
			//{
			//	bool active = i < herd.UnitCount;
			//	_unitIcons[i].SetActive(active);
			//	if (active)
			//	{
			//		if (herd.Units[i].Maturity == Herd.UnitMaturity.Juvenile)
			//		{
			//			_unitIcons[i].transform.localScale = new Vector3(1, 0.5f, 1);
			//		} else if (herd.Units[i].Maturity == Herd.UnitMaturity.Adult)
			//		{
			//			_unitIcons[i].transform.localScale = new Vector3(1, 1, 1);
			//		} else
			//		{
			//			_unitIcons[i].transform.localScale = new Vector3(1, 0.8f, 1);
			//		}
			//	}

			//}
		}
	}

	private void OnHerdSelected()
	{
		if (World.World == null || World.HerdSelected < 0)
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
