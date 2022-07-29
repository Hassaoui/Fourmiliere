using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GMLeafsManager : MonoBehaviour
{

    public float widthSize = 3;
    public float lenghtSize = 3;
    public List<Transform> listLeafs;
    public Transform leaf;

    //Ajout d'une feuille dans le garde Mangé
    public void AddLeafGM()
	{
        GameObject parent = this.gameObject;
        float x = UnityEngine.Random.Range(-widthSize / 2, widthSize / 2);
        float y = UnityEngine.Random.Range(-lenghtSize / 2, lenghtSize / 2);
        Transform leafTransform = Instantiate(leaf, new Vector3(transform.position.x + x, transform.position.y + y), Quaternion.Euler(0.0f, 0.0f, Random.Range(0.0f, 360.0f)));
        listLeafs.Add(leafTransform);
        leafTransform.transform.parent = parent.transform;
    }

    //Enlève une feuille dans le Garde mangé
	public void RemoveLeafGM()
    {
        int idLeaf = UnityEngine.Random.Range(0, listLeafs.Count);
        Destroy(listLeafs[idLeaf].gameObject);
        listLeafs.Remove(listLeafs[idLeaf]);
    }
}
