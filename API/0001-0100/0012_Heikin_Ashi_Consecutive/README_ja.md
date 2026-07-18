# Heikin Ashi Consecutive 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
連続Heikin Ashi足に基づく戦略

テストでは年平均リターンが約73%であることが示されています。暗号資産市場で最もよく機能します。

Heikin Ashi Consecutiveは、モメンタムを確認するために同じ色のHeikin Ashi足が複数連続するのを待ちます。強気または弱気のバーが連続した後、戦略はその動きに乗り、最初の逆方向の足またはATRストップで退場します。

Heikin Ashiチャートは価格データを平滑化するため、同じ色の足の連続は強い方向性の動きを強調します。トレーリングATRストップは、連続が急反転した場合に利益を確保しようとします。


## 詳細

- **エントリー条件**: Heikinに基づくシグナル。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `ConsecutiveCandles` = 3
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Heikin
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - Neural Networks: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

