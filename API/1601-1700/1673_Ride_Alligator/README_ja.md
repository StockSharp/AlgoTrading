# Alligator 追跡戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Ride Alligator 戦略の実装。この手法はAlligatorインジケーターとして知られる3本の移動平均線を使用します。TeethラインがJawsの下にある状態でLipsラインがJawsラインを上抜けするとロングポジションを開きます。LipsがJawsを下抜けし、TeethラインがJawsの上にある場合はショートポジションを開きます。オープンポジションはJawsラインに追従するトレーリングストップで保護されます。

## 詳細

- **エントリー条件**:
  - ロング: `Lips > Jaws && Teeth < Jaws && previous Lips < previous Jaws`
  - ショート: `Lips < Jaws && Teeth > Jaws && previous Lips > previous Jaws`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: `price <= Jaws`
  - ショート: `price >= Jaws`
- **ストップ**: Alligator Jaws でのトレーリングストップ
- **デフォルト値**:
  - `AlligatorPeriod` = 5
  - `MaType` = MovingAverageTypeEnum.Weighted
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Alligator
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
