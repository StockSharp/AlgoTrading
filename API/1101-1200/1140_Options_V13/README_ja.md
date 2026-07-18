# オプション戦略 V1.3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSI、ATRベースのストップとテイクプロフィット、および出来高フィルターを備えたEMAクロスオーバー戦略です。オープニングレンジのブレイクアウトをオプションで要求でき、ニューヨーク時間15:55にポジションをクローズします。事前定義されたセッションおよびユーザー指定の取引禁止インターバル中は取引がブロックされます。

## 詳細

- **エントリー条件**:
  - **ロング**: 短期EMAが長期EMAを上抜け、RSI ≥ `RsiLongThreshold`、出来高 ≥ 出来高SMA、オプションで終値 > オープニングレンジ高値。
  - **ショート**: 短期EMAが長期EMAを下抜け、RSI ≤ `RsiShortThreshold`、出来高 ≥ 出来高SMA、オプションで終値 < オープニングレンジ安値。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - ATRベースのストップロスとテイクプロフィット。
  - 逆方向のEMAクロス。
  - NY時間15:55に自動クローズ。
- **ストップ**: はい。
- **デフォルト値**:
  - `EmaShortLength = 8`
  - `EmaLongLength = 28`
  - `RsiLength = 12`
  - `AtrLength = 14`
  - `SlMultiplier = 1.4`
  - `TpSlRatio = 4`
  - `VolumeMaLength = 20`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 設定可能
  - インジケーター: EMA, RSI, ATR, SMA
  - ストップ: はい
  - 時間軸: イントラデイ
