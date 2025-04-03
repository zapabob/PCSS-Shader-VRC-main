# PCSS for VRChat

VRChatアバター用のPCSSシャドウシェーダーとプラグイン

## 必要環境

- Unity 2019.4.31f1以降
- VRChat SDK3-AVATAR 2022.06.03.04.45以降
- [lilToon](https://lilxyzw.github.io/lilToon/)
- [Modular Avatar](https://modular-avatar.nadena.dev/)

## 導入方法

1. PCSSシェーダーパッケージをUnityプロジェクトにインポートします
2. アバターのマテリアルにlilToonシェーダーを適用します
3. 同じマテリアルにPCSSLilToon.shaderを追加で適用します
4. アバターのルートオブジェクトにPCSSLightInstaller.csをアタッチします

## 設定項目

### PCSSLilToon.shader

#### モジュラーアバター設定
- `Use Modular Avatar`: モジュラーアバターの機能を使用するかどうか
- `Preserve On AutoFix`: VRChatのAutoFIXで消されないようにするかどうか

#### PCSS設定
- `Softness`: 影の柔らかさ（0-7.5）
- `Sample Radius`: サンプリング半径（0-1）
- `Blocker Sample Count`: ブロッカーサンプリング数（1-64）
- `PCF Sample Count`: PCFサンプリング数（1-64）
- `Max Static Gradient Bias`: 静的グラデーションバイアスの最大値（0-0.15）
- `Blocker Gradient Bias`: ブロッカーグラデーションバイアス（0-1）
- `PCF Gradient Bias`: PCFグラデーションバイアス（0-1）
- `Cascade Blend Distance`: カスケードブレンド距離（0-1）
- `Shadow Strength`: 影の強度（0-1）

### PCSSLightInstaller

#### モジュラーアバター設定
- `Preserve On AutoFix`: VRChatのAutoFIXで消されないようにするかどうか
- `Sync With Mirror`: ミラー内での同期を最適化するかどうか

#### PCSS設定
- `Resolution`: シャドウマップの解像度
- `Custom Shadow Resolution`: カスタム解像度を使用するかどうか
- その他のPCSS設定はシェーダーと同様

## 特徴

- モジュラーアバター対応で非破壊的な設定が可能
- VRChatのAutoFIXで消されない
- ミラー内での最適化処理
- パラメーター永続化対応
- 距離に応じた効果の自動調整

## パフォーマンス最適化

- 距離に応じて効果が自動的に調整されます（10m以上で徐々に減衰）
- ミラー内での処理が最適化されています
- カスタム解像度設定で品質とパフォーマンスのバランスを調整可能

## トラブルシューティング

1. 影が表示されない場合
   - アバターのルートにPCSSLightInstallerが配置されているか確認
   - Preserve On AutoFixが有効になっているか確認
   - シェーダーの適用順序を確認

2. パフォーマンスが低下する場合
   - Resolution値を下げる
   - Sample Count値を下げる
   - Custom Shadow Resolutionを無効にする

3. ミラー内で影が正しく表示されない場合
   - Sync With Mirrorが有効になっているか確認
   - MAMergeAnimatorが正しく設定されているか確認

## 既知の制限事項

- 一部のアバターやシーンで期待通りの結果が得られない場合があります
- VRChatのパフォーマンスランクに影響を与える可能性があります
- 複数のPCSSライトを使用する場合、パフォーマンスが大きく低下する可能性があります

## ライセンス

MITライセンスで提供されています。
