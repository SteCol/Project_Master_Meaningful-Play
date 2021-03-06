﻿//De speler zal moeten kunne wisselen tussen Steve en Hungry.
//Hij zal de drie perspectieven moeten kunnen zien.
//Hij zal de muziek moeten kunnen opzetten
//Stene zal iets zichtbaarders worden in hungry’s beeld
//Atilla zal dichter en dichter bij steve komen

//Dit zal worden voorgesteld als een grid van ui knoppen.
//Iedere knop is oftewel Steve, Hungry of Atilla.
//De knoppen binnen 10 knoppen van Steve worden dezelfde kleur.
//Iedere keer dat hij muziek speelt wordt de radius van gekleurde knoppen kleiner.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class scr_GameManager : MonoBehaviour
{
    [Header("Controls")]
    public int gridSize = 30;
    public GameObject buttonPrefab;
    public GameObject gridPanel;
    public int steveButtonIndex, possibleSteveButtonIndex, hungryButtonIndex, atillaButtonIndex;

    [Header("GamePlay stuff")]
    public int musicVolume;
    public IEnumerator activeCoreRoutine;
    public bool gameOver;

    [Header("UI Stuff")]
    public GameObject gameOverScreen;
    public Text resultText;

    [Header("Color")]
    public Color emptyColor;
    public Color obstacleColor;
    public Color steveColor;
    public Color hungryColor;
    public Color atillaColor;

    [Header("Grid")]
    public List<cls_Grid> grid;
    public List<cls_Grid> mightContainSteve;

    #region UI Button Stuff
    public void PlayRadio()
    {
        MoveAtilla();
        //RemoveRadioBox();
        ReduceCircle();
    }

    public void Restart()
    {
        SceneManager.LoadScene(0);
    }
    #endregion

    void Start()
    {
        //Make a grid of empty buttons
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                GameObject button = Instantiate(buttonPrefab, gridPanel.transform);
                button.name = "Button_" + i.ToString("000") + "x" + j.ToString("000");
                button.transform.localScale = new Vector3(1, 1, 1);

                int r = Random.Range(0, 100);

                if (r < 75)
                {
                    grid.Add(new cls_Grid(button.name, grid.Count, button, new Vector2(i, j), enum_Contains.Empty, emptyColor));

                }
                else
                {
                    grid.Add(new cls_Grid(button.name, grid.Count, button, new Vector2(i, j), enum_Contains.Obstackle, obstacleColor ));

                }
            }
        }

        //Assign Steve, Hungry and Atilla
        AddCharacters();

        //Draw the circle around Steve
        DrawSteveCircle(musicVolume, true);

        //Add onClick cause I sure as hell ain't doing that shit manually.
        foreach (cls_Grid g in grid)
        {
            g.button.GetComponent<Button>().onClick.AddListener(g.OnButtonClick);
            g.UpdateColor();
        }
    }

    //Draws a circle of size "_size" around steve
    void DrawSteveCircle(float _size, bool _newPosition)
    {
        List<int> possibleStevePositions = new List<int>();

        //Get a possible Steve Button Index
        for (int i = 0; i < grid.Count; i++)
        {
            float distance = Vector2.Distance(grid[steveButtonIndex].buttonPos, grid[i].buttonPos);
            if (distance < _size)
                possibleStevePositions.Add(i);
        }

        if (_newPosition)
            possibleSteveButtonIndex = possibleStevePositions[Random.Range(0, possibleStevePositions.Count - 1)];
        else
        {
            //Move the possibleSteveButtonIndex closer to steve
            float distance = Vector2.Distance(grid[possibleSteveButtonIndex].buttonPos, grid[steveButtonIndex].buttonPos);
            if (distance >= musicVolume)
                possibleSteveButtonIndex = ClosestButton(possibleSteveButtonIndex, 1.1f, grid[steveButtonIndex].buttonPos);
        }


        //Get the buttons to color
        foreach (cls_Grid g in grid)
        {
            float distance = Vector2.Distance(grid[possibleSteveButtonIndex].buttonPos, g.buttonPos);
            if (distance < _size && g.contains == enum_Contains.Empty)
            {
                g.buttonColor = steveColor;
                mightContainSteve.Add(g);
            }
        }

        Debug.Log("[StevePos: " + grid[steveButtonIndex].buttonPos + "] [PossibleStevePos :" + grid[possibleSteveButtonIndex].buttonPos + "]");
    }

    void ReduceCircle()
    {
        foreach (cls_Grid g in mightContainSteve)
        {
            g.buttonColor = emptyColor;
            g.UpdateColor();

        }
        musicVolume--;

        mightContainSteve.Clear();
        DrawSteveCircle(musicVolume, false);

        foreach (cls_Grid g in mightContainSteve)
        {
            g.buttonColor = steveColor;
            g.UpdateColor();
        }

    }

    void RemoveRadioBox()
    {
        int toRemove = Random.Range(0, mightContainSteve.Count);

        //This to make sure Steve is never revealed and Atilla & Hungry are never painted over
        if (mightContainSteve[toRemove].contains == enum_Contains.Empty)
        {
            mightContainSteve[toRemove].buttonColor = emptyColor;
            mightContainSteve[toRemove].UpdateColor();
        }
        else
            RemoveRadioBox();
    }

    #region Game Over Stuff
    void FixedUpdate()
    {
        if (!gameOver)
        {
            if (atillaButtonIndex == steveButtonIndex)
                GameOver("You've failed to reach Steve In time. He was eaten...");
            else if (hungryButtonIndex == steveButtonIndex)
                GameOver("You've reached Steve In time. Nice Job!");
        }
    }

    void GameOver(string _message)
    {
        gameOverScreen.SetActive(true);
        resultText.text = _message;
        gameOver = true;
    }
    #endregion

    #region Movement
    public int ClosestButton(int _index, float _distance, Vector2 _moveTo)
    {
        //Make a list of possible points to move to
        List<cls_Grid> buttonsInRange = GetInRange(grid[_index].buttonPos, _distance);

        //Set up a "furthest away point"
        float closestDistance = 1000.0f;
        cls_Grid closestButton = grid[_index];

        //Get the closest button
        foreach (cls_Grid g in buttonsInRange)
        {
            float distance = Vector2.Distance(_moveTo, g.buttonPos);
            if (distance < closestDistance)
            {
                closestButton = g;
                closestDistance = distance;
            }
        }

        //Return the index, used to find it in the grid list
        return closestButton.index;
    }

    public void MoveHungry(Vector2 _moveTo)
    {
        int closestButtonIndex = ClosestButton(hungryButtonIndex, 1.1f, _moveTo);

        //Clear the old button
        SetCharacter(enum_Contains.Empty, hungryButtonIndex, emptyColor);

        //Set the new button
        SetCharacter(enum_Contains.Hungry, closestButtonIndex, hungryColor);
        hungryButtonIndex = closestButtonIndex;

        //Redraw the grid colors
        foreach (cls_Grid g in grid)
            g.UpdateColor();
    }

    public void MoveAtilla()
    {
        int closestButtonIndex = ClosestButton(atillaButtonIndex, 1.1f, grid[steveButtonIndex].buttonPos);

        //Clear the old atilla button
        SetCharacter(enum_Contains.Empty, atillaButtonIndex, emptyColor);

        //Set up the new atilla button
        SetCharacter(enum_Contains.Atilla, closestButtonIndex, atillaColor);
        atillaButtonIndex = closestButtonIndex;

        //Redraw the grid colors
        foreach (cls_Grid g in grid)
            g.UpdateColor();
    }
    #endregion

    #region Calculation stuff
    //Returns a list of the closest by buttons
    List<cls_Grid> GetInRange(Vector2 _from, float _distance)
    {
        List<cls_Grid> buttonIndexes = new List<cls_Grid>();

        for (int i = 0; i < grid.Count; i++)
        {
            float distance = Vector2.Distance(_from, grid[i].buttonPos);
            if (distance < _distance)
                buttonIndexes.Add(grid[i]);
        }

        return buttonIndexes;
    }
    #endregion

    #region Game Setup stuff
    void AddCharacters()
    {
        //Set the buttons
        int randomButton = Random.Range(0, grid.Count - 3);
        int randomDistance = Random.Range(50, 150);

        steveButtonIndex = randomButton;
        hungryButtonIndex = 0;
        atillaButtonIndex = grid.Count - 1;

        //Set the characters
        SetCharacter(enum_Contains.Steve, steveButtonIndex, steveColor);
        SetCharacter(enum_Contains.Hungry, hungryButtonIndex, hungryColor);
        SetCharacter(enum_Contains.Atilla, atillaButtonIndex, atillaColor);
    }

    void SetCharacter(enum_Contains _contains, int _buttonIndex, Color _color)
    {
        grid[_buttonIndex].contains = _contains;
        grid[_buttonIndex].buttonColor = _color;
    }
    #endregion
}

[System.Serializable]
public class cls_Grid
{
    public string buttonName;
    public int index;
    public GameObject button;
    public enum_Contains contains;
    public Vector2 buttonPos;
    public Color buttonColor;

    public cls_Grid(string _buttonName, int _index, GameObject _button, Vector2 _buttonPos, enum_Contains _contains, Color _buttonColor)
    {
        buttonName = _buttonName;
        index = _index;
        button = _button;
        buttonPos = _buttonPos;
        contains = _contains;
        buttonColor = _buttonColor;
    }

    public void UpdateColor()
    {
        button.GetComponent<Image>().color = buttonColor;
    }

    public void OnButtonClick()
    {
        Debug.Log(button.name + " Was Clicked " + "[" + index + ", " + contains + "]");
        GameObject.FindGameObjectWithTag("GameController").GetComponent<scr_GameManager>().MoveHungry(buttonPos);
        GameObject.FindGameObjectWithTag("GameController").GetComponent<scr_GameManager>().MoveAtilla();

    }
}

public enum enum_Contains
{
    Empty,
    Obstackle,
    MightContainSteve,
    Steve,
    Hungry,
    Atilla
}