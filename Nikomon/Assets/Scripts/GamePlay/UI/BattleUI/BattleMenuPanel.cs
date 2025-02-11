﻿using System;
using System.Collections.Generic;
using System.Linq;
using GamePlay.UI.PokemonChooserTable;
using GamePlay.UI.UIFramework;
using GamePlay.UI.UtilUI;
using PokemonCore;
using PokemonCore.Attack;
using PokemonCore.Attack.Data;
using PokemonCore.Combat;
using PokemonCore.Inventory;
using PokemonCore.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.UI.BattleUI
{
    public class BattleMenuPanel : BaseUI, IUIAnimator
    {
        public Button Fight;
        public Button Pokemon;
        public Button Bag;
        public Button Run;

        private readonly string[] PokemonChooses = new[] {"Switch", "Show Ability", "Cancel"};
        private readonly string[] BagChooses = new[] {"Use", "Cancel"};

        public override bool IsOnly => true;


        private void HandlePokemonChoose(int chooseIndex, int bagIndex)
        {
            // print(PokemonChooses[index]);
            switch (chooseIndex)
            {
                case 0: //Switch

                    if (Game.trainer.party[bagIndex] == null || Game.trainer.pokemonOnTheBattle[bagIndex] ||
                        Game.trainer.party[bagIndex].HP <= 0) return;
                    Instruction ins = new Instruction(currentPoke.CombatID, Command.SwitchPokemon, bagIndex,
                        null);
                    BuildInstrustruction(ins);

                    break;

                case 1: //Show Ability
                    UIManager.Instance.Show<AbilityPanel>(Game.trainer, Game.trainer.party[bagIndex]);
                    break;
                case 2: //Cancel
                    // UIManager.Instance.Hide(this);
                    break;
            }

            UIManager.Instance.Hide<PokemonChooserPanelUI>();
        }

        private void HandleItem(int optionIndex, Item item)
        {
            switch (optionIndex)
            {
                case 0:
                    // UIManager.Instance.Show<TargetChooserPanel>();
                    //TODO:实现更复杂的效果
                    UseItem(item, BattleHandler.Instance.OpponentPokemons[0].CombatID);
                    UIManager.Instance.Hide<BagPanelUI>();
                    break;
                case 1:
                    break;
            }

            UIManager.Instance.Hide<BagPanelUI>();
        }

        public override void Init(params object[] args)
        {
            base.Init(args);

            bool[] canUse;
            if (args == null || args.Length == 0)
            {
                canUse = new[] {true, true, true, true};
            }
            else
            {
                canUse = (args[0] as List<bool>)?.ToArray();
            }

            if (canUse != null)
            {
                Fight.interactable = canUse[0];
                Pokemon.interactable = canUse[1];
                Bag.interactable = canUse[2];
                Run.interactable = canUse[3];
            }

            FirstSelectable = Fight.gameObject;

            Fight.onClick.RemoveAllListeners();
            Pokemon.onClick.RemoveAllListeners();
            Bag.onClick.RemoveAllListeners();
            Run.onClick.RemoveAllListeners();

            //Init Fight
            Fight.onClick.AddListener(() => { UIManager.Instance.Show<MovePanel>((Action<int>) ChooseMove); });

            //Init Pokemon
            Pokemon.onClick.AddListener(() =>
            {
                UIManager.Instance.Show<PokemonChooserPanelUI>(Game.trainer,
                    PokemonChooses,
                    (Action<int, int>) HandlePokemonChoose
                );
            });

            //Init Bag
            Bag.onClick.AddListener(() =>
            {
                UIManager.Instance.Show<BagPanelUI>(Game.bag, BagChooses.ToList(), (Action<int, Item>) HandleItem);
            });


            //Init Run
            Run.onClick.AddListener(ToRun);
#if UNITY_ANDROID||UNITY_IPHONE
            UIManager.Instance.Hide<VirtualControllerPanel>();
#endif
        }

        private CombatPokemon currentPoke => BattleHandler.Instance.CurrentPokemon;


        public void UseItem(Item item, int target)
        {
            if (currentPoke.HP > 0)
            {
                UseItem(item, new List<int>() {target});
            }
        }

        public void UseItem(Item item, List<int> target)
        {
            target.Insert(0, item.ID);
            Instruction ins = new Instruction(currentPoke.CombatID, Command.Items, (int) item.tag,
                target);
            BuildInstrustruction(ins);
        }

        public void ToRun()
        {
            if (currentPoke == null)
            {
                UIManager.Instance.Hide(this);
                return;
            }

            Instruction ins = new Instruction(currentPoke.CombatID, Command.Run, Game.trainer.id,
                null);
            BuildInstrustruction(ins);
        }

        public void ChooseMove(int index)
        {
            if (currentPoke.HP <= 0) return;
            Move move = currentPoke.pokemon.moves[index];
            //如果技能效果是针对对面宝可梦而且宝可梦只有一个的话
            if (move._baseData.Target == Targets.SELECTED_OPPONENT_POKEMON &&
                BattleHandler.Instance.OpponentPokemons.Count == 1)
            {
                UIManager.Instance.Hide(this);
                Instruction instruction =
                    new Instruction(currentPoke.CombatID, Command.Move, index,
                        BattleHandler.Instance.OpponentPokemons[0].CombatID);
                UIManager.Instance.Hide<MovePanel>();
                BuildInstrustruction(instruction);
            }
            else
            {
                // TargetChooserHandler.ShowTargetChooser(move._baseData.Target);
                // TargetChooserHandler.OnCancelChoose = () => { MoveUI.SetActive(true); };
                Action<List<int>> onChoose = (o) => OnChooseTarget(o, index);
                // Action onCancel = () => UIManager.Instance.Hide<Targetch>(this);
                UIManager.Instance.Show<TargetChooserPanel>(
                    ShowType.Type1,
                    BattleHandler.Instance.OpponentPokemons,
                    BattleHandler.Instance.AlliesPokemons,
                    move._baseData.Target,
                    onChoose,
                    null
                );
            }

            void OnChooseTarget(List<int> targets, int index)
            {
                UIManager.Instance.Hide<TargetChooserPanel>();
                // UIManager.Instance.Hide(this);
                // UnityEngine.Debug.Log(targets.ConverToString());
                Instruction instruction =
                    new Instruction(currentPoke.CombatID, Command.Move, index,
                        targets);
                UIManager.Instance.Hide<MovePanel>();
                BuildInstrustruction(instruction);
            }


            // UIManager.Instance.Refresh<DialogPanel>();
        }


        public void BuildInstrustruction(Instruction instruction)
        {
            BattleHandler.Instance.ReceiveInstruction(instruction);
        }

        public void OnEnterAnimator()
        {
            LeanTween.scale(gameObject, Vector3.one, 0.5f)
                .setOnStart(() =>
                {
                    gameObject.SetActive(false);
                    gameObject.SetActive(true);
                })
                .setOnComplete(() => { gameObject.SetActive(true); });
        }

        public void OnExitAnimator()
        {
            LeanTween.scale(gameObject, Vector3.zero, 0.5f).setOnComplete(() => { gameObject.SetActive(false); });
        }
    }
}