# 平均化Stoch & WPR戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はStochasticオシレーターとWilliams %Rを組み合わせて、極端な市場状況を検知します。
Stochasticの値が0.1を下回り、Williams %Rが-90を下回ると、深い売られすぎの圧力を示すロングポジションを建てます。
Stochasticが99.9を上回り、Williams %Rが-5を超えると、強い買われすぎの状況を示すショートポジションを建てます。

この戦略は選択したローソク足タイプがサポートするあらゆる銘柄と時間軸で機能します。ロングとショートの両方のポジションを取引でき、リスク管理のためにオプションの割合ベースのストップロスを提供します。

## 詳細

- **エントリー条件**:
  - **ロング**: Stochastic < 0.1 かつ Williams %R < -90。
  - **ショート**: Stochastic > 99.9 かつ Williams %R > -5。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルまたはストップロス発動。
- **ストップ**: オプションの割合ベースのストップロス。
- **インジケーター**:
  - Stochasticオシレーター（デフォルト期間 26）。
  - Williams %R（デフォルト期間 26）。

## パラメーター

- `StochPeriod` – Stochasticの計算期間。
- `WprPeriod` – Williams %Rの計算期間。
- `StopLossPercent` – 割合ベースのストップロスのサイズ。
- `CandleType` – インジケーター計算に使用するローソク足タイプ。
