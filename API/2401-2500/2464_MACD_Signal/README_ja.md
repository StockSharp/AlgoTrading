# MACDシグナル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMACD線とシグナル線の差に基づいて取引します。
差がATRベースのしきい値を超えるとポジションを開き、逆方向のクロスで決済します。
ティック単位のトレーリングストップと固定テイクプロフィットを適用します。

## 詳細

- **エントリー条件**:
  - **ロング**: MACD - Signal が `ATR * Level` を上抜け。
  - **ショート**: MACD - Signal が `-ATR * Level` を下抜け。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 逆方向のしきい値クロス。
- **ストップ**:
  - ティック単位の固定テイクプロフィット。
  - オプションのトレーリングストップ。
- **インジケーター**:
  - MACD（fast、slow、signalの各期間を設定可能）。
  - ATR(200) でしきい値をスケーリング。
