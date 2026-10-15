# 買い売り出来高戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、買い出来高と売り出来高の分布を使用して圧力を検出します。
買い出来高が優勢で、出来高指標がボラティリティバンドを上抜け、
価格が週次VWAPを上回っているときにロングポジションを開きます。ショートポジションは
逆の条件を使用します。

## 詳細

- **エントリー条件**:
  - **ロング**: 調整済み買い出来高 > 調整済み売り出来高、出来高指標が上限バンドを超過、close が週次VWAP を上回る。
  - **ショート**: 調整済み売り出来高 > 調整済み買い出来高、出来高指標が上限バンドを超過、close が週次VWAP を下回る。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルまたはATRベースのテイクプロフィット/ストップロス。
- **ストップ**: `ProfitTargetLong`、`StopLossLong`、`ProfitTargetShort`、`StopLossShort` によるATRパーセント乗数。
- **デフォルト値**:
  - Length 20, StdDev 2.
  - ProfitTargetLong 100, StopLossLong 1.
  - ProfitTargetShort 100, StopLossShort 5.
- **フィルター**:
  - カテゴリ: 出来高ベース
  - 方向: 両方
  - インジケーター: カスタム
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: 中期
