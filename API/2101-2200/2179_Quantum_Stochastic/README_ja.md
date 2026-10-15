# Quantum Stochastic戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はStochasticオシレーターを使用して取引します。%Kが売られすぎゾーンを抜けて`LowLevel`を上抜けすると、ロングポジションを建てます。%Kが買われすぎゾーンから落ちて`HighLevel`を下抜けすると、ショートポジションを建てます。利益を確保するために、ポジションは極値閾値で閉じられます。

## 詳細

- **エントリー条件**:
  - **ロング**: %Kが`LowLevel`を上抜け。
  - **ショート**: %Kが`HighLevel`を下抜け。
- **エグジット条件**:
  - **ロング**: %Kが`HighCloseLevel`に到達。
  - **ショート**: %Kが`LowCloseLevel`に到達。
- **インジケーター**: Stochastic Oscillator。
- **時間軸**: パラメーター`CandleType`（デフォルト1分）。
- **パラメーター**:
  - `KPeriod` – %K線の期間。
  - `DPeriod` – %D線の期間。
  - `Slowing` – Stochasticの平滑化係数。
  - `HighLevel` – 買われすぎゾーンの下限境界。
  - `LowLevel` – 売られすぎゾーンの上限境界。
  - `HighCloseLevel` – ロングポジションを閉じるレベル。
  - `LowCloseLevel` – ショートポジションを閉じるレベル。
