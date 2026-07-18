# MACD + Bollinger Bands + RSI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この複合的な設定は、ボリンジャーバンドを超えて広がる優勢なMACDモメンタムに逆らう押し目を探します。MACDがプラスであるにもかかわらず、価格が下限バンドを下回って引けてRSIが売られ過ぎの場合、戦略はトレンド継続を見込んで買います。ショートはその逆が適用されます。

## 詳細

- **エントリー条件**:
  - **ロング**: `MACD > 0` かつ `Close < LowerBand` かつ `RSI < 30`
  - **ショート**: `MACD < 0` かつ `Close > UpperBand` かつ `RSI > 70`
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `RSILength` = 14
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: MACD, Bollinger Bands, RSI
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい
  - リスクレベル: 中
