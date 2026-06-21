# Zig Zag Aroon戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

シンプルなZig Zagピボット検出とAroonインジケーターを組み合わせた戦略。Aroon UpがAroon Downを上回るクロスで最新ピボットが高値の場合に買い。Aroon DownがAroon Upを上回るクロスで最新ピボットが安値の場合にショートポジションを建てる。

## 詳細

- **エントリー条件**: Aroonのクロスと一致するZig Zagピボット。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `ZigZagDepth` = 5
  - `AroonLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Aroon, ZigZag
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
