using Assets.Fileparsers;
using Assets.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour {

    public RawImage Background;
    private CacheManager _cacheManager;

    // Use this for initialization
    void Start () {
        _cacheManager = FindObjectOfType<CacheManager>();
        Background.gameObject.SetActive(false);
        ShowMenu("6mainmn1");

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void ShowMenu(string backgroundFilename)
    {
        var texture = _cacheManager.GetTexture(backgroundFilename);
        Background.texture = texture;
        Background.rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
        Background.gameObject.SetActive(true);
    }
}
