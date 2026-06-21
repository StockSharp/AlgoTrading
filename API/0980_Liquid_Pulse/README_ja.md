# リキッド・パルス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MACDとADXで確認された高出来高スパイクを検出します。ATRがストップとテイクプロフィットを定義し、1日の取引回数を制限します。

## 詳細

- **エントリー条件**:
  - ロング: 出来高スパイク、MACDがシグナルを上抜け、+DI > -DI、ADX >= しきい値
  - ショート: 出来高スパイク、MACDがシグナルを下抜け、-DI > +DI、ADX >= しきい値
- **ロング/ショート**: 両方
- **エグジット条件**: ATRベースのストップまたはテイクプロフィット
- **ストップ**: ATRの倍数
- **デフォルト値**:
  - `VolumeSensitivity` = Medium
  - `MacdSpeed` = Medium
  - `DailyTradeLimit` = 20
  - `AtrPeriod` = 9
  - `AdxTrendThreshold` = 41
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: MACD, ADX, ATR, 出来高
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
