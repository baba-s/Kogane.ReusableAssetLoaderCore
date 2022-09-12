using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kogane
{
    /// <summary>
    /// アセットの非同期読み込みの自動キャンセルに対応したクラス
    /// このクラスでアセットを読み込む場合、
    /// アセットの読み込み中に新しいアセットの読み込みがリクエストされた時に
    /// 既存のアセットの読み込みを自動でキャンセルしてから新しいアセットの読み込みを開始します
    /// </summary>
    public sealed class ReusableAssetLoaderCore<T> : IDisposable where T : Object
    {
        //================================================================================
        // 変数(readonly)
        //================================================================================
        private readonly Func<string, CancellationToken, IAssetLoaderHandle<T>> m_onLoad;

        //================================================================================
        // 変数
        //================================================================================
        private IAssetLoaderHandle<T>   m_handle;
        private CancellationTokenSource m_cancellationTokenSource;

        //================================================================================
        // 関数
        //================================================================================
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ReusableAssetLoaderCore( Func<string, CancellationToken, IAssetLoaderHandle<T>> onLoad )
        {
            m_onLoad = onLoad;
        }

        /// <summary>
        /// 指定されたパスに存在するアセットを非同期で読み込みます
        /// 指定されたコンポーネントを持つゲームオブジェクトが破棄されたら
        /// 自動で非同期読み込みを中断して、
        /// すでに読み込み済みのアセットが存在する場合はアンロードします
        /// </summary>
        public async UniTask<T> LoadAsync( Component component, string path )
        {
            return await LoadAsync( component.gameObject, path );
        }

        /// <summary>
        /// 指定されたパスに存在するアセットを非同期で読み込みます
        /// 指定されたゲームオブジェクトが破棄されたら
        /// 自動で非同期読み込みを中断して、
        /// すでに読み込み済みのアセットが存在する場合はアンロードします
        /// </summary>
        public async UniTask<T> LoadAsync( GameObject gameObject, string path )
        {
            return await LoadAsync( path, gameObject.GetCancellationTokenOnDestroy() );
        }

        /// <summary>
        /// 指定されたパスに存在するアセットを非同期で読み込みます
        /// 指定された CancellationToken のキャンセル処理が実行されたら
        /// 自動で非同期読み込みを中断して、
        /// すでに読み込み済みのアセットが存在する場合はアンロードします
        /// </summary>
        public async UniTask<T> LoadAsync( string path, CancellationToken cancellationToken )
        {
            // すでに読み込み済みのアセットが存在する場合はアンロードする処理を
            // 指定された CancellationToken のキャンセル処理に紐付けます
            cancellationToken.Register( () => Dispose() );

            // すでに LoadAsync 関数が呼び出されて
            // アセットの非同期読み込み処理が開始している場合はキャンセルします
            m_cancellationTokenSource?.Cancel();
            m_cancellationTokenSource?.Dispose();
            m_cancellationTokenSource = new CancellationTokenSource();
            m_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource
            (
                m_cancellationTokenSource.Token,
                cancellationToken
            );

            var oldHandle = m_handle;

            // アセットの非同期読み込みを開始します
            m_handle = m_onLoad( path, m_cancellationTokenSource.Token );
            await m_handle.Task;

            // すでに読み込み済みのアセットが存在する場合はアンロードします
            // 新しいアセットの読み込み前に読み込み済みのアセットをアンロードすると
            // 読み込んだアセットが正常に表示されないことがあるためここでアンロードしています
            oldHandle?.Dispose();

            // アセットの非同期読み込みが完了したので
            // 不要になった CancellationToken を破棄します
            m_cancellationTokenSource.Dispose();
            m_cancellationTokenSource = null;

            // 読み込んだアセットを返します
            return m_handle.Result;
        }

        /// <summary>
        /// 読み込んだアセットをアンロードします
        /// </summary>
        public void Dispose()
        {
            m_handle?.Dispose();
            m_handle = null;

            m_cancellationTokenSource?.Cancel();
            m_cancellationTokenSource?.Dispose();
            m_cancellationTokenSource = null;
        }
    }
}