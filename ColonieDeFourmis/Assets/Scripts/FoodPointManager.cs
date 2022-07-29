using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FoodPointManager : MonoBehaviour
{
    public float widthSize = 3;
    public float lenghtSize = 3;
    public int numberOfStartingLeafsLeafs = 5;
    public int maximumNumberOfLeafs = 50;
    public List<Transform> listLeafs;
    public Transform leaf;
    public Transform square;
    public TextMeshProUGUI UIFood;
    public WorkingAntManager manager;

    public float timeSpawnLeaf = 3f;
    private float timer;
    public int id;

    //initialiser les variables necessaire lorsque le programme commence
    void Start()
    {
        timer = timeSpawnLeaf;
        listLeafs = new List<Transform>();
		for (int i = 0; i < numberOfStartingLeafsLeafs; i++)
		{
            GameObject parent = this.gameObject;
            float x = UnityEngine.Random.Range(-widthSize / 2, widthSize / 2);
            float y = UnityEngine.Random.Range(-lenghtSize / 2, lenghtSize / 2);
            Transform leafTransform = Instantiate(leaf, new Vector3(transform.position.x + x, transform.position.y + y), Quaternion.Euler(0.0f, 0.0f, Random.Range(0.0f, 360.0f)));
            listLeafs.Add(leafTransform);
            leafTransform.transform.parent = parent.transform;
            UpdateUI();
        }
        square.localScale = new Vector3(widthSize, lenghtSize, 1);
    }

    //reduire le temps du timer et lorsqu'il atteint 0 créer une feuille
    void Update()
    {
        if(GetFoodQuantity() > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                spawnLeaf();
                timer = timeSpawnLeaf;
            }
        }
    }
    //fonction pour créé une feuille
    private void spawnLeaf()
    {
        GameObject parent = this.gameObject;
        float x = UnityEngine.Random.Range(-widthSize / 2, widthSize / 2);
        float y = UnityEngine.Random.Range(-lenghtSize / 2, lenghtSize / 2);
        Transform leafTransform = Instantiate(leaf, new Vector3(transform.position.x + x, transform.position.y + y), Quaternion.Euler(0.0f, 0.0f, Random.Range(0.0f, 360.0f)));
        listLeafs.Add(leafTransform);
        leafTransform.transform.parent = parent.transform;
        UpdateUI();
        manager.UpdateFoodSource();
    }

    //fonction qui enlèeve une feuille de la source
    public void RemoveLeaf()
    {
        if(listLeafs.Count > 0)
        {
            int idLeaf = UnityEngine.Random.Range(0, listLeafs.Count);
            Destroy(listLeafs[idLeaf].gameObject);
            listLeafs.Remove(listLeafs[idLeaf]);
            UpdateUI();
            manager.UpdateFoodSource();
        }
    }

    //mettre à jour le UI
    private void UpdateUI()
	{

        UIFood.text = (listLeafs.Count).ToString();
    }

    //retourne la quantité de nourriture dans la source
    public int GetFoodQuantity()
	{
        return listLeafs.Count;
    }
    //retourne la quantité de nourriture de départ de la source
    public int GetFoodQuantityOnStart()
	{
        return numberOfStartingLeafsLeafs;
    }
}
