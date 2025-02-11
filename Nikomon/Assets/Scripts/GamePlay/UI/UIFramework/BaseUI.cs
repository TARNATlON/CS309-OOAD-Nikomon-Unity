﻿using System;
using System.Collections;
using System.Collections.Generic;
using GamePlay.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GamePlay.UI.UIFramework
{
    enum ShowType
    {
        Type1,
        Type2
    }

    public interface IUIAnimator
    {
        void OnEnterAnimator();
        void OnExitAnimator();
    }

    public abstract class BaseUI : MonoBehaviour
    {
        public enum GET_TYPE
        {
            GameObject,
            Component
        }

        public virtual UILayer Layer { get; set; } = UILayer.NormalUI;

        public virtual bool IsOnly { get; }

        // public virtual bool IsBlockPlayerControl { get; set; }
        public virtual float DisplayTime { get; } = -1; //-1表示一直显示

        // private bool CanPlayerControlBefore;

        private bool _canQuitNow = true;

        protected bool CanQuitNow
        {
            get => _canQuitNow;
            set
            {
                _canQuitNow = value;
                if (ExitBtn != null)
                    ExitBtn.onClick.RemoveAllListeners();

                if (ExitBtn != null && _canQuitNow)
                {
                    ExitBtn.onClick.AddListener(() => UIManager.Instance.Hide(this));
                }
            }
        }


        private List<CancelTrigger> _cancelTriggers = new List<CancelTrigger>();


        /// <summary>
        /// 用于手柄，当窗口打开时默认选中哪一种元素
        /// </summary>
        public GameObject FirstSelectable;

        public Button ExitBtn;

        public virtual void Init(params object[] args)
        {
        }

        public IEnumerator DoExit()
        {
            if (ExitBtn != null && _canQuitNow)
            {
                EventSystem.current.SetSelectedGameObject(ExitBtn.gameObject);
                yield return new WaitForSeconds(0.25f);
                UIManager.Instance.Hide(this);
            }
            else
            {
                if (_canQuitNow)
                    UIManager.Instance.Hide(this);
            }
        }

        public IEnumerator TimeToExit(float time)
        {
            yield return new WaitForSeconds(time);
            UIManager.Instance.Hide(this);
        }

        

        protected T GET<T>(T obj, string name, GET_TYPE type = GET_TYPE.Component) where T : UnityEngine.Object
        {
            if (this.name.Equals(name))
            {
                if (type == GET_TYPE.Component)
                {
                    T result = obj != null ? obj : this.GetComponent<T>();
                    return result;
                }
                else if (type == GET_TYPE.GameObject)
                {
                    T result = obj != null ? obj : this.gameObject as T;
                    return result;
                }
            }
            else
            {
                if (type == GET_TYPE.Component)
                {
                    T result = obj != null ? obj : transform.Find(name).GetComponent<T>();
                    return result;
                }
                else if (type == GET_TYPE.GameObject)
                {
                    T result = obj != null ? obj : transform.Find(name).gameObject as T;
                    return result;
                }
            }


            return null;
        }


        public virtual void OnEnter(params object[] args)
        {
            this.gameObject.SetActive(true);
            this.Init(args);
            if (this is IUIAnimator) (this as IUIAnimator).OnEnterAnimator();

            if (ExitBtn != null)
            {
                ExitBtn.onClick.RemoveAllListeners();
                ExitBtn.onClick.AddListener(() => { UIManager.Instance.Hide(this); });
            }

            // CanPlayerControlBefore = GlobalManager.Instance.CanPlayerControlled;
            // if (IsBlockPlayerControl) GlobalManager.Instance.CanPlayerControlled = false;


            _cancelTriggers.AddRange(GetComponentsInChildren<CancelTrigger>());
            foreach (var cancelTrigger in _cancelTriggers)
            {
                cancelTrigger.cancel = (o) => StartCoroutine(DoExit());
            }

            if (FirstSelectable != null)
            {
                EventSystem.current.SetSelectedGameObject(FirstSelectable.gameObject);
            }

            if (DisplayTime > 0)
            {
                StartCoroutine(TimeToExit(DisplayTime));
            }
            
            gameObject.GetOrAddComponent<CanvasGroup>().interactable = true;
        }

        public virtual void OnExit()
        {
            // print("Onexit");
            if (this is IUIAnimator) (this as IUIAnimator).OnExitAnimator();
            else gameObject.SetActive(false);

            // GlobalManager.Instance.CanPlayerControlled = CanPlayerControlBefore;
        }

        private GameObject currentSelectObj;

        /// <summary>
        /// 这个是用于当别的窗口叠在本窗口之上时，本窗口内暂停
        /// </summary>
        public virtual void OnPause()
        {
            currentSelectObj = EventSystem.current.currentSelectedGameObject;
            gameObject.GetOrAddComponent<CanvasGroup>().interactable = false;
        }

        /// <summary>
        /// 当别的窗口被pop掉时，本窗口继续
        /// </summary>
        public virtual void OnResume()
        {
            gameObject.SetActive(true);
            
            gameObject.GetOrAddComponent<CanvasGroup>().interactable = true;

            if (this is IUIAnimator)
            {
                print($"{this.GetType().Name} IUI Animator Resume");
                (this as IUIAnimator)?.OnEnterAnimator();
            }

            if (currentSelectObj != null)
            {
                EventSystem.current.SetSelectedGameObject(currentSelectObj);
            }
        }

        public virtual void OnRefresh(params object[] args)
        {
        }
    }
}