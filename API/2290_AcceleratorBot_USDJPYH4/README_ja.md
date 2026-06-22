# AcceleratorBot USDJPY H4 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

AcceleratorBot戦略は、H4時間軸のUSDJPY向けに設計されたオリジナルMQL4エキスパートを変換したものです。平均方向性指数（ADX）によるトレンド強度、ストキャスティクス・オシレーターによるモメンタム、およびマルチ時間軸の加速/減速（AC）値を組み合わせています。ローソク足パターンが方向フィルターとして使用されます。

## 詳細

- **エントリー条件**: ローソク足フィルターで確認されたトレンドまたはモメンタムシグナル。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナル、ストップロス、テイクプロフィット、またはトレーリングストップ。
- **ストップ**: 固定とトレーリング。
- **デフォルト値**:
  - `StopLossPoints` = 750
  - `TakeProfitPoints` = 9999
  - `TrailPoints` = 0
  - `AdxPeriod` = 14
  - `AdxThreshold` = 20m
  - `X1` = 0
  - `X2` = 150
  - `X3` = 500
  - `CandleType` = TimeSpan.FromHours(4)
- **フィルター**:
  - カテゴリ: トレンドとモメンタム
  - 方向: 両方
  - インジケーター: ADX, Stochastic, AC
  - ストップ: はい
  - 複雑さ: 上級
  - 時間軸: H4
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
