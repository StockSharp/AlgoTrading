# MFI 売られすぎゾーン離脱・ナンピン戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMoney Flow Index (MFI) が売られすぎゾーンに入るのを待ちます。MFIが売られすぎレベルを上抜けすると、現在の終値から固定パーセンテージ下に指値買い注文を発注します。指定したバー数以内に注文が約定しない場合はキャンセルされます。ストップロスとテイクプロフィットはStartProtectionで適用されます。

## 詳細

- **エントリー条件**:
  - MFIが`MfiOversoldLevel`を下回った後に上抜けしたら、終値の`LongEntryPercentage`下に指値買いを発注。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - テイクプロフィットまたはストップロス（`ExitGainPercentage`、`StopLossPercentage`）によりポジション決済。
- **ストップ**: はい、StartProtection経由。
- **デフォルト値**:
  - `MfiPeriod` = 14
  - `MfiOversoldLevel` = 20
  - `LongEntryPercentage` = 0.1
  - `StopLossPercentage` = 1
  - `ExitGainPercentage` = 1
  - `CancelAfterBars` = 5
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロング
  - インジケーター: MFI
  - ストップ: はい
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
