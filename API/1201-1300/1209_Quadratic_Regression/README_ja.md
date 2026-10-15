# 二次回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は直近の`Length`本のバーに対して二次回帰線を計算し、価格と回帰線のクロスオーバーで取引します。

## 詳細

- **エントリー条件**: 価格が二次回帰線を上抜け/下抜けする。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対方向のクロスオーバー。
- **ストップ**: なし。
- **デフォルト値**:
  - `Length` = 54.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Quadratic Regression
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
