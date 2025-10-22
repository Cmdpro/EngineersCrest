using HutongGames.PlayMaker.Actions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EngineersCrest
{
    public class OneTimeAudio : MonoBehaviour
    {
        public AudioSource audioSource;
        public void Start()
        {
            audioSource = GetComponent<AudioSource>();
        }
        public void Update()
        {
            if (!audioSource.isPlaying)
            {
                Destroy(this.gameObject);
            }
        }
    }
}
