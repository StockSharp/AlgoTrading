# KNN を用いた最適化グリッド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、T3 速線が T3 遅線を上抜けし、KNN ベースの平均価格変化がプラスのときにロングポジションを建てます。エントリーおよびエグジットの閾値は平均変化に基づいて調整されます。T3 速線が遅線を下抜けし、価格が利益閾値を超えた時点でポジションをクローズします。

- **エントリー条件**: `t3Fast > t3Slow` かつ `averageChange > 0`
- **エグジット条件**: `t3Fast < t3Slow` かつ `(close - lastEntryPrice)/lastEntryPrice > adjustedCloseTh`
- **インジケーター**: T3
