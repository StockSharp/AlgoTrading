# Payday アノマリー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、選択した給料日（毎月1日、2日、16日、31日）にロングポジションを建て、翌日にポジションを決済します。

## 詳細

- **エントリー条件**:
  - **ロング**: 月の選択した日にロングポジションを建てる。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - 選択した日でない場合にロングポジションを決済する。
- **ストップ**: なし。
- **デフォルト値**:
  - `Trade1st` = true.
  - `Trade2nd` = true.
  - `Trade16th` = true.
  - `Trade31st` = true.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **フィルター**:
  - カテゴリ: 季節性
  - 方向: ロング
  - インジケーター: なし
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: はい
  - ニューラルネットワーク: なし
  - ダイバージェンス: なし
  - リスクレベル: 低
