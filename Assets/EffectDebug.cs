﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectDebug : MonoBehaviour {

    // Use this for initialization
    public string[] Eff;
    void Start () {
        SFXLoader.Instance.Init();
        Eff = SFXLoader.Instance.Eff;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void PlayEffect(string sfx)
    {
        sfx += ".ef";
        SFXLoader.Instance.PlayEffect(sfx, gameObject, false);
    }

    public void PlayEffect(int idx)
    {
        SFXLoader.Instance.PlayEffect(idx, gameObject, false);
    }
}
