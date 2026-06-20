# Bollinger Heikin Ashi エントリー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Heikin Ashiローソク足にBollinger Bandsを使用する戦略。下限バンドに触れた2本連続の弱気Heikin Ashiローソク足の後、その上で強気ローソク足が出たときに買いエントリー。逆の条件で売りエントリー。

エントリー後、リスクと同等の最初の目標でポジションを一部利確し、前のローソク足の高値/安値を使ってストップをトレーリングします。

## 詳細

- **エントリー条件**:
  - ロング: 下限バンドに触れた2本の弱気HAローソク足、その後バンド上で強気
  - ショート: 上限バンドに触れた2本の強気HAローソク足、その後バンド下で弱気
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: 最初の目標1R、その後前の安値でトレーリングストップ
  - ショート: 最初の目標1R、その後前の高値でトレーリングストップ
- **ストップ**: 前のローソク足の安値/高値
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: Bollinger Bands, Heikin Ashi
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
