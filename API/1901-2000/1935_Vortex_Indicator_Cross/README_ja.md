# Vortexインジケーター・クロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、VortexインジケーターのVI+ラインとVI-ラインのクロスを取引します。
VI+がVI-を上抜けした場合にロング、VI-がVI+を上抜けした場合にショートになります。
価格ステップ単位のストップロスとテイクプロフィットが自動的に管理されます。

## パラメーター

- **Vortex Length** – Vortexインジケーターの期間。
- **Candle Type** – インジケーター計算に使用する時間軸。
- **Stop Loss** – 価格ステップ単位の保護ストップ。
- **Take Profit** – 価格ステップ単位の目標利益。

## 詳細

- **インジケーター**: Vortex
- **方向**: ロングおよびショート
- **時間軸**: 設定可能
- **リスク管理**: `StartProtection` によるストップロスとテイクプロフィット。
