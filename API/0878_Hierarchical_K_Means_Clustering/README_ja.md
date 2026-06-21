# 階層的K-Meansクラスタリング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はSuperTrendシステムにボラティリティクラスタリングを適用します。ATR値を3つのクラスターに分類して市場レジームを判断し、SuperTrendの方向がエントリーのトリガーとなります。オプションの移動平均とADXフィルターがトレンドの強さを確認します。強気/弱気のボリューム比率がバランスに向かったとき、ポジションを早期決済できます。

## 詳細

- **エントリー条件**:
  - **ロング**: SuperTrendが強気に転換 && クラスタートレンド > 0 && フィルター通過。
  - **ショート**: SuperTrendが弱気に転換 && クラスタートレンド < 0 && フィルター通過。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - ボリュームバランスまたは逆シグナル。
- **ストップ**: ボリュームベースのみ。
- **デフォルト値**:
  - `ATR Length` = 11.
  - `SuperTrend Factor` = 3.
  - `Training Data Length` = 200.
  - `Moving Average Length` = 50.
  - `Trend Strength Period` = 14.
  - `Trend Strength Threshold` = 20.
  - `Volume Ratio Threshold` = 0.9.
  - `Delay Bars` = 4.
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: はい
  - 複雑さ: 複雑
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
