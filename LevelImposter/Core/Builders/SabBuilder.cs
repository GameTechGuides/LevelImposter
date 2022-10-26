﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Events;
using UnityEngine;
using LevelImposter.DB;

namespace LevelImposter.Core
{
    public class SabBuilder : IElemBuilder
    {
        private static Dictionary<SystemTypes, SabotageTask> _sabDB = null;
        private GameObject _sabContainer = null;

        public SabBuilder()
        {
            _sabDB = new Dictionary<SystemTypes, SabotageTask>();
        }

        public void Build(LIElement elem, GameObject obj)
        {
            if (!elem.type.StartsWith("sab-") || elem.type.StartsWith("sab-btn"))
                return;

            if (_sabContainer == null)
            {
                _sabContainer = new GameObject("Sabotages");
                _sabContainer.transform.SetParent(LIShipStatus.Instance.transform);
                _sabContainer.SetActive(false);
            }

            SabData sabData = AssetDB.Sabs[elem.type];
            ShipStatus shipStatus = LIShipStatus.Instance.ShipStatus;
            SabotageTask sabClone = sabData.Behavior.Cast<SabotageTask>();

            // System
            SystemTypes systemType = 0;
            if (elem.properties.parent != null)
                systemType = RoomBuilder.GetSystem((Guid)elem.properties.parent);

            // Task
            if (!_sabDB.ContainsKey(systemType))
            {
                LILogger.Info("Adding sabotage for " + elem.name + "...");
                GameObject sabContainer = new GameObject(elem.name);
                sabContainer.transform.SetParent(_sabContainer.transform);

                SabotageTask task = sabContainer.AddComponent(sabData.Behavior.GetIl2CppType()).Cast<SabotageTask>();
                task.StartAt = sabClone.StartAt;
                task.TaskType = sabClone.TaskType;
                task.MinigamePrefab = sabClone.MinigamePrefab;
                task.Arrows = new(0);

                shipStatus.SpecialTasks = MapUtils.AddToArr(shipStatus.SpecialTasks, task);
                _sabDB.Add(systemType, task);
            }
        }

        public void PostBuild() { }

        public static SabotageTask FindSabotage(SystemTypes systemType)
        {
            SabotageTask sabotage;
            _sabDB.TryGetValue(systemType, out sabotage);
            return sabotage;
        }
    }
}
