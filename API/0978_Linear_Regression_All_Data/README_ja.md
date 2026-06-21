# 全データ使用線形回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は利用可能なすべてのバーを使用して線形回帰ラインを計算し、チャートに描画します。
また、傾き、切片、相関係数もログに記録します。

## 詳細

- **エントリー条件**: なし。
- **ロング/ショート**: なし。
- **エグジット条件**: なし。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `MaxBarsBack` = 5000.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **フィルター**:
  - カテゴリ: ユーティリティ
  - 方向: なし
  - インジケーター: Linear Regression
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
