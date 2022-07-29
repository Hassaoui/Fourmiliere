using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuennAnt : MonoBehaviour
{
    public TextMeshProUGUI UIFood;
    public GameObject CanavasToHide;
    public WorkingAntManager manager;


    private int amountOfFood;
    public float TimeToEat = 2;
    private float timerEat;
    public int id;
    private bool dead = false;

	private void Start()
	{
        timerEat = TimeToEat;
	}

    //fait manger la queen à chaque intervalle de temps
	void Update()
    {
		if (!dead)
        {
            if (timerEat <= 0)
            {
                timerEat = TimeToEat;
                SetAmountOfFood(amountOfFood - 1);
            }
            timerEat -= Time.deltaTime;

            if (amountOfFood == 0)
            {
                Dies();
            }
        }
    }

    //update ui
    public void SetAmountOfFood(int amount)
	{
        UIFood.text = amount.ToString();
        amountOfFood = amount;
        manager.UpdateFoodForQueen();
	}

    //retourne la quantité de nourriture que la reine a
    public int GetFoodQuantity()
	{
        return amountOfFood;
	}

    //lorsque la reine n'a plus de nourriture elle meurt
    public void Dies()
    {
        dead = true;
        CanavasToHide.SetActive(false);
        this.GetComponent<SpriteRenderer>().enabled = false;
    }

    //ajout de nourriture pour la reine
    public void AddOneFood()
	{
        if(!dead)
            SetAmountOfFood(amountOfFood + 1);
	}
}
