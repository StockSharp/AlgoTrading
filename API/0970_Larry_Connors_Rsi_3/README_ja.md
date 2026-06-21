# Larry Connors RSI 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Larry Connors の RSI ルールに基づく平均回帰戦略。

価格が 200 期間 SMA を上回り、2 期間 RSI がトリガーレベルを上回った状態から 3 日連続で売られ過ぎ領域まで下落した場合に買います。RSI が買われ過ぎレベルを上回ると、ポジションを決済します。

## 詳細

- **エントリー条件**: SMA を上回るクローズ、かつ 2 期間 RSI がトリガーを上回る水準から 3 日間下落して売られ過ぎとなること。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: RSI が買われ過ぎレベルを上回る。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `RsiPeriod` = 2
  - `SmaPeriod` = 200
  - `DropTrigger` = 60m
  - `OversoldLevel` = 10m
  - `OverboughtLevel` = 70m
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロング
  - インジケーター: RSI, SMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
