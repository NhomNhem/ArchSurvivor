#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArchSurvivor.Editor
{
    public static class ProjectFolderSetup
    {
        [MenuItem("Tools/ArchSurvivor/Setup Project Folders")]
        public static void CreateFolders()
        {
            string root = "Assets/_ArchSurvivor";
            List<string> folders = new List<string>
            {
                // --- CORE & INSTALLERS ---
                $"{root}/_Boot",
                $"{root}/Installers",
                $"{root}/Configs", 

                // --- APP ARCHITECTURE ---
                $"{root}/App/Interfaces",
                $"{root}/App/Services/Data",
                $"{root}/App/Services/Economy",
                $"{root}/App/Services/Audio",

                // --- COMMON ---
                $"{root}/Common/Base",
                $"{root}/Common/EventBus",
                $"{root}/Common/Pooling",
                $"{root}/Common/Utilities",

                // --- ART ASSETS (Nơi chứa .anim, .fbx, .controller rỗng) ---
                $"{root}/Art/Characters/Animations", // <--- Để Animation Clip nhân vật vào đây
                $"{root}/Art/Characters/Materials",
                $"{root}/Art/Characters/Models",
                
                $"{root}/Art/Enemies/Animations",    // <--- Để Animation Clip quái vào đây
                $"{root}/Art/Enemies/Models",
                
                $"{root}/Art/Environment",
                $"{root}/Art/UI",
                $"{root}/Art/VFX",                   // Particle System

                // --- FEATURES (LOGIC & CONFIG) ---
                
                // PLAYER
                $"{root}/Features/Player/Args",
                $"{root}/Features/Player/Configs",    
                $"{root}/Features/Player/Interfaces",
                $"{root}/Features/Player/Logic/FSM",
                $"{root}/Features/Player/Visuals",    // Code PlayerView (Animancer)
                $"{root}/Features/Player/Prefabs",    

                // CHARACTER
                $"{root}/Features/Character/Configs", 
                $"{root}/Features/Character/Logic",
                $"{root}/Features/Character/Prefabs", 

                // ENEMY
                $"{root}/Features/Enemy/Args",
                $"{root}/Features/Enemy/Configs",     
                $"{root}/Features/Enemy/Interfaces",
                $"{root}/Features/Enemy/Logic",
                $"{root}/Features/Enemy/Visuals",
                $"{root}/Features/Enemy/Prefabs",     

                // COMBAT
                $"{root}/Features/Combat/Interfaces",
                $"{root}/Features/Combat/Damage",
                $"{root}/Features/Combat/Projectiles/Args",
                $"{root}/Features/Combat/Projectiles/Logic",
                $"{root}/Features/Combat/Projectiles/Prefabs", 

                // SKILLS & UI
                $"{root}/Features/Skills/Configs",    
                $"{root}/Features/Skills/Logic",
                $"{root}/Features/Skills/Prefabs",    
                $"{root}/Features/HUD/Prefabs",       
                $"{root}/Features/MetagameUI/Prefabs" 
            };

            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"<color=green><b>[SUCCESS]</b></color> Đã cập nhật thư mục ART để chứa Animation!");
        }
    }
}
#endif