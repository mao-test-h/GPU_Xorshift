using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace PRNG
{
    public class GPU_Xorshift : MonoBehaviour
    {
        // ------------------------------
        #region // Defines

        /// <summary>
        /// テスト用テクスチャサイズ
        /// </summary>
        int TextTextureSize = 256;

        #endregion  // Defines

        // ------------------------------
        #region // Private Members(Editable)

        /// <summary>
        /// ComputeShaderの参照
        /// </summary>
        [SerializeField] ComputeShader _computeShader;

        /// <summary>
        /// テスト用テクスチャの表示用Panel
        /// </summary>
        [SerializeField] GameObject _showPlane;

        #endregion  // Private Members(Editable)

        // ------------------------------
        #region // Private Members

        uint _seed;

        // CSMain関連
        int _kernelIndex_CSMain;
        ComputeBuffer _resultBuffer = null;
        uint _kernelThreadSize_CSMain_X = 0;
        float[] _randomResult = null;
        int _randomResultIndex = 0;

        // CSTest関連
        int _kernelIndex_CSTest;
        RenderTexture _testTexture;

        #endregion  // Private Members


        // ---------------------------------------------------------------------
        #region // Unity Events

        void Start()
        {
            this.Initialize((uint)DateTime.Now.Ticks);
            this.GenerateTestTexture();
        }

        void OnDestroy()
        {
            this.Release();
        }

        #endregion // Unity Events

        // ---------------------------------------------------------------------
        #region // Public Functions

        /// <summary>
        /// 乱数生成
        /// </summary>
        /// <param name="seed">シード値</param>
        public void Generate(uint seed)
        {
            this._seed = seed;
            this._computeShader.SetInt("_seed", (int)this._seed);
            this._computeShader.SetBuffer(this._kernelIndex_CSMain, "_resultBuffer", this._resultBuffer);
            this._computeShader.Dispatch(this._kernelIndex_CSMain, 1, 1, 1);
            this._resultBuffer.GetData(this._randomResult);
            this._randomResultIndex = 0;
        }

        /// <summary>
        /// 乱数の取得
        /// </summary>
        /// <returns>乱数(0f~1f)</returns>
        public float GetValue()
        {
            var val = this._randomResult[this._randomResultIndex];
            this._randomResultIndex++;
            if (this._randomResultIndex == this._randomResult.Length)
            {
                // 取りあえずは全部出し切ったら_seedを加算して再生成しておく
                // HACK. 後はやりたい実装に合わせること
                this.Generate(++this._seed);
            }
            return val;
        }

        /// <summary>
        /// テスト用テクスチャのデータ生成
        /// </summary>
        public void GenerateTestTexture()
        {
            this._computeShader.Dispatch(this._computeShader.FindKernel("CSTest"), 1, TextTextureSize, 1);
            this._showPlane.GetComponent<Renderer>().material.mainTexture = this._testTexture;
        }

        #endregion // Public Functions

        // ---------------------------------------------------------------------
        #region // Private Functions

        /// <summary>
        /// バッファの初期化
        /// </summary>
        void Initialize(uint seed)
        {
            uint y, z;  // dummy

            // CSMain
            this._kernelIndex_CSMain = this._computeShader.FindKernel("CSMain");
            this._computeShader.GetKernelThreadGroupSizes(this._kernelIndex_CSMain, out this._kernelThreadSize_CSMain_X, out y, out z);
            this._resultBuffer = new ComputeBuffer((int)this._kernelThreadSize_CSMain_X, Marshal.SizeOf(typeof(float)));
            this._randomResult = new float[this._kernelThreadSize_CSMain_X];
            this.Generate(seed);

            // CSTest
            this._testTexture = new RenderTexture(TextTextureSize, TextTextureSize, 0, RenderTextureFormat.ARGB32);
            this._testTexture.enableRandomWrite = true;
            this._testTexture.Create();
            this._kernelIndex_CSTest = this._computeShader.FindKernel("CSTest");
            this._computeShader.SetTexture(this._kernelIndex_CSTest, "_testTextureBuffer", this._testTexture);
        }

        /// <summary>
        /// バッファの解放
        /// </summary>
        void Release()
        {
            if (this._resultBuffer != null)
            {
                this._resultBuffer.Release();
                this._resultBuffer = null;
            }
            if (this._testTexture != null)
            {
                this._testTexture.Release();
                this._testTexture = null;
            }
        }

        #endregion // Private Functions
    }
}