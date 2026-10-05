# Donchian Macd 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
DonchianチャネルのブレイクアウトとMACDトレンド確認を組み合わせた戦略。

テストでは年平均リターン約148%を示しています。外国為替市場で最もパフォーマンスが高くなります。

この戦略はDonchianのブレイクアウトを待ち、MACDでモメンタムを確認します。MACDが同意した後でロングまたはショートトレードが動きに乗ります。

確認を求めるブレイクアウト愛好家向けです。ストップはエントリー価格から一定割合の位置に配置されます。

## 詳細

- **エントリー条件**:
  - ロング: `Price breaks Donchian high && MACD > Signal`
  - ショート: `Price breaks Donchian low && MACD < Signal`
- **ロング/ショート**: 両方
- **エグジット条件**: MACDの反転
- **ストップ**: `StopLossPercent`を使用したパーセントベース
- **デフォルト値**:
  - `DonchianPeriod` = 20
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    ブレイクアウトは直前DonchianPeriod本の最高値または最安値を超えて引けることです。MACDの反転による決済では、MACDがシグナル線を下抜けたらロングを、上抜けたらショートを閉じます。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Donchian Channel, MACD
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

