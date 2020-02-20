﻿using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using System.Text;
using System.Reflection;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using Verse.AI;


namespace NewRatkin
{
    public class Mote_CountDown : MoteThrown
    {
        public string text;

        public Color textColor = Color.white;

        public float overrideTimeBeforeStartFadeout = -1f;

        protected float TimeBeforeStartFadeout
        {
            get
            {
                return (overrideTimeBeforeStartFadeout < 0f) ? def.mote.solidTime : overrideTimeBeforeStartFadeout;
            }
        }

        protected override bool EndOfLife
        {
            get
            {
                return AgeSecs >= TimeBeforeStartFadeout + def.mote.fadeOutTime;
            }
        }

        public override void Draw()
        {
        }

        public override void DrawGUIOverlay()
        {
            float a = 1f - (AgeSecs - TimeBeforeStartFadeout) / def.mote.fadeOutTime;
            Color color = new Color(textColor.r, textColor.g, textColor.b, a);

            Vector3 position = new Vector3(exactPosition.x, 0f, exactPosition.z);
            Vector2 vector = Find.Camera.WorldToScreenPoint(position) / Prefs.UIScale;
            vector.y = UI.screenHeight - vector.y;
            Text.Font = GameFont.Medium;
            GUI.color = textColor;
            Text.Anchor = TextAnchor.UpperCenter;
            float x = Text.CalcSize(text).x;
            Widgets.Label(new Rect(vector.x - x / 2f, vector.y - 2f, x, 999f), text);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}

