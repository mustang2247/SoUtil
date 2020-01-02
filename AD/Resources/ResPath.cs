﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static AD.ResourceModule;

namespace AD
{
    public static class ResPath
    {
        static ResPath()
        {
            InitResourcePath();
        }

        public static string BuildPlatformName
        {
            get { return GetBuildPlatformName(); }
        } // ex: IOS, Android, AndroidLD

        public static string BundlesDirName
        {
            get { return Configs.BundlesDirName; }
        }
        
        /// <summary>
        /// Unity Editor load or build AssetBundle directly from the Asset Bundle Path,
        /// C://Project/ResPackage/Bundles
        /// </summary>
        public static string EditorAssetBundleFullPath
        {
            get { return $"{EditorResFullPath}/{BundlesDirName}/{GetBuildPlatformName()}"; }
        }

        /// <summary>
        /// Product Folder Full Path , Default: C:\xxxxx\xxxx\../ResPackage
        /// </summary>
        public static string EditorResFullPath
        {
            get { return Path.GetFullPath(Configs.EditorResourcesDir); }
        }

        /// <summary>
        /// Bundles/Android/ etc... no prefix for streamingAssets
        /// </summary>
        public static string BundlesPathRelative { get; private set; }

        public static string StreamingPath { get; private set; }

        public static string PersistentPath
        {
            get { return Application.persistentDataPath; }
        }


        private static string _unityEditorActiveBuildTarget;

        /// <summary>
        /// UnityEditor.EditorUserBuildSettings.activeBuildTarget, Can Run in any platform~
        /// </summary>
        public static string UnityEditor_activeBuildTarget
        {
            get
            {
                if (Application.isPlaying && !string.IsNullOrEmpty(_unityEditorActiveBuildTarget))
                {
                    return _unityEditorActiveBuildTarget;
                }
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var a in assemblies)
                {
                    if (a.GetName().Name == "UnityEditor")
                    {
                        Type lockType = a.GetType("UnityEditor.EditorUserBuildSettings");
                        var p = lockType.GetProperty("activeBuildTarget");

                        var em = p.GetGetMethod().Invoke(null, new object[] { }).ToString();
                        _unityEditorActiveBuildTarget = em;
                        return em;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Different platform's assetBundles is incompatible.
        /// CosmosEngine put different platform's assetBundles in different folder.
        /// Here, get Platform name that represent the AssetBundles Folder.
        /// </summary>
        /// <returns>Platform folder Name</returns>
        private static string GetBuildPlatformName()
        {
            string buildPlatformName = "Windows"; // default

            if (Application.isEditor)
            {
                var buildTarget = UnityEditor_activeBuildTarget;
                //UnityEditor.EditorUserBuildSettings.activeBuildTarget;
                switch (buildTarget)
                {
                    case "StandaloneOSXIntel":
                    case "StandaloneOSXIntel64":
                    case "StandaloneOSXUniversal":
                        buildPlatformName = "MacOS";
                        break;
                    case "StandaloneWindows": // UnityEditor.BuildTarget.StandaloneWindows:
                    case "StandaloneWindows64": // UnityEditor.BuildTarget.StandaloneWindows64:
                        buildPlatformName = "Windows";
                        break;
                    case "Android": // UnityEditor.BuildTarget.Android:
                        buildPlatformName = "Android";
                        break;
                    case "iPhone": // UnityEditor.BuildTarget.iPhone:
                    case "iOS":
                        buildPlatformName = "iOS";
                        break;
                    default:
                        Debugger.Assert(false);
                        break;
                }
            }
            else
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.OSXPlayer:
                        buildPlatformName = "MacOS";
                        break;
                    case RuntimePlatform.Android:
                        buildPlatformName = "Android";
                        break;
                    case RuntimePlatform.IPhonePlayer:
                        buildPlatformName = "iOS";
                        break;
                    case RuntimePlatform.WindowsPlayer:
#if !UNITY_5_4_OR_NEWER
                    case RuntimePlatform.WindowsWebPlayer:
#endif
                        buildPlatformName = "Windows";
                        break;
                    default:
                        Debugger.Assert(false);
                        break;
                }
            }

            return buildPlatformName;
        }

        /// <summary>
        /// 统一在字符串后加上.ab, 取决于配置的AssetBundle后缀
        /// </summary>
        /// <param name="path"></param>
        /// <param name="formats"></param>
        /// <returns></returns>
        public static string GetAssetBundlePath(string path, params object[] formats)
        {
            return string.Format(path + Configs.AssetBundleExt, formats);
        }


        /// <summary>
        /// 完整路径，www加载
        /// </summary>
        /// <param name="url"></param>
        /// <param name="inAppPathType"></param>
        /// <param name="withFileProtocol">是否带有file://前缀</param>
        /// <param name="isLog"></param>
        /// <returns></returns>
        public static string GetResourceFullPath(string url, bool withFileProtocol = true, bool isLog = true)
        {
            string fullPath;
            if (GetResourceFullPath(url, withFileProtocol, out fullPath, isLog) !=
                ResourceModule.GetResourceFullPathType.Invalid)
                return fullPath;

            return null;
        }

        /// <summary>
        /// 根据相对路径，获取到StreamingAssets完整路径，或Resources中的路径
        /// </summary>
        public static GetResourceFullPathType GetResourceFullPath(string url, bool withFileProtocol,
            out string fullPath,
            bool isLog = true)
        {
            if (string.IsNullOrEmpty(url))
                Log.Error("尝试获取一个空的资源路径！");

            string docUrl;
            bool hasDocUrl = TryGetPersistentResourceUrl(url, withFileProtocol, out docUrl);

            string inAppUrl;
            bool hasInAppUrl = TryGetInAppStreamingUrl(url, withFileProtocol, out inAppUrl);

            if (Configs.ResourcePathPriorityType == KResourcePathPriorityType.PersistentDataPathPriority) // 優先下載資源模式
            {
                if (hasDocUrl)
                {
                    if (Application.isEditor)
                        Log.Warning("[Use PersistentDataPath] {0}", docUrl);
                    fullPath = docUrl;
                    return ResourceModule.GetResourceFullPathType.InDocument;
                }
                // 優先下載資源，但又沒有下載資源文件！使用本地資源目錄 
            }

            if (!hasInAppUrl) // 连本地资源都没有，直接失败吧 ？？ 沒有本地資源但又遠程資源？竟然！!?
            {
                if (isLog)
                    Log.Error("[Not Found] StreamingAssetsPath Url Resource: {0}", url);
                fullPath = null;
                return ResourceModule.GetResourceFullPathType.Invalid;
            }

            fullPath = inAppUrl; // 直接使用本地資源！

            return ResourceModule.GetResourceFullPathType.InApp;
        }

        // 检查资源是否存在
        public static bool ContainsResourceUrl(string resourceUrl)
        {
            string fullPath;
            return GetResourceFullPath(resourceUrl, false, out fullPath, false) !=
                   ResourceModule.GetResourceFullPathType.Invalid;
        }

        /// <summary>
        /// 可被WWW读取的Resource路径
        /// </summary>
        /// <param name="url"></param>
        /// <param name="withFileProtocol">是否带有file://前缀</param>
        /// <param name="newUrl"></param>
        /// <returns></returns>
        public static bool TryGetPersistentResourceUrl(string url, bool withFileProtocol, out string newUrl)
        {
            newUrl = Path.Combine(PersistentPath, url);

            if (File.Exists(newUrl))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// (not android ) only! Android资源不在目录！
        /// Editor返回文件系统目录，运行时返回StreamingAssets目录
        /// </summary>
        /// <param name="url"></param>
        /// <param name="withFileProtocol">是否带有file://前缀</param>
        /// <param name="newUrl"></param>
        /// <returns></returns>
        public static bool TryGetInAppStreamingUrl(string url, bool withFileProtocol, out string newUrl)
        {
            newUrl = Path.Combine(StreamingPath, url);

            // 注意，StreamingAssetsPath在Android平台時，壓縮在apk里面，不要做文件檢查了
            if (!Application.isEditor && Application.platform == RuntimePlatform.Android)
            {
                if (!ADAndroidPlugin.IsAssetExists(url))
                    return false;
            }
            else
            {
                // Editor, 非android运行，直接进行文件检查
                if (!File.Exists(newUrl))
                {
                    return false;
                }
            }

            // Windows/Edtiro平台下，进行大小敏感判断
            if (Application.isEditor)
            {
                var result = FileExistsWithDifferentCase(newUrl);
                if (!result)
                {
                    Log.Error("[大小写敏感]发现一个资源 {0}，大小写出现问题，在Windows可以读取，手机不行，请改表修改！", url);
                }
            }
            return true;
        }

        /// <summary>
        /// 大小写敏感地进行文件判断, Windows Only
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static bool FileExistsWithDifferentCase(string filePath)
        {
            if (File.Exists(filePath))
            {
                string directory = Path.GetDirectoryName(filePath);
                string fileTitle = Path.GetFileName(filePath);
                string[] files = Directory.GetFiles(directory, fileTitle);
                var realFilePath = files[0].Replace("\\", "/");
                filePath = filePath.Replace("\\", "/");

                return String.CompareOrdinal(realFilePath, filePath) == 0;
            }
            return false;
        }

        /// <summary>
        /// Initialize the path of AssetBundles store place ( Maybe in PersitentDataPath or StreamingAssetsPath )
        /// </summary>
        /// <returns></returns>
        static void InitResourcePath()
        {

            string editorProductPath = EditorResFullPath;

            BundlesPathRelative = string.Format("{0}/{1}/", BundlesDirName, GetBuildPlatformName());

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXEditor:
                {
                    StreamingPath = Application.streamingAssetsPath;
                }
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.OSXPlayer:
                {
                    StreamingPath = $"{Application.dataPath}/StreamingAssets";
                }
                    break;
                case RuntimePlatform.Android:
                {
                    StreamingPath = $"jar:file://{Application.dataPath}!/assets/";
                }
                    break;
                case RuntimePlatform.IPhonePlayer:
                {
                    StreamingPath = $"{Application.dataPath}/Raw";
                }
                    break;
                default:
                {
                    Debugger.Assert(false);
                }
                    break;
            }
        }
    }
}