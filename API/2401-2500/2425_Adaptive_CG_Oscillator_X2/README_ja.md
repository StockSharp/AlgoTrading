# 適応型CGオシレーター X2戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

適応型CGオシレーターを2つの異なる時間軸で使用します。
上位時間軸が優勢なトレンドを定義し、下位時間軸が
オシレーターのクロスオーバーに基づいて実際のエントリーとエグジットを管理します。

## 詳細

- **エントリー条件**:
  - ロング: グローバルトレンドが上昇中にオシレーターがシグナルラインを下方クロス
  - ショート: グローバルトレンドが下降中にオシレーターがシグナルラインを上方クロス
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナルまたは明示的なクローズフラグ
- **ストップ**: なし
- **デフォルト値**:
  - `TrendAlpha` = 0.07m
  - `SignalAlpha` = 0.07m
  - `TrendCandleType` = TimeSpan.FromHours(6).TimeFrame()
  - `SignalCandleType` = TimeSpan.FromMinutes(30).TimeFrame()
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: Adaptive CG Oscillator
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: マルチ時間軸
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
