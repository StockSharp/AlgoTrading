# MFI減速
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は上位時間軸でマネーフローインデックス（MFI）を監視し、極端なゾーンに達したときに反応します。`SeekSlowdown`が有効な場合、2つの連続したバー間でMFIの値が1ポイント未満しか変化しない場合にのみシグナルが確認されます。上方シグナルではショートポジションを決済し、オプションで新しいロングポジションを建てます。下方シグナルではロングポジションを決済し、ショートポジションを建てることができます。リスク管理はStartProtectionによって処理されます。

## 詳細

- **エントリー条件**:
  - 上方シグナル: `MFI >= UpperThreshold` かつ（減速チェックなしまたは減速検出）。
  - 下方シグナル: `MFI <= LowerThreshold` かつ（減速チェックなしまたは減速検出）。
- **ロング/ショート**: パラメーターに応じて両方。
- **エグジット条件**:
  - 反対のシグナルがポジションを決済する。
  - `StopLossPercent`と`TakeProfitPercent`によるストップロスとテイクプロフィット。
- **ストップ**: はい、StartProtection経由。
- **デフォルト値**:
  - `MfiPeriod` = 2
  - `UpperThreshold` = 90
  - `LowerThreshold` = 10
  - `SeekSlowdown` = true
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 1
  - `CandleType` = 6時間の時間軸
  - `BuyPosOpen` = `BuyPosClose` = `SellPosOpen` = `SellPosClose` = true
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: 両方
  - インジケーター: MFI
  - ストップ: はい
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: オプション（減速チェック）
  - リスクレベル: 中
