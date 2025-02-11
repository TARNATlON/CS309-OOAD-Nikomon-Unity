﻿using System;
using System.Collections.Generic;
using GamePlay.UI.BattleUI;
using GamePlay.UI.UIFramework;
using PokemonCore;
using PokemonCore.Character;
using PokemonCore.Combat;
using UnityEngine;
using UnityEngine.UI;
using Debug = PokemonCore.Debug;

namespace GamePlay.UI.UtilUI
{
    public class AbilityPanel : BaseUI
    {
        public GameObject abilityTable;
        private List<MoveElement> _moveElements=new List<MoveElement>();
        private GameObject MoveElementPrefab;
        public Transform MoveDetial;
        public Trainer trainer;
        public Pokemon Pokemon;
        public RadarTest _radarTest;
        public Nametext _nametext;
        public Content2text _Content2;
        public GameObject pokenmons;
        public Text movecontent;
        public GameObject content2;
        public GameObject content3;
        public override bool IsOnly { get; } = true;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">0 for trainer,1 for pokenmon</
        public override void OnEnter(params object[] args)
        {
            movecontent.text = "";
            MoveElementPrefab = GameResources.SpawnPrefab(typeof(MoveElement));
            if (_moveElements.Count == 0)
            {
                for (int i = 0; i < Game.MaxMovesPerPokemon; i++)
                {
                    GameObject obj = Instantiate(MoveElementPrefab, MoveDetial);
                    obj.name = "Move" + i;
                    obj.GetComponent<TriggerSelect>().onSelect = () =>
                    {
                        //TODO:技能描述！！！不好处理！！！！
                        // description;
                        movecontent.text = Messages.Messages.Get("Move"+obj.GetComponent<MoveElement>()._move.moveID) ; //._move._baseData.description;
                        //movecontent.text = obj.name;

                    };
                    _moveElements.Add(obj.GetComponent<MoveElement>());
                }
            }
            base.OnEnter(args);
            if (args != null)
            {
                trainer=args[0] as Trainer;
                Pokemon=args[1] as Pokemon;
            }
            for (int i = 0; i < Pokemon.moves.Length; i++)
            {
                if (Pokemon.moves[i] == null)
                {
                    _moveElements[i].gameObject.SetActive(false);
                }
                else
                {
                    _moveElements[i].Init(Pokemon.moves[i]);
                    _moveElements[i].gameObject.SetActive(true);
                }
                
            }
            foreach (Transform child in pokenmons.transform)
            {
                // print(child.name);
                child.gameObject.SetActive(false);
            }
            string name = Pokemon.ID.ToString() + Pokemon._base.innerName;
            pokenmons.SetActive(true);
            print(name);
            pokenmons.transform.Find(name).gameObject.SetActive(true);
            _radarTest.Init(Pokemon);
            _nametext.Init(Pokemon,trainer);
            _Content2.Init(Pokemon);

            content2.GetComponent<TriggerSelect>().onDeSelect = () =>
            {
                _nametext.Init(Pokemon,trainer);
                _radarTest.Init(Pokemon);
            };
            content2.GetComponent<TriggerSelect>().onSelect = () =>
            {
                _radarTest.Init(Pokemon);
                _Content2.Init(Pokemon);
            };
            
            content2.GetComponent<TriggerSelect>().onSelect = () =>
            {
               
            };

            // content2.GetComponent<TriggerSelect>().onDeSelect = () =>
            // {
            //     _nametext.Init(Pokemon,trainer);
            //     _radarTest.Init(Pokemon);
            // };
            // content2.GetComponent<TriggerSelect>().onSelect = () =>
            // {
            //     _radarTest.Init(Pokemon);
            //     _Content2.Init(Pokemon);
            // };
            
            
            // print(Pokemon.Name);
            //
            // _radarTest.Init(Pokemon);
            // _nametext.Init(Pokemon,trainer);
            // _Content2.Init(Pokemon);



        }

        
        
        
    }
}