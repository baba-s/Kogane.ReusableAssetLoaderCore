using System;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Kogane
{
    /// <summary>
    /// アセットの非同期読み込みを管理するハンドルのインターフェイス
    /// </summary>
    public interface IAssetLoaderHandle<T> : IDisposable where T : Object
    {
        //================================================================================
        // プロパティ
        //================================================================================
        /// <summary>
        /// 非同期読み込みのタスクを返します
        /// </summary>
        UniTask<T> Task { get; }
    }
}