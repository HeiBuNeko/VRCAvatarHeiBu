using System;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.runtime
{
    public enum BitSize
    {
        [InspectorName("圧縮しない (最大: 255)")] None,
        [InspectorName("1Bit (最大: 1)")] _1,
        [InspectorName("2Bit (最大: 3)")] _2,
        [InspectorName("3Bit (最大: 7)")] _3,
        [InspectorName("4Bit (最大: 15)")] _4,
        [InspectorName("5Bit (最大: 31)")] _5,
        [InspectorName("6Bit (最大: 63)")] _6,
        [InspectorName("7Bit (最大: 127)")] _7,
    }

    /// <summary>
    /// アバターのパラメータを圧縮するBehaviour
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Satania Tabako/Satabako Expression Parameters Compressor (Int)")]
    public class ParametersCompressor : SatabakoBehaviour
    {
        [Serializable]
        public class CompressSetting
        {
            public string parameterName;
            public BitSize bitSize;
        }

        public CompressSetting[] compressSettings;
        //public string[] useCompressParameterNames = new string[0];
        //public BitSize[] useCompressParameterBitSize = new BitSize[0];
    }


}