using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeafManager : MonoBehaviour
{
	/*
	 *  Fonction pour montrer ou cacher l'image de la feuille sur les fourmis
	 * */
    public GameObject leaf;
   
    public void ShowLeaf()
	{
		leaf.SetActive(true);
	}
	public void HideLeaf()
	{
		leaf.SetActive(false);
	}
}
