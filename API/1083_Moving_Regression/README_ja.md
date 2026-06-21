# 移動回帰
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

多項式移動回帰を適用して次の価格を予測する戦略。予測が現在の値より上のときロングポジションを開き、下のときショートポジションを開きます。

## 詳細

- **エントリー条件**: 予測の方向。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `Degree` = 2
  - `Window` = 18
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Polynomial Regression
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
