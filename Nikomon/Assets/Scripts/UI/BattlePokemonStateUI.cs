using System;
using System.Collections;
using System.Collections.Generic;
using PokemonCore.Combat;
using UnityEngine;
using UnityEngine.UI;

public class BattlePokemonStateUI : MonoBehaviour
{
    public bool isOpponent;
    public Text NameText;
    public Text LevelText;
    public Text HealthText;
    public Slider HealthSlider;
    public Slider ExpSlider;
    
    public void Init(CombatPokemon pokemon)
    {
        NameText.text = pokemon.Name;
        LevelText.text = "Lv." + pokemon.Level;
        HealthText.text = pokemon.HP + "/" + pokemon.TotalHP;
        HealthSlider.value = pokemon.HP / (float)pokemon.TotalHP;
        ExpSlider.value = pokemon.pokemon.Exp.Current / (float)pokemon.pokemon.Exp.NextLevelExp;
    }

    public void UpdateState(CombatPokemon pokemon)
    {
        LevelText.text = "Lv." + pokemon.Level;
        HealthText.text = pokemon.HP + "/" + pokemon.TotalHP;
        HealthSlider.value = pokemon.HP / (float)pokemon.TotalHP;
    }
    
    
}
