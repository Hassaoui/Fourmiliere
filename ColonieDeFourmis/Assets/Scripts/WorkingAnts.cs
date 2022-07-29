using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkingAnts : MonoBehaviour
{
    //enum de tous les state possible des fourmis
    public enum stateAnts
    {
        Nothing,
        goingToTakeFood,
        HaveFoodInHand,
        WaitLineQueen,
        TakingFoodToQueen,
        CheckWhichQueenToServe,
        WaitLineFood,
        ReturningFromQueen,
        goingToDoNothing,
        goingBackWithOutFood
    };
    public Transform transform;
    public Transform target;
    public List<Transform> path;
    private LeafManager leafManager;
    public int id;
    public int whoToServe;
    public int state;
    private int oldState;
    private int oldServe;
    public bool isFirstFromSource = false;
    public bool isFirstForQueen = false;

    public float speed;
    public float rotationSpeed;
    public List<Transform> LineGMFromSource;
    public List<Transform> LineGMFromQueen;
    int placeInPath;
    public WorkingAntManager manager;

    //initialise le leaf manager pour pouvoir montrer et cacher les feuilles
    public void AddLeafManager()
	{
       leafManager = transform.GetComponent<LeafManager>();
	}

    //fonction qui regarde si la fourmis a atteint le point qu'elle viait
    //cette fonction est appeler chaque frame pour chaque fourmis dans le WorkingAntManager
    public void checkTarget()
    {
        Vector2 dir = target.position - transform.position;
        //pour la rotation de la fourmi
        if (dir != Vector2.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(Vector3.forward, dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }
        //selon le state de la fourmi certaines actions vont être prise
        if (oldState != state && state != (int)stateAnts.WaitLineQueen && state != (int)stateAnts.goingBackWithOutFood)
        {
            target = path[0];
            placeInPath = 0;
        }
        if(oldState == state && whoToServe != oldServe)
		{
            target = path[placeInPath];
            oldServe = whoToServe;
        }
        if(oldState != state && state == (int)stateAnts.WaitLineQueen)
		{
            manager.AntsInLineForQueen.Add(this);
            target = LineGMFromQueen[0];
        }

        if(oldState == (int)stateAnts.CheckWhichQueenToServe)
		{
            path = manager.GetPathQueenByID(whoToServe);
            target = path[0];
            placeInPath = 0;
            state = (int)stateAnts.TakingFoodToQueen;
            manager.ChangeSetSingleAnt(id, state);
        }

        if (state == (int)stateAnts.Nothing)
            path = new List<Transform>() { manager.DoNothingPlace };

        //si la fourmi a atteint sa target elle va regarder quelle est la prochaine
        if (Vector2.Distance(transform.position, target.position) <= 0.05f)
        {
            nextPoint();
        }
    }

    //fonction qui change le state de la fourmis
    //elle est appeler chaque frame pour chaque fourmis dans WorkingAntManager
    public void ChangeStateNFinalTarget(int toServe, int _state)
	{
        oldServe = whoToServe;
        oldState = state;
        whoToServe = toServe;
        state = _state;
	}

    public void SetPath(List<Transform> _path)
	{
        path = _path;
	}

    //fonction qui regarde quelle est le prochain objectif selon le state de la fourmi
    //cette fonction s'occupe aussi de faire les actions comme prendre la nourriture d'une source
    //ou donner de la nourriture a une reine
    private void nextPoint()
    {
        //lorsque la fourmis ne fait rien alors il ne faut rien changer
        if(state == (int)stateAnts.Nothing)
		{
            target = manager.DoNothingPlace;
		}
        else if (state == (int)stateAnts.goingToTakeFood)
        {
            //when they directlyGo to a foodSource
            if (placeInPath < path.Count - 1)
            {
                placeInPath++;
                target = path[placeInPath];
            } 
            //losqu'elle arrive a son objectif elle prendre une feuille de la source et change son state 
            //pour retourner à la colonie
            else if (placeInPath == path.Count - 1)
            {
                
                leafManager.ShowLeaf();
                manager.RemoveFromSource(whoToServe - 5);
                state = (int)stateAnts.HaveFoodInHand;
                manager.ChangeSetSingleAnt(id, state);
            }
        } else if (state == (int)stateAnts.HaveFoodInHand)
        {
            if (placeInPath > 0)
			{
                placeInPath--;
                target = path[placeInPath];
            }
            //lorsqu'elle retourne a la colonie avec de la nourriture elle va en ligne pour la mettre dans le garde mangé
            else if(placeInPath == 0)
            {
                manager.AntsInLineFromSource.Add(this);
                state = (int)stateAnts.WaitLineFood;
                manager.ChangeSetSingleAnt(id, state);
                target = LineGMFromSource[0];
			}
		}else if (state == (int)stateAnts.TakingFoodToQueen)
        {
            if (placeInPath < path.Count - 1)
            {
                placeInPath++;
                target = path[placeInPath];
            }
            //losque la fourmi arrive à la reine pour lui donner de la nourriture elle lui donne et elle revient au garde mangé
            else if (placeInPath == path.Count - 1)
            {
                //do the add food to queen action
                leafManager.HideLeaf();
                manager.AddToQueen(whoToServe);
                state = (int)stateAnts.ReturningFromQueen;
                manager.ChangeSetSingleAnt(id, state);
            }
        }
        else if (state == (int)stateAnts.ReturningFromQueen)
        {
            if (placeInPath > 0)
            {
                placeInPath--;
                target = path[placeInPath];
            }
            //lorsqu'elle revient de nourrire la reine elle va rien faire jusqu'à ce que'elle prenne une auttre decision
            else if (placeInPath == 0)
            {
                state = (int)stateAnts.goingToDoNothing;
                manager.ChangeSetSingleAnt(id, state);
            }
        }
        //fonction qui gère la position des fourmis dans la ligne vers le garde mangé des sources de nourriture
        else if(state == (int)stateAnts.WaitLineFood)
		{
            int positionFile = 0;
			for(int i = 0; i < manager.AntsInLineFromSource.Count; i++)
			{
                if (manager.AntsInLineFromSource[i].id == id)
				{
                    positionFile = i;
                }
			}
            if (positionFile > LineGMFromSource.Count - 1)
                positionFile = LineGMFromSource.Count - 1;

            if(positionFile < placeInPath)
			{
                placeInPath--;
                target = LineGMFromSource[placeInPath];
			}else if(positionFile > placeInPath)
            {
                placeInPath++;
                target = LineGMFromSource[placeInPath];
            }else if(placeInPath == 0 && Vector2.Distance(transform.position, LineGMFromSource[0].position) <= 0.05f)
			{
                isFirstFromSource = true;
			}
        }
        //fonction qui gère la position des fourmis dans la ligne vers le garde mangé des reines
        else if (state == (int)stateAnts.WaitLineQueen)
        {
            int positionFile = 0;
            for (int i = 0; i < manager.AntsInLineForQueen.Count; i++)
            {
                if (manager.AntsInLineForQueen[i].id == id)
                {
                    positionFile = i;
                }
            }
            if (positionFile > LineGMFromQueen.Count - 1)
                positionFile = LineGMFromQueen.Count - 1;

            if (positionFile < placeInPath)
            {
                placeInPath--;
                target = LineGMFromQueen[placeInPath];
            }
            else if (positionFile > placeInPath)
            {
                placeInPath++;
                target = LineGMFromQueen[placeInPath];
            }
            else if (placeInPath == 0 && Vector2.Distance(transform.position, LineGMFromQueen[0].position) <= 0.05f)
            {
                isFirstForQueen = true;
            }
        }else if(state == (int)stateAnts.goingToDoNothing)
		{
            target = manager.DoNothingPlace;
            if (Vector2.Distance(transform.position, manager.DoNothingPlace.position) <= 0.05f)
			{
                state = (int)stateAnts.Nothing;
                manager.ChangeSetSingleAnt(id, state);
            }
        }
        //losqu'une fourmi ne peut pas prendre de nourriture d'aucune source parce qu'elles sont tous vide
        else if(state == (int)stateAnts.goingBackWithOutFood)
        {
            if (placeInPath > 0)
            {
                placeInPath--;
                target = path[placeInPath];
            }
            else if (placeInPath == 0)
            {
                state = (int)stateAnts.goingToDoNothing;
                manager.ChangeSetSingleAnt(id, state);
                target = manager.DoNothingPlace;
                path = new List<Transform>() { manager.DoNothingPlace };
            }
        }
    }

    //losque le garde mangé dans la ligne de source permet a une fourmi de quitter celui-ci cette fonction est appeler pour la fourmi
    public void GetOutLineFromSource()
	{
        leafManager.HideLeaf();
        manager.PutFoodInGM();
        placeInPath = 0;
        isFirstFromSource = false;
        state = (int)stateAnts.goingToDoNothing;
        manager.ChangeSetSingleAnt(id, state);
        path = new List<Transform>() { manager.DoNothingPlace };
        target = manager.DoNothingPlace;
    }
    //losque le garde mangé dans la ligne des reines permet a une fourmi de quitter celui-ci cette fonction est appeler pour la fourmi
    public void GetOutLineForQueen()
	{
        //put leaf on
        leafManager.ShowLeaf();
        manager.TakeFoodFromGM();
        placeInPath = 0;
        isFirstForQueen = false;
        state = (int)stateAnts.CheckWhichQueenToServe;
        manager.ChangeSetSingleAnt(id, state);
    }

}
