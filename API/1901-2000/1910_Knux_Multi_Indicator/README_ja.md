# Knux マルチインジケーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はトレンド強度インジケーターとモメンタムオシレーターを組み合わせてブレイクアウトを取引します。2つの移動平均線の強気または弱気のクロスオーバーを待ちながら、Average Directional Index (ADX)が強いトレンドを示すまで待機します。Relative Vigor Index (RVI)、Commodity Channel Index (CCI)、Williams %Rがフィルターとして機能し、モメンタムが動きを確認し、市場が過剰伸張していないことを保証します。

システムはロングとショートの両ポジションを開くことができます。反対のシグナルが現れるまでポジションを保持し、専用のストップロスは使用しません。インジケーターのピリオドや閾値などのすべてのパラメーターは設定可能です。

## 詳細

- **エントリー条件**:
  - **ロング**: 速いSMAが遅いSMAを上抜け、`ADX > AdxLevel`、`RVI`が上昇、`CCI < -CciLevel`、`WPR <= -100 + WprBuyRange`。
  - **ショート**: 速いSMAが遅いSMAを下抜け、`ADX > AdxLevel`、`RVI`が下降、`CCI > CciLevel`、`WPR >= -WprSellRange`。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対シグナル（反対方向へのクロスオーバー）。
- **ストップ**: 明示的なストップロスなし。
- **デフォルト値**:
  - `FastMaLength` = 5
  - `SlowMaLength` = 20
  - `AdxPeriod` = 14
  - `AdxLevel` = 15
  - `RviPeriod` = 20
  - `CciPeriod` = 40
  - `CciLevel` = 150
  - `WprPeriod` = 60
  - `WprBuyRange` = 15
  - `WprSellRange` = 15
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: なし
  - 複雑さ: 中程度
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
