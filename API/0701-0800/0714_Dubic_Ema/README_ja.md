# Dubic EMA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、高値と安値で計算された指数移動平均に対するクローズの位置に基づいて取引します。狭いレンジや低ボラティリティ期間中の取引は避けます。ポジションは ATR ベースのストップ、利益確定レベル、およびオプションの Parabolic SAR トレーリングストップで保護されます。

## 詳細

- **エントリー条件**:
  - **ロング**: Close > EMA(High) かつ Close > EMA(Low)、レンジフィルター非アクティブ、ボラティリティ十分。
  - **ショート**: Close < EMA(High) かつ Close < EMA(Low)、レンジフィルター非アクティブ、ボラティリティ十分。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - Parabolic SAR、ATR/固定ストップロスまたは利益確定。
- **ストップ**: はい。
- **フィルター**: レンジおよびボラティリティフィルター。
