# 株価純資産倍率（Book-to-Market）戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Book-to-Market Value** 戦略は、book-to-marketファクターのためのユニバースパラメーター設定と日足ローソク足のサブスクリプションを実演します。
このサンプルはプレースホルダーであり、現在は取引ロジックを含んでいません。

## 詳細
- **エントリー条件**: ファクターロジックは未実装。
- **ロング/ショート**: 両方向。
- **エグジット条件**: なし。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: ファンダメンタル
  - 方向: 両方
  - インジケーター: Fundamentals
  - ストップ: いいえ
  - 複雑さ: 初心者
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
