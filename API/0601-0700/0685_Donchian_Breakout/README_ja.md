# Donchian ブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Donchianチャネルを使ったブレイクアウトシステムで、ボラティリティとボリュームのフィルターを組み合わせています。

この戦略は、価格がDonchianチャネルの上限を上回って終値をつけ、EMAとRSIが50を超えてトレンドが確認された場合に買いを入れます。下限チャネルを下回るブレイクではショートポジションを取ります。ポジションは逆方向のDonchianシグナルが出たとき、またはATRベースのストップが発動したときに決済されます。

## 詳細

- **エントリー条件**: EMA、RSI、ボラティリティ、ボリュームフィルターを伴うDonchianチャネルブレイクアウト。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆方向のブレイクアウトまたはATRストップ。
- **ストップ**: ATRベース。
- **デフォルト値**:
  - `EntryLength` = 20
  - `ExitLength` = 10
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `EmaLength` = 50
  - `VolumeSmaLength` = 20
  - `AtrSmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Donchian, ATR, EMA, RSI, Volume
  - ストップ: ATRストップ
  - 複雑さ: 中級
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
