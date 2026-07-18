# Chande Krollトレンド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Chande KrollストップとトレンドフィルターとしてのSMAを組み合わせた戦略です。終値が下方ストップを上抜けしSMAを上回るとロングポジションを建てます。終値が上方ストップを下抜けするとポジションを決済します。ポジションサイズは1560バーにわたる最安値終値とリスク乗数に基づいて算出します。

## 詳細

- **エントリー条件**:
  - ロング: `previous close <= previous low stop && Close > low stop && Close > SMA`
- **ロング/ショート**: ロングのみ
- **エグジット条件**:
  - ロング: `Close < high stop`
- **ストップ**: Chande Krollストップ（Donchianの極値 ± ATR）
- **デフォルト値**:
  - `CalcMode` = CalcMode.Exponential
  - `RiskMultiplier` = 5m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `StopLength` = 21
  - `SmaLength` = 21
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: ATR, Donchian, SMA, Lowest
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
