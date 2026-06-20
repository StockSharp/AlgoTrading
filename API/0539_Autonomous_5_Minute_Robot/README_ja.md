# 自律型5分ロボット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

自律型5分ロボット戦略は5分足の時間軸で取引します。
価格が上昇トレンドにあり、買い圧力が売り圧力を上回るときにロングエントリーし、
逆の条件でショートエントリーします。

## 詳細

- **エントリー条件**: 上昇トレンド（50期間SMAを上回る終値かつ6本前の終値を上回る）かつ買いボリュームが売りボリュームより多い。
- **エグジット条件**: 反対シグナルでポジションを転換。
- **ストップ**: エントリー価格から3%のストップロスおよび29%のテイクプロフィット。
- **デフォルト値**:
  - `MaLength` = 50
  - `VolumeLength` = 10
  - `StopLossPercent` = 3
  - `TakeProfitPercent` = 29
  - `CandleType` = 5m
