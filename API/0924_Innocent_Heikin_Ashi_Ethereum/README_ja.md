# Innocent Heikin Ashi Ethereum戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、EMA50を下回る弱気ローソク足のシーケンスに続いてEMA50を上回る強気ローソク足が現れたときにEthereumをロングします。ストップロスは直近28バーの最安値に設定され、テイクプロフィットは`RiskReward`乗数で計算されます。オプションの**Moon Mode**はEMA200を上回った際のエントリーを許可します。売りシグナルやトラップシグナルで早期クローズすることもあります。

## 詳細

- **エントリー条件**:
  - **ロング**: EMA50を下回る`ConfirmationLevel`本以上の陰線の後、EMA50を上回る陽線。
  - **アグレッシブ**: `EnableMoonMode`が真でかつ価格がEMA200を上回っている場合。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - 直近28バーの最安値にストップロス。
  - `RiskReward`乗数によるテイクプロフィット。
  - 早期エグジット用のオプションの売りシグナルまたはトラップシグナル。
- **ストップ**: はい。
- **デフォルト値**:
  - `RiskReward` = 1.
  - `ConfirmationLevel` = 1.
  - `EnableMoonMode` = true.
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングのみ
  - インジケーター: EMA
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
