# Market Slayer戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、上位時間軸のSSLトレンド確認と組み合わせた加重移動平均クロスオーバーを使用します。短期WMAが長期WMAを上抜けし、トレンドが強気の場合にロングポジションを建て、逆の条件でショートポジションを建てます。オプションで絶対値のテイクプロフィットとストップロスを有効にできます。

## 詳細

- **エントリー条件**:
  - **ロング**: 短期WMAが長期WMAを上抜けし、上位時間軸のSSLが強気。
  - **ショート**: 短期WMAが長期WMAを下抜けし、上位時間軸のSSLが弱気。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - トレンドフィルターが逆転する。
  - 有効時のオプションのストップロスまたはテイクプロフィット。
- **ストップ**: オプション。
- **デフォルト値**:
  - `ShortLength` = 10.
  - `LongLength` = 20.
  - `ConfirmationTrendValue` = 2.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
  - `TrendCandleType` = TimeSpan.FromMinutes(240).TimeFrame().
  - `TakeProfitEnabled` = false.
  - `TakeProfitValue` = 20.
  - `StopLossEnabled` = false.
  - `StopLossValue` = 50.
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: WMA, SSL
  - ストップ: オプション
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
