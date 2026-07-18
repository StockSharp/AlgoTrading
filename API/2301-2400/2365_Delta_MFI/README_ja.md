# Delta MFI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

高速および低速のマネーフローインデックス（MFI）値の比較に基づく戦略です。低速MFIがシグナルレベルを上回っている間に高速MFIが低速MFIを上回ったときにロングします。低速MFIが100マイナスシグナルレベルを下回っている間に高速MFIが低速MFIを下回ったときにショートします。

## 詳細

- **エントリー条件**: 
  - `slow MFI > Level` かつ `fast MFI > slow MFI` のとき買い
  - `slow MFI < 100 - Level` かつ `fast MFI < slow MFI` のとき売り
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のシグナル
- **ストップ**: なし
- **デフォルト値**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 50
  - `Level` = 50
  - `CandleType` = 4時間足ローソク足
- **フィルター**:
  - カテゴリ: インジケーター
  - 方向: 両方
  - インジケーター: Money Flow Index
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: H4
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
