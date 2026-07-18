# ベイジアン BBSMA オシレーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Bollinger BandsとSMAを基にしたベイジアンモデルを使って、次のローソク足が上または下にブレイクする確率を推定します。Bill WilliamsのAcceleratorとAlligatorインジケーターによるオプションの確認でシグナルをフィルタリングできます。上方ブレイクの確率が閾値を超えると、ロングトレードが開かれます。下方ブレイクの高い確率はショートを引き起こします。

## 詳細

- **エントリー条件**:
  - 主要または上昇確率が `LowerThreshold`（デフォルト15%）を上回り、有効な場合はBill Williamsの確認が強気のときにロング。
  - 主要または下降確率が閾値を上回り、有効な場合はBill Williamsの確認が弱気のときにショート。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `BbSmaPeriod` = 20
  - `BbStdDevMult` = 2.5
  - `AoFast` = 5
  - `AoSlow` = 34
  - `AcFast` = 5
  - `SmaPeriod` = 20
  - `BayesPeriod` = 20
  - `LowerThreshold` = 15
  - `UseBwConfirmation` = false
  - `JawLength` = 13
- **フィルター**:
  - カテゴリ: 確率的トレンドフォロー
  - 方向: 両方
  - インジケーター: Bollinger Bands, SMA, Awesome Oscillator, Accelerator Oscillator, Alligator
  - ストップ: いいえ
  - 複雑さ: 高
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
