# EMA SAR Bulls Bears戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、高速・低速の指数移動平均線（EMA）、Parabolic SAR、Bulls/Bears Powerインジケーターを組み合わせます。設定されたイントラデイウィンドウ内でのみ取引し、シンプルな利益・損失保護を使用します。

EMA3がEMA34を下回り、Parabolic SARがローソク足の高値の上にあり、Bears Powerが負だが上昇しているときにショートポジションを開きます。EMA3がEMA34を上回り、SARがローソク足の安値の下にあり、Bulls Powerが正だが下降しているときにロングポジションを開きます。

## 詳細

- **エントリー条件**:
  - **ロング**: EMA3がEMA34を上回り、SARがローソク足の安値を下回り、Bulls Power > 0かつ減少中。
  - **ショート**: EMA3がEMA34を下回り、SARがローソク足の高値を上回り、Bears Power < 0かつ増加中。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルまたはストップ/テイクの発動。
- **ストップ**: あり、絶対的なテイクプロフィット（400ポイント）とストップロス（2000ポイント）。
- **フィルター**:
  - 09:00から17:00の間のみ取引。
  - 15分ローソク足で動作。
