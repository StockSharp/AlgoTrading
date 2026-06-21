# Efficient Work戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、短期・中期・長期の3つのホライズンで移動平均を使用します。高速平均が両方の長期平均を上回るとロングポジションを取り、下回るとショートポジションを取ります。

## 詳細

- **エントリー条件**:
  - **ロング**: `fast MA > medium MA` かつ `fast MA > high MA`。
  - **ショート**: `fast MA < medium MA` かつ `fast MA < high MA`。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 逆シグナルで反転。
- **ストップ**: なし。
- **デフォルト値**:
  - `MA Period` = 20
  - `Medium TF Multiplier` = 5
  - `High TF Multiplier` = 10
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
