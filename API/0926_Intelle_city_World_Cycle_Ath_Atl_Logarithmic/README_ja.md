# Intelle city World Cycle ATH ATL対数戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はスケーリングされた移動平均を使用してPi Cycleの概念に基づいた史上最高値（ATH）と史上最安値（ATL）のシグナルをマークします。

スケーリングされたATH長期MAが短期MAを下抜けると売り、スケーリングされたATL長期MAが短期MAを上抜けると買いとなります。

## 詳細

- **エントリー条件**: スケーリングされたATH長期SMAがATH短期SMAを下抜けで売り。スケーリングされたATL長期SMAがATL短期SMAを上抜けで買い。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆方向シグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `AthLongLength` = 350
  - `AthShortLength` = 111
  - `AtlLongLength` = 471
  - `AtlShortLength` = 150
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA, EMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
