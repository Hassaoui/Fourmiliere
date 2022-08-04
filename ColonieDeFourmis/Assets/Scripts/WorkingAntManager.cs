using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

public class WorkingAntManager : MonoBehaviour
{
    public enum action
    {
        Food,
        Queen,
        Nothing
    };

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
    public bool parallele = false;


    [Header("GM stats")]
    public int startingFoodGm;
    private int foodInGM = 0;
    public float timeInBetweenPeople = 0.5f;
    private float timerPPl;
    private bool IsSomeoneIn = true;
    private action nextInGM;
    public GMLeafsManager GMmanager;
    public Transform DoNothingPlace;

    [Header("Queens info")]
    //public
    public int startingFoodQueen = 2;
    public List<QuennAnt> queenList;
    public List<FoodPointManager> foodSourceList;
    //private


    [Header("Workers info")]
    //public
    public int numberofAnts = 5;
    public Transform antPrefab;
    public Transform _target;
    public float antSpeed = 5;
    //private
    private List<WorkingAnts> workingAntsList;
    private List<Transform> antsPosition;
    public Transform spawnPoints;
    NativeArray<float3> positionArray;
    NativeArray<float3> _antsTarget;
    NativeArray<float> speed_Ants;
    NativeArray<int> whoToServe;
    NativeArray<int> antsState;
    NativeArray<int> randomState;

    [Header("Stats")]
    public List<int> numberAntPerSourceOfFood;
    NativeArray<int> FoodPerSource;
    NativeArray<int> FoodPerQueen;
    public List<WorkingAnts> AntsInLineFromSource;
    public List<WorkingAnts> AntsInLineForQueen;
    public float pourcentageAntsDoingNothingMax = 0.1f;
    public int foodBeforeFeedingQueenAbsolutly = 10;
    public int threshOldTooManyPeopleInLineSource = 70;
    public int threshOldTooManyPeopleInLineQueen = 80;

    [Header("UI")]
    public TextMeshProUGUI UIFood;

    [Header("Path")]

    public List<Transform> PathToGMFromSources;
    public List<Transform> PathToGMFromQueens;
    public List<Transform> PathToQueen;
    public List<Transform> PathToQueen1;
    public List<Transform> PathToQueen2;
    public List<Transform> PathToQueen3;
    public List<Transform> PathToQueen4;
    public List<Transform> PathToFood;
    public List<Transform> PathToFood1;
    public List<Transform> PathToFood2;
    public List<Transform> PathToFood3;



    //fonction qui initialise toutes les variables et listes au début du programme
    private void Start()
    {
		for (int i = 0; i < startingFoodGm; i++)
		{
            PutFoodInGM();
		}
        inizializeQueens();
        inizializeSource();
        inizializeWorkers();
        timerPPl = timeInBetweenPeople;
        UIFood.text = (foodInGM).ToString();

    }

    //fonction qui est appeler à chaque frame et qui modifie la position de chaque fourmi
    //la fonction fait aussi prendre des decisions aux fourmi à chaque frame comme ça elle sont plus flexible sur l'actions qu'elles vont faire
	private void Update()
	{
        //boucle qui sort le nombre de fourmi qui ne font rien
        int antdoingNothing = 0;
        for (int i = 0; i < workingAntsList.Count; i++)
		{
            if (antsState[i] == (int)stateAnts.Nothing)
                antdoingNothing += 1;
		}

        //création d'une fonction qui va calculer le mouvement des fourmis en parallele
        MoveAnts movement = new MoveAnts
        {
            deltaTime = Time.deltaTime,
            PositionAnts = positionArray,
            antsSpeed = speed_Ants,
            targetAnts = _antsTarget,
        };

        //copmmencenemnt de la fonction en parallèle
        JobHandle jobHandle = movement.Schedule(workingAntsList.Count, 100);
        jobHandle.Complete();

        //création de la fonction en parallèle qui va faire prendre les decision aux fourmis
        DecisionAnts takeDecision = new DecisionAnts
        {
            _antsState = antsState,
            _whoToServe = whoToServe,
            foodPerQueen = FoodPerQueen,
            foodPerFoodSource = FoodPerSource,
            antsDoingNothing = antdoingNothing,
            numberOfAntsDoingnothingMax = (int)(workingAntsList.Count * pourcentageAntsDoingNothingMax),
            threshOldFeedingQueen = foodBeforeFeedingQueenAbsolutly,
            foodInGM = foodInGM,
            PeopleLineQueen = AntsInLineForQueen.Count,
            PeopleLineSource = AntsInLineFromSource.Count,
            threshOldTooManyPeopleInLineSource = threshOldTooManyPeopleInLineSource,
            threshOldTooManyPeopleInLineQueen = threshOldTooManyPeopleInLineQueen,
        };

        //commencememnt de la fonction qui va faire prendre des decisions en parallèle aux fourmis
        JobHandle jobHandle1 = takeDecision.Schedule(workingAntsList.Count, 100);
        jobHandle1.Complete();

        //mets a jour ce qui a été changer des les fonctions en parallèle dans les fourmis
        //ces fonctions ne peuvent pas être appeler en parallèle car elles dépendent d'objets qui ne peuvent pas être accéder en parallèle
        for (int i = 0; i < workingAntsList.Count; i++)
        {
            workingAntsList[i].transform.position = positionArray[i];
            workingAntsList[i].ChangeStateNFinalTarget(whoToServe[i], antsState[i]);
        }
        setPathWorkers();
        for (int i = 0; i < workingAntsList.Count; i++)
        {
            workingAntsList[i].checkTarget();
            _antsTarget[i] = workingAntsList[i].target.position;
        }
    ReduceTimerGM();
    }

    //-------------------------------- GARDE MANGER ----------------------------------

    //fonction qui decique qui peut entrer le garde manger entre le premier de la ligne des reines et la ligne des sources de nourriture
    //cette decision est alléatoire de base, mais plusieurs conditions sont ajouter pour que la decision soit la bonne dans certaines situations, 
    //mais sinon elle est alléatoire
    private bool DecisionOnWhoEntersGMNext()
	{
        if (AntsInLineFromSource.Count == 0 && AntsInLineForQueen.Count == 0)
        {
            return false;
        }
        int rand = UnityEngine.Random.Range(0, 2);
        if (rand == 0)
            nextInGM = action.Food;
        else
            nextInGM = action.Queen;

        if (AntsInLineFromSource.Count == 0)
        {
            nextInGM = action.Queen;
        }
        else if (AntsInLineForQueen.Count == 0)
        {
            nextInGM = action.Food;
        }

        if (foodInGM <= 0 && AntsInLineFromSource.Count > 0)
            nextInGM = action.Food;
        else if (foodInGM <= 0 && AntsInLineFromSource.Count <= 0)
            nextInGM = action.Nothing;
        else if(foodInGM >= 0 && AntsInLineFromSource.Count <= 0)
            nextInGM = action.Queen;


        if (AntsInLineForQueen.Count == 0 && AntsInLineFromSource.Count == 0)
        {
            nextInGM = action.Nothing;
        }

        if (nextInGM == action.Food)
        {
            if (!AntsInLineFromSource[0].isFirstFromSource)
                return false;
            AntsInLineFromSource[0].GetOutLineFromSource();
            AntsInLineFromSource.RemoveAt(0);
        }
        else if (nextInGM == action.Queen)
        {
            if (!AntsInLineForQueen[0].isFirstForQueen)
                return false;
            AntsInLineForQueen[0].GetOutLineForQueen();
            AntsInLineForQueen.RemoveAt(0);
        }
        else if (nextInGM == action.Nothing)
            return false;


        return true;

    }

    //fonction appeler chaque frame pour reduire le temps entre les fourmis qui peuvent entre dans le garde mangé
    //lorsque le timer arrive à 0 la decsion sur qui peut entre dans le garde mangé est prise
    private void ReduceTimerGM()
    {
        timerPPl -= Time.deltaTime;
        if (timerPPl <= 0)
        {
            bool someWentIn = DecisionOnWhoEntersGMNext();
            if(someWentIn)
                timerPPl = timeInBetweenPeople;

        }
    }

    //fonction qui ajoute de la nourriture dans le garde mangé
    public void PutFoodInGM()
	{
        foodInGM += 1;
        UIFood.text = (foodInGM).ToString();
        GMmanager.AddLeafGM();
    }
    //fonction qui enlève de la nourriture dans le garde mangé
    public void TakeFoodFromGM()
	{
        foodInGM -= 1;
        UIFood.text = (foodInGM).ToString();
        GMmanager.RemoveLeafGM();
    }

    //-------------------------------- FOOD PER QUEEN ----------------------------------
    //fonction qui change la quantité de nourriture que chaque reine a dans un tableau de int utilise dans la prise de decision
    public void UpdateFoodForQueen()
    {
        for (int i = 0; i < FoodPerQueen.Length; i++)
		{
            FoodPerQueen[i] = queenList[i].GetFoodQuantity();
        }
    }

    //donction qui retourne le chamin vers la reine qui est demandé
    public List<Transform> GetPathQueenByID(int id)
	{
        if (id == 0)
            return PathToQueen;
        else if(id == 1)
            return PathToQueen1;
        else if(id == 2)
            return PathToQueen2;
        else if(id == 3)
            return PathToQueen3;
        return PathToQueen4;
     }
    //fonction qui ajoute de la nourrite à la queen demandé
    public void AddToQueen(int id)
    {
        if(id < queenList.Count)
        {
            QuennAnt queen = queenList[id];
            queen.AddOneFood();
        }
    }
    //-------------------------------- FOOD PER SOURCE ----------------------------------
    //fonction qui change la quantité de nourriture que chaque soource a dans un tableau de int utilise dans la prise de decision
    public void UpdateFoodSource()
    {
		for (int i = 0; i < FoodPerSource.Length; i++)
		{
            FoodPerSource[i] = foodSourceList[i].GetFoodQuantity();
		}
    }

    //fonction qui enlève une feuille de la source qui est demandé
    public void RemoveFromSource(int id)
    {
        if (id < foodSourceList.Count)
        {
            FoodPointManager foodPoint = foodSourceList[id];
            foodPoint.RemoveLeaf();
        }
    }

    //change le state d'une fourmi par son id dans un tableau utiliser pour la prise de decision
    public void ChangeSetSingleAnt(int idAnt, int newState)
	{
        antsState[idAnt] = newState;
	}

    //mets le path qui faut à chaque fourmis à chaque update
    private void setPathWorkers()
	{
        for(int i = 0; i < workingAntsList.Count; i++)
		{
            if(whoToServe[i] == 0)
                workingAntsList[i].SetPath(PathToQueen);
            else if(whoToServe[i] == 1)
                workingAntsList[i].SetPath(PathToQueen1);
            else if(whoToServe[i] == 2)
                workingAntsList[i].SetPath(PathToQueen2);
            else if(whoToServe[i] == 3)
                workingAntsList[i].SetPath(PathToQueen3);
            else if(whoToServe[i] == 4)
                workingAntsList[i].SetPath(PathToQueen4);
            else if(whoToServe[i] == 5)
                workingAntsList[i].SetPath(PathToFood);
            else if(whoToServe[i] == 6)
                workingAntsList[i].SetPath(PathToFood1);
            else if(whoToServe[i] == 7)
                workingAntsList[i].SetPath(PathToFood2);
            else if(whoToServe[i] == 8)
                workingAntsList[i].SetPath(PathToFood3);

        }
	}


    //---------------------------------------- Initialisation de tous les tableaux -----------------------
    void inizializeQueens()
    {
        FoodPerQueen = new NativeArray<int>(queenList.Count, Allocator.TempJob);
        for (int i = 0; i < queenList.Count; i++)
        {
            queenList[i].SetAmountOfFood(startingFoodQueen);
            queenList[i].id = i;
            FoodPerQueen[i] = queenList[i].GetFoodQuantity();
        }
    }
    
    void inizializeSource()
    {

        FoodPerSource = new NativeArray<int>(foodSourceList.Count, Allocator.TempJob);
        for (int i = 0; i < foodSourceList.Count; i++)
        {
            FoodPerSource[i] = foodSourceList[i].GetFoodQuantityOnStart();
            foodSourceList[i].id = i + queenList.Count;
        }
    }
    void inizializeWorkers()
    {
        antsPosition = new List<Transform>();
        workingAntsList = new List<WorkingAnts>();
        List<Transform> antsTarget = new List<Transform>();
        AntsInLineFromSource = new List<WorkingAnts>();
        AntsInLineForQueen = new List<WorkingAnts>();
        List<int> antsNewTarget = new List<int>();
        for (int i = 0; i < numberofAnts; i++)
        {
            antsTarget.Add(_target);
            antsNewTarget.Add(0);
            antsPosition.Add(spawnPoints.transform);
            Transform workingAntTransform = Instantiate(antPrefab, spawnPoints.transform.position, Quaternion.identity);
            workingAntsList.Add(new WorkingAnts
            {
                transform = workingAntTransform,
                target = DoNothingPlace,
                speed = UnityEngine.Random.Range(antSpeed - 2, antSpeed),
                rotationSpeed = 720,
                state = 0,
                whoToServe = 0,
                id = i,
                manager = this,
                LineGMFromQueen = PathToGMFromQueens,
                LineGMFromSource = PathToGMFromSources
            });
            workingAntsList[i].AddLeafManager();
        }

        positionArray = new NativeArray<float3>(workingAntsList.Count, Allocator.TempJob);
        _antsTarget = new NativeArray<float3>(antsTarget.Count, Allocator.TempJob);
        speed_Ants = new NativeArray<float>(workingAntsList.Count, Allocator.TempJob);
        antsState = new NativeArray<int>(workingAntsList.Count, Allocator.TempJob);
        whoToServe = new NativeArray<int>(workingAntsList.Count, Allocator.TempJob);
        for (int i = 0; i < workingAntsList.Count; i++)
		{
            speed_Ants[i] = workingAntsList[i].speed;
            positionArray[i] = workingAntsList[i].transform.position;
            _antsTarget[i] = workingAntsList[i].target.position;
            antsState[i] = 0;
            whoToServe[i] = 0;
		}
    }
}

//fonction qui change la position de chaque fourmi en parallèle
[BurstCompile]
public struct MoveAnts : IJobParallelFor
{
    public NativeArray<float3> PositionAnts;
    public NativeArray<float3> targetAnts;
    public NativeArray<float> antsSpeed;

    [ReadOnly] public float deltaTime;
	public void Execute(int index)
	{
        float3 dir = PositionAnts[index] - targetAnts[index];

        float magnitude = math.sqrt(math.pow(dir.x, 2) + math.pow(dir.y, 2));
        if(magnitude > 0.05)
        {
            dir.x = dir.x / magnitude;
            dir.y = dir.y / magnitude;
            dir.z = 0;
            PositionAnts[index] -= dir * antsSpeed[index] * deltaTime;
        }   
    }
}

//fonction qui prend une decision sur quoi faire en parallele pour chaque fourmi
[BurstCompile]
public struct DecisionAnts : IJobParallelFor
{

    /*
        0 - Nothing,
        1 - goingToTakeFood,
        2 - HaveFoodInHand,
        3 - WaitLineQueen,
        4 - TakingFoodToQueen,
        5 - CheckWhichQueenToServe,
        6 - WaitLineFood,
        7 - ReturningFromQueen,
        8 - goingToDoNothing,
        9 - oingBackWithOutFood
    
    */
    public NativeArray<int> _whoToServe;
    public NativeArray<int> _antsState;
    [ReadOnly] public NativeArray<int> foodPerQueen;
    [ReadOnly] public NativeArray<int> foodPerFoodSource;
    public int antsDoingNothing;
    public int numberOfAntsDoingnothingMax;
    public int threshOldFeedingQueen;
    public int threshOldTooManyPeopleInLineSource;
    public int threshOldTooManyPeopleInLineQueen;
    public int foodInGM;
    public int PeopleLineQueen;
    public int PeopleLineSource;

    public void Execute(int index)
    {
        //lorsqu'une fourmi est dans un certain state elle ne prend pas de decision, car c'est fait dans l'object de la fourmi
        if (_antsState[index] == 2 || _antsState[index] == 3 || _antsState[index] == 4 || _antsState[index] == 6 || _antsState[index] == 7 || _antsState[index] == 8 || _antsState[index] == 9)
            return;

        //pour savoir quelle reine a moins de nourriture
        int queenLessFood = 1000000;
        for (int i = 0; i < foodPerQueen.Length; i++)
        {
            if (foodPerQueen[i] < queenLessFood && foodPerQueen[i] > 0)
            {
                queenLessFood = foodPerQueen[i];
            }
        }

        bool justChanged = false;
        //prend une decision sur quoi faire si elle fait rien
        if (_antsState[index] == 0)
        {
            if (queenLessFood < threshOldFeedingQueen)
			{
                _antsState[index] = 3;
                return;
			}else if(foodInGM < PeopleLineQueen)
			{
                _antsState[index] = 1;
                justChanged = true;
            }
            else if(PeopleLineSource > threshOldTooManyPeopleInLineSource && PeopleLineQueen > threshOldTooManyPeopleInLineQueen)
			{
                return;
			}else if(PeopleLineSource < threshOldTooManyPeopleInLineSource && PeopleLineQueen > threshOldTooManyPeopleInLineQueen)
            {
                _antsState[index] = 1;
                justChanged = true;
            }
            else if(PeopleLineSource > threshOldTooManyPeopleInLineSource && PeopleLineQueen < threshOldTooManyPeopleInLineQueen)
            {
                _antsState[index] = 3;
                return;
			}
			else
			{
                if(index % 2 == 1)
                {
                    _antsState[index] = 3;
                    return;
                }
                _antsState[index] = 1;
                justChanged = true;
            }
		}
        //prend une decision sur quoi quelle source visé losqu'elle va prendre de la nourriture a une source
        if (_antsState[index] == 1)
        {

            int numberSourceWithFood = 0;
            for (int i = 0; i < foodPerFoodSource.Length; i++)
            {
                if (foodPerFoodSource[i] > 0)
                    numberSourceWithFood++;
            }

            if (numberSourceWithFood <= 0)
			{
                _antsState[index] = 9;
                return;
			}

			if (justChanged)
            {
                int idhighest = -1;
                int quantity = 0;
                for (int i = 0; i < foodPerFoodSource.Length; i++)
                {
                    if (foodPerFoodSource[i] > quantity && foodPerFoodSource[i] > 0)
                    {
                        idhighest = i;
                        quantity = foodPerFoodSource[i];
                    }
                }
                if (idhighest != -1)
                {
                    _antsState[index] = 1;
                    _whoToServe[index] = idhighest + 5;
                }
                else
                    _antsState[index] = 8;
			}
            //cette parti est pour que si la source est vide pendant que la fourmi allait dessus, celle-ci change vers une autre source qui a de la nourriture
			else if(_antsState[index] != 5)
			{
                int idhighest = -1;
                int quantity = 0;
                for (int i = 0; i < foodPerFoodSource.Length; i++)
                {
                    if (foodPerFoodSource[i] > quantity && foodPerFoodSource[i] > 0)
                    {
                        idhighest = i;
                    }
                }
                if(idhighest == -1)
				{
                    _antsState[index] = 9;
                    _whoToServe[index] = 11;
				}
                else if (foodPerFoodSource[_whoToServe[index] - 5] <= 0)
                {
                    _whoToServe[index] = idhighest + 5;
                }
            }

        }
        //prend la decision sur quelle reine aller nourrire selon la quantité de nourriture de chaque reine
        if (_antsState[index] == 5)
        {
            int idlowestQueen = -1;
            int quantity = 1000000000;
            for (int i = 0; i < foodPerQueen.Length; i++)
            {
                if (foodPerQueen[i] < quantity && foodPerQueen[i] > 0)
                {
                    idlowestQueen = i;
                    quantity = foodPerQueen[i];
                }
            }
            if (idlowestQueen != -1)
            {
                _antsState[index] = 4;
                _whoToServe[index] = idlowestQueen;
            }
            else
                _antsState[index] = 8;
            return;
        }
    }
}
