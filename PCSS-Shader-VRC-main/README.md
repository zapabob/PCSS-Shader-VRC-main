README.md:

ゲーミングカラーオーバーレイシェーダー
ゲーミングカラーオーバーレイシェーダーは、VRChatアバター用のマテリアルにゲーミングカラーエフェクトを追加するためのUnityシェーダーです。このシェーダーを使用することで、既存のマテリアルの色を変更したり、特定の色を強調したりすることができます。

特徴
既存のマテリアルにゲーミングカラーエフェクトを追加できます。
オーバーレイテクスチャを使用して、ゲーミングカラーエフェクトを適用する領域を指定できます。
ゲーミングカラーエフェクトの色や強度を調整できます。
ゲーミングカラーエフェクトのオン/オフを切り替えられます。
動作環境
Unity 2019.4以降
VRChatアバター用のマテリアル
導入方法
ゲーミングカラーオーバーレイシェーダーのUnityパッケージをインポートします。
対象のマテリアルにGamingColorOverlayシェーダーを適用します。
マテリアルのプロパティを設定します。
詳細な導入方法やプロパティの説明については、同梱のユーザーマニュアル.mdを参照してください。

ライセンス
このシェーダーは、MITライセンスの下で公開されています。詳細については、LICENSEファイルを参照してください。

## PCSS Shader for VRChat

VRChatアバター用のPCSSシャドーシェーダーです。モジュラーアバターに対応し、AutoFIXでの削除を防ぎます。

## 必要環境 / Requirements

- Unity 2019.4.31f1 以上
- [VRChat SDK3 Avatars](https://vrchat.com/home/download)
- [lilToon](https://lilxyzw.github.io/lilToon/#/)
- [Modular Avatar](https://modular-avatar.nadena.dev/)

## 導入方法 / Installation

### 1. パッケージのインポート / Import Package
1. このリポジトリをダウンロードし、UnityプロジェクトのAssetsフォルダに展開します
2. VRChat SDK、lilToon、Modular Avatarをインポートしていない場合は、先にインポートしてください

### 2. アバターへの設定 / Avatar Setup
1. アバターのルートオブジェクトに`PCSSLightInstaller`コンポーネントを追加
2. インスペクターで以下の設定を確認：
   - `Preserve On AutoFix`: 有効推奨（AutoFIXでの削除を防ぎます）
   - `Sync With Mirror`: 有効推奨（ミラーでの表示を最適化）

### 3. マテリアルの設定 / Material Setup
1. シェーダーを`lilToon/PCSS`に変更
2. PCSSパラメーターを調整：
   - `Shadow Map Resolution`: シャドウマップの解像度（推奨: 1024-2048）
   - `PCSS Sample Count`: サンプリング数（高いほど品質が向上、推奨: 16-32）
   - `Shadow Softness`: シャドウのぼかし具合（0-1）

## パフォーマンス最適化 / Performance Optimization

- シャドウマップの解像度は必要以上に上げないでください
- サンプリング数は品質とパフォーマンスのバランスを考慮して設定
- `Distance Based Settings`を活用し、遠距離での処理を最適化

## トラブルシューティング / Troubleshooting

### シャドウが表示されない場合 / Shadows Not Showing
- アバターのルートに`PCSSLightInstaller`が正しく設定されているか確認
- マテリアルのシェーダーが`lilToon/PCSS`になっているか確認
- カメラの深度テクスチャが有効になっているか確認

### パフォーマンスの問題 / Performance Issues
- シャドウマップの解像度を下げる
- サンプリング数を減らす
- 遠距離での効果を調整

### コンパイルエラーが出る場合 / Compilation Errors
- Modular Avatarのエラー（MAComponent等が見つからない）
  1. VCCでModular Avatarが正しくインストールされているか確認
  2. プロジェクトを再起動
  3. アセンブリ参照に`nadena.dev.modular-avatar.core`と`nadena.dev.ndmf`が含まれているか確認

## 制限事項 / Limitations

- VRChatのパフォーマンスランクに影響を与える可能性があります
- 複数のPCSSライトの使用は推奨されません
- Quest版VRChatでは使用できません

## ライセンス / License

MIT License

## 詳細なプロジェクト構造 / Detailed Project Structure

```
Assets/
└── PCSS-Shader/
    ├── Scripts/
    │   ├── Core/
    │   │   ├── PCSSLightPlugin.cs        # PCSS中核機能
    │   │   ├── PCSSLightInstaller.cs     # インストーラー
    │   │   └── PCSS.Core.asmdef         # コア機能のアセンブリ定義
    │   └── Utils/
    │       ├── PoissonTools.cs          # ポアソンディスク計算
    │       ├── SetCameraDepth.cs        # カメラ深度設定
    │       └── PCSS.Utils.asmdef        # ユーティリティのアセンブリ定義
    ├── Shaders/
    │   ├── PCSSLiltoonShader.shader    # PCSSシェーダー
    │   └── PCSS.Shaders.asmdef         # シェーダーのアセンブリ定義
    ├── Samples/                         # サンプルシーンとデモ
    ├── Prefabs/                         # プレハブ
    ├── package.json                     # パッケージ情報
    └── README.md                        # ドキュメント
```

### アセンブリ構造 / Assembly Structure

- `PCSS.Core`: メインの機能とModular Avatar連携
- `PCSS.Utils`: ユーティリティ機能（PCSS.Coreに依存）
- `PCSS.Shaders`: シェーダー関連（PCSS.CoreとPCSS.Utilsに依存）

### パッケージ依存関係 / Package Dependencies

- VRChat SDK3 Avatars (>=3.4.0)
- Modular Avatar (>=1.8.0)
- NDMF (>=1.2.0)