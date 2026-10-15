# ゴールデン・トランスフォーム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Rate of Changeインジケーターと三重Hullベースのトリプルトリックス（TRIX）、Hull MAフィルター、平滑化されたフィッシャー・トランスフォームを組み合わせます。TRIXがゼロ未満でROCがTRIXを上抜け、始値がHull MAを上回ったときにロングエントリーします。逆の条件でショートエントリーします。逆のクロスが生じたとき、または平滑化フィッシャーが閾値を超えて反転したときにポジションを決済します。

## 詳細

- **エントリー条件**:
  - **ロング**: `ROC crosses above TRIX` && `TRIX < 0` && `Open > Hull MA`
  - **ショート**: `ROC crosses below TRIX` && `TRIX > 0` && `Open < Hull MA`
- **ロング/ショート**: ロングとショート
- **エグジット条件**:
  - ロング: `ROC crosses below TRIX` または (`Fisher HMA > 1.5` && `Fisher HMA crosses below previous Fisher`)
  - ショート: `ROC crosses above TRIX` または (`Fisher HMA < -1.5` && `Fisher HMA crosses above previous Fisher`)
- **ストップ**: いいえ
- **デフォルト値**:
  - `ROC Length` = 50
  - `Hull TRIX Length` = 90
  - `Hull Entry Length` = 65
  - `Fisher Length` = 50
  - `Fisher Smooth Length` = 5
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: ROC, Hull MA, Fisher Transform
  - ストップ: いいえ
  - 複雑さ: 中
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
