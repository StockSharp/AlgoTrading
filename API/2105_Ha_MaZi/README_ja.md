# Ha MaZi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Heikin Ashiローソク足、EMAフィルター、ZigZagピボット確認を組み合わせます。EMAの上でZigZagの新たな安値に強気のHeikin Ashiローソク足が形成されたときにロング取引を開きます。EMAの下でZigZagの新たな高値に弱気のローソク足が現れたときにショートポジションを開きます。ポジションは固定ストップロスまたはテイクプロフィットによってクローズされます。

## 詳細
- **エントリー条件**: Heikin Ashiの方向とEMAフィルターによるZigZagピボット。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ストップロスまたはテイクプロフィット。
- **ストップ**: 固定ストップと目標。
- **デフォルト値**:
  - `MaPeriod` = 40
  - `ZigzagLength` = 13
  - `StopLoss` = 70
  - `TakeProfit` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Heikin Ashi, EMA, ZigZag
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
