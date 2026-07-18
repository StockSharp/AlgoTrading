# 方向性移動ローソク足戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はローソク足の終値でのRelative Strength Index (RSI)を監視します。RSIがニュートラルゾーンを抜けてユーザー定義のレベルをクロスすると、戦略はモメンタムの方向にポジションを開き、逆方向のエクスポージャーをクローズします。

## 詳細

- **インジケーター**: 調整可能な`RsiPeriod`を持つRelative Strength Index。
- **HighLevel**: 強気のモメンタムを示すRSI値。
- **MiddleLevel**: 参照用に保持するニュートラル閾値。
- **LowLevel**: 弱気のモメンタムを示すRSI値。
- **エントリー**:
  - RSIが`HighLevel`を下から上抜けするとロング。
  - RSIが`LowLevel`を上から下抜けするとショート。
- **エグジット**: 逆シグナルが新しいポジションを開く前に既存のポジションをクローズ。
- **ロング/ショート**: 両方向。
- **ストップ**: デフォルトでは使用しない。
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `HighLevel` = 70
  - `MiddleLevel` = 50
  - `LowLevel` = 30
  - `CandleType` = 5分足時間軸
