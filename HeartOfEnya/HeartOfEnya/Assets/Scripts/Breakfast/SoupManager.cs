﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// controls all soup-related items & behaviors, including what buttons are active, what ingredients are selected, etc.
/// </summary>
[System.Serializable]
public class SoupManager : MonoBehaviour
{
	//globally-accessible instance used to access the data.
	public static SoupManager main;

	//editor-facing data about the ingredient list
    public int totalIngredients; //how many ingredients we have available
    public int ingredientsPerSoup; //how many ingredients are used per soup
    public List<Ingredient> ingredients = new List<Ingredient>(); //list of all ingredients we have
    public Ingredient defaultIngredient; //emergency ingredient, used to pad out the list if players collect too few ingredients to make soup

    //UI details
    public Transform parentCanvas; //the UI canvas that's the parent of the buttons
    public IngredientButton buttonPrefab; //button prefab used as a template
    public int buttonX; //x-coord of buttons
    public int buttonY; //max y-coord of buttons
    public int buttonOffset; //distance between buttons. Should be >= the size of the button (see the prefab)
    public SoupButton soupButton; //reference to the "make soup" button

    //internal variables used for state tracking, etc.
    private List<Ingredient> activeIngredients = new List<Ingredient>(); // selected ingredients

    private FMODUnity.StudioEventEmitter sfxSelect;
    private FMODUnity.StudioEventEmitter sfxCancel;

    /// <summary>
    /// Implements the singleton pattern
    /// </summary>
    private void Awake()
    {
        if (main == null)
        {
            main = this;
            Initialize(); //spawn the buttons & perform other startup tasks
        }
        else
            Destroy(gameObject); //there can only be one singleton

    }

    /// <summary>
    /// Performs 1st-time setup, primarily spawning the ingredient buttons & initializing a few variables
    /// </summary>
    private void Initialize()
    {
    	sfxSelect = GameObject.Find("UISelect").GetComponent<FMODUnity.StudioEventEmitter>();
        sfxCancel = GameObject.Find("UICancel").GetComponent<FMODUnity.StudioEventEmitter>();

    	//spawn ingredient buttons
    	int totalSpawned = 0; //how many buttons we've currently spawned
        //make sure we don't spawn FP ingredients before they're introduced, or Lua ingredients before she unfreezes
        ingredients = ingredients.FindAll(IsValidIng);
        while(totalSpawned < 6 && ingredients.Count > 0)
    	{
    		//pick a random ingredient that we haven't spawned yet
    		int choice = Random.Range(0, ingredients.Count);
    		Debug.Log("chosen ingredient " + choice + ", AKA " + ingredients[choice].name);
    		
    		//spawn the button
    		var button = Instantiate(buttonPrefab);
            //connect it to the UI canvas and move it to the next open slot
            button.transform.SetParent(parentCanvas.transform, false);
            button.transform.localPosition = new Vector3(buttonX, buttonY - (totalSpawned * buttonOffset), 0);
            button.ingredient = ingredients[choice]; //set its ingredient
            button.UpdateData(); //tell the button to refresh its images with data from the new ingredient

            //remove the chosen ingredient from our options & increment totalSpawned
            ingredients.RemoveAt(choice);
            totalSpawned++;
    	}

        //sanity check - if fewer than ingredientsPerSoup buttons were spawned, pad out to ingredientsPerSoup ingredients using the default ingredient
        while (totalSpawned < ingredientsPerSoup)
        {
            //spawn the button
            var button = Instantiate(buttonPrefab);
            //connect it to the UI canvas and move it to the next open slot
            button.transform.SetParent(parentCanvas.transform, false);
            button.transform.localPosition = new Vector3(buttonX, buttonY - (totalSpawned * buttonOffset), 0);
            button.ingredient = defaultIngredient; //set its ingredient
            button.UpdateData(); //tell the button to refresh its images with data from the new ingredient

            totalSpawned++;
        }
    }

    /// <summary>
    /// Called when the user de-selects an ingredient.
    /// Removes its ingredient from the list of active ingredients and tells the "make soup" button to update itself
    /// </summary>
    public void DisableIngredient(Ingredient ing)
    {
    	sfxCancel.Play();
        activeIngredients.Remove(ing);
    	soupButton.UpdateRecipe(activeIngredients);
    }

    /// <summary>
    /// Called when the user selects an ingredient.
    /// If there's still room in the soup, adds its ingredient to the list of active ingredients, tells the "make soup" button to update itself,
    /// and returns true.
    /// If there's no more room in the soup, returns false.
    /// </summary>
    public bool EnableIngredient(Ingredient ing)
    {
    	if (activeIngredients.Contains(ing))
    	{
    		//fail if already enabled
    		return false;
    	}
    	else if (activeIngredients.Count >= ingredientsPerSoup)
    	{
    		//fail if we've already filled all ingredient slots
    		return false;
    	}
    	else
    	{
    		sfxSelect.Play();

            //add ingredients to soup and tell the soup button to update its images
    		activeIngredients.Add(ing);
    		soupButton.UpdateRecipe(activeIngredients);
    		return true; //report success
    	}
    }

    /// <summary>
    /// Function that checks whether an ingredient is valid for the current game phase
    /// (i.e. no FP before that mechanic is introduced & no Lua before she's found).
    /// Used in filtering the ingredients prior to spawning them
    /// </summary>
    private static bool IsValidIng(Ingredient ing)
    {
        var pData = DoNotDestroyOnLoad.Instance.persistentData;
        if (pData.InTutorialFirstDay || pData.InTutorialSecondDay)
        {
            if (ing.effectType == Ingredient.Effect.restore)
            {
                return false;
            }
        }

        //filter Lua ingredients if she's not free
        if (!pData.LuaUnfrozen)
        {
            if (ing.target == Ingredient.Character.lua)
            {
                return false;
            }
        }

        //if neither of the above is true, the ingredient is valid
        return true;
    }
}
