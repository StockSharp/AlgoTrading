# ナイト Stochastic戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ナイトStochastic戦略は、静かな夜間セッションの**21:00**から**06:00**の間のみ取引します。Stochastic Oscillatorの%Kラインを使用して売られすぎと買われすぎの状態を検出します。

オシレーターが売られすぎレベルを下回るとロングポジションを建てます。買われすぎレベルを上回るとショートポジションを建てます。各取引は価格ポイントで計測された固定ストップロスとテイクプロフィットレベルで保護されます。

## 詳細

- **エントリー条件**:
  - **ロング**: `%K < StochOversold` かつ時間が21:00〜06:00の間。
  - **ショート**: `%K > StochOverbought` かつ時間が21:00〜06:00の間。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 事前定義されたストップロスまたはテイクプロフィットでポジションを閉じる。
- **ストップ**: あり、固定ストップロスとテイクプロフィットを使用。
- **デフォルト値**:
  - `StopLossPoints` = 40
  - `TakeProfitPoints` = 20
  - `StochOversold` = 30
  - `StochOverbought` = 70
  - `CandleType` = 15分時間軸
- **フィルター**:
  - カテゴリ: インジケーターベース
  - 方向: 両方
  - インジケーター: Stochastic Oscillator
  - 時間軸: 短期
  - 取引ウィンドウ: サーバー時間 21:00-06:00
