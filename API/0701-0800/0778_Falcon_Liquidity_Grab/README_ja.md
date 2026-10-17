# Falcon Liquidity Grab戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、シンプル移動平均でトレンドを定義しながら、主要な市場セッション中の流動性グラブをトレードします。価格が直近のスイングレベルを越えてトレンドとともに反転するときにエントリーします。各トレードはティック単位の固定ストップロスとテイクプロフィットを使用します。

## 詳細

- **エントリー条件**:
  - **ロング**: `Low < lowest(swing period)` && `Close > SMA` && `session filter`
  - **ショート**: `High > highest(swing period)` && `Close < SMA` && `session filter`
- **エグジット条件**: 固定ストップロスとテイクプロフィット。
- **タイプ**: リバーサル
- **インジケーター**: SMA、Highest、Lowest
- **時間軸**: 15分（デフォルト）
- **ストップ**: `StopLossPoints` ティック、`TakeProfitMultiplier`× ストップ距離

**厳密な条件と初期値:** `SwingPeriod=20`、`TrendLength=50`、`StopLossPoints=50`、`TakeProfitMultiplier=2`、`UseSession=true`、`SessionStart=07:00`、`SessionEnd=21:00` (UTC)、`CandleType=15分`。確定したシグナル足の高安を、その足を除く**過去**N本の高安と比較します。Lowが過去最安値より低くCloseがSMAより高ければロング、Highが過去最高値より高くCloseがSMAより低ければショート。SMAにはシグナル足を含めます。新規エントリーはノーポジションでUTC時間窓内のみ（開始を含み終了を含まない、日跨ぎ対応）。Level1のbid/askによるネイティブ成行保護は足の間でも動き、ストップ幅は`StopLossPoints × Security.PriceStep`、利確幅は`TakeProfitMultiplier × ストップ幅`。反対シグナルでの終値決済はありません。
