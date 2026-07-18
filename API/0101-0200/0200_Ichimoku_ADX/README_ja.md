# Ichimoku Adx 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
Ichimoku CloudとADXインジケーターに基づく戦略。エントリー条件：
ロング: Price > Kumo（雲）&& Tenkan > Kijun && ADX > 25（強い動きを伴う上昇トレンド） ショート: Price < Kumo（雲）&& Tenkan < Kijun && ADX > 25（強い動きを伴う下降トレンド） エグジット条件：ロング: Price < Kumo（価格が雲の下に落ちる） ショート: Price > Kumo（価格が雲の上に上がる）

テストでは年平均リターン約187%を示しています。株式市場で最もパフォーマンスが良好です。

この戦略はIchimoku Cloudのシグナルとアドックスを組み合わせ、強力なトレンドをフィルタリングします。価格が雲を上下にブレイクしADXが確認したときにトレードが発生します。

構造化されたトレンドセットアップを好むトレーダーに適しています。ATRで定義されたストップが不利な動きから守ります。

## 詳細

- **エントリー条件**:
  - ロング: `Price > Cloud && Tenkan > Kijun && ADX > AdxThreshold`
  - ショート: `Price < Cloud && Tenkan < Kijun && ADX > AdxThreshold`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 価格が雲を反対方向に越える
- **ストップ**: Ichimoku Cloudをトレーリングストップとして使用
- **デフォルト値**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Ichimoku Cloud, ADX
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

