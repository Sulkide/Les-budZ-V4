using System;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sulkide.Dialogue
{
    [Serializable]
    public enum Speaker
    {
        Player = 0,
        NPC    = 1,
        Sulkide = 2,
        Darckox = 3,
        MrSlow  = 4,
        Sulana  = 5,
    }

    [Serializable]
    public enum AnimationKind
    {
        None = 0,
        Idle,
        TalkingNormal,
        TalkingHappy,
        TalkingSad,
        TalkingAngry,
        TalkingStress,
        Shocked,
        Giving,
    }

    [Serializable]
    public class DialogueLine
    {
        public Speaker speaker = Speaker.NPC;
        [TextArea(2, 6)] public string text;
        public AnimationKind animation = AnimationKind.TalkingNormal;
        public AudioClip audio;
    }
}



namespace Sulkide.Dialogue
{
    [CreateAssetMenu(fileName = "CharacterDialogueData", menuName = "Sulkide/Dialogue/Character Dialogue Data")]
    public class CharacterDialogueData : ScriptableObject
    {
        [Header("Affichage")]
        public string characterName = "Sulkide";
        public string npcDisplayName = "PNJ";

        [Serializable]
        public class DialogueOption
        {
            public string optionLabel;
            [Tooltip("Cacher l’option au démarrage ?")]
            public bool hiddenInitially = false;
            [Tooltip("Révéler cette option après avoir joué l’option d’indice X")]
            public int revealedByOptionIndex = -1;
            [Tooltip("Masquer cette option après l’avoir jouée une fois ?")]
            public bool hideAfterUse = false;

            [Header("Contenu")]
            public List<DialogueLine> lines = new();
        }

        [Header("Options")]
        public List<DialogueOption> dialogueOptions = new();
    }
}
