# 適応型トレンドフロー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

適応型トレンドフロー戦略は、典型価格の高速・低速EMAからボラティリティベースのチャネルを構築します。価格がチャネルの境界を越えると内部トレンドが反転します。トレンドが上向きになりオプションのSMAおよびMACDフィルターが確認したとき、ロングポジションを開きます。トレンドが下向きに反転したときにポジションを閉じます。

## 詳細

- **エントリー条件**:
  - トレンドが下降から上昇に変わり、フィルターが確認する。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - トレンドが上昇から下降に変わる。
- **ストップ**: なし。
- **デフォルト値**:
  - `Length` = 2
  - `SmoothLength` = 2
  - `Sensitivity` = 2.0
  - `UseSmaFilter` = true
  - `SmaLength` = 4
  - `UseMacdFilter` = true
  - `MacdFastLength` = 2
  - `MacdSlowLength` = 7
  - `MacdSignalLength` = 2
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングのみ
  - インジケーター: EMA, SMA, MACD, Standard Deviation
  - ストップ: いいえ
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
