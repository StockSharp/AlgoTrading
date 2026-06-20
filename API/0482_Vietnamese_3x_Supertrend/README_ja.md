# Vietnamese 3x Supertrend 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、異なるATR長と乗数を持つ3つのSuperTrendインジケーターを積み重ねます。低速トレンドが弱気でより高速なトレンドがプルバックの機会を示している場合にロングポジションをスケールインします。オプションのブレークイーブン・ストップが、価格が有利に動いた後の利益を保護します。

## 詳細

- **エントリー条件**:
  - 低速SuperTrendが下降トレンド。
  - **Long 1**: 中速が上昇トレンドで高速が下降トレンド。
  - **Long 2**: 中速が下降トレンドで価格が高速SuperTrendラインより上。
  - **Long 3**: 高速が下降トレンドで、その高速下降トレンド中の最高値を上抜けるブレイクアウト。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - すべてのSuperTrendが上向きに転換し、ローソク足が弱気で閉じる。
  - 平均エントリー価格が現在の終値より上。
  - 有効な場合のオプションのブレークイーブン・ストップ。
- **ストップ**: オプションのブレークイーブン・ストップ。
- **デフォルト値**:
  - `FastAtrLength` = 10
  - `FastMultiplier` = 1
  - `MediumAtrLength` = 11
  - `MediumMultiplier` = 2
  - `SlowAtrLength` = 12
  - `SlowMultiplier` = 3
  - `UseHighestOfTwoRedCandles` = False
  - `UseEntryStopLoss` = True
  - `UseAllDowntrendExit` = True
  - `UseAvgPriceInLoss` = True
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング
  - インジケーター: SuperTrend
  - ストップ: オプション
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
