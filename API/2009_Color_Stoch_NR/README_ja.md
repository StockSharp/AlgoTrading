# Color Stochastic NR戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、複数の選択可能なモードを持つStochasticオシレーターを使用して取引します。各モードは、%Kと%Dラインを解釈して売買シグナルを生成する方法を定義します。

モード:

- **Breakdown** – %Kがレベル50を上抜けするとロング、下抜けするとショート。
- **OscTwist** – %Kの方向変化に反応する。
- **SignalTwist** – %Dの方向変化に反応する。
- **OscDisposition** – %Kが%Dを上抜けするとロング、下抜けするとショート。
- **SignalBreakdown** – %Dがレベル50を横切るときに取引する。

反対のシグナルは既存のポジションを閉じ、反対方向に新しいポジションを開きます。リスク管理は固定パーセントのストップロスとテイクプロフィットレベルによって行われます。

## 詳細

- **エントリー条件**:
  - **ロング**: 選択したモードに依存、上記参照。
  - **ショート**: 選択したモードに依存、上記参照。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対シグナルまたはストップ保護。
- **ストップ**: はい、`StopLossPercent` と `TakeProfitPercent`。
- **デフォルト値**:
  - `KPeriod` = 5
  - `DPeriod` = 3
  - `Mode` = `OscDisposition`
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 2
  - `CandleType` = 4 hour
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: Stochastic
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 4H
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
