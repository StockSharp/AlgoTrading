# RMI トレンドシンク
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RMI Trend Sync は RSI と MFI のモメンタムシグナルを SuperTrend トレーリングストップと組み合わせます。平均モメンタムが EMA の傾き確認とともに閾値を上回るとロングトレードが開き、下方ブレイクでショートトレードが発動します。SuperTrend が出口トレイルを提供します。

## 詳細

- **エントリー条件**: モメンタム平均が EMA 傾き確認を伴いながら閾値を越える。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆方向モメンタムまたは SuperTrend ストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `RmiLength` = 21
  - `PositiveThreshold` = 70
  - `NegativeThreshold` = 30
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3.5
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: RSI, MFI, EMA, SuperTrend
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
