# Fractal ADX クラウド
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はStockSharpのAverage Directional Indexインジケーターを使用して、オリジナルのMQLエキスパート`Fractal_ADX_Cloud`を近似します。4時間足のローソク足で動作し、+DIと-DIコンポーネントのクロスを分析します。強気コンポーネント（+DI）が弱気コンポーネント（-DI）を上回ると、戦略はショートポジションをクローズし、新しいロングポジションを開く可能性があります。-DIが+DIを上回った場合、ロジックはショートトレードのために反転されます。

ストップロスとテイクプロフィットの保護は絶対価格単位で適用されます。追加パラメーターにより、各方向でのポジションの開閉を個別に有効・無効にできます。

## 詳細

- **エントリー条件**: ADXの+DIと-DIラインのクロス。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップ。
- **ストップ**: はい、絶対価格距離を使用。
- **デフォルト値**:
  - `AdxPeriod` = 30
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(4)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ADX
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 4h
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
