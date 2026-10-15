# Color XTRIXヒストグラム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、対数終値から計算された平滑化TRIX（三重指数移動平均モメンタム）の方向変化に基づいて取引します。TRIXヒストグラムが下降後に上向きに転じるとロングポジションが建てられ、上昇後に下向きに転じるとショートポジションが建てられます。反対の転換時にポジションが反転されます。ストップロスやテイクプロフィットは使用しません。

## 詳細

- **エントリー条件**:
  - **ロング**: `TRIX rising` && `previous TRIX falling`
  - **ショート**: `TRIX falling` && `previous TRIX rising`
- **ロング/ショート**: ロングとショート
- **エグジット条件**:
  - ロング: `TRIX turns downward`
  - ショート: `TRIX turns upward`
- **ストップ**: なし
- **デフォルト値**:
  - `TRIX Length` = 5
  - `Smooth Length` = 5
  - `Momentum Period` = 1
  - `Candle Type` = 4h時間軸
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: TRIX
  - ストップ: なし
  - 複雑さ: 低
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
