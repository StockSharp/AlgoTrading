# MH Hull Moving Average ベーストレーディング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Hull Moving Averageに基づくブレイクアウト戦略。

戦略はHull Moving Averageから導出された動的レベルと始値を比較します。価格が上部レベルを上抜けしたときにロング、下部レベルを下抜けしたときにショートにエントリーします。逆方向のブレイクアウトで既存ポジションを決済します。

## 詳細

- **エントリー条件**: Hull Moving Averageレベルに対する価格の関係。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆方向のブレイクアウト。
- **ストップ**: なし。
- **デフォルト値**:
  - `HullPeriod` = 210
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
