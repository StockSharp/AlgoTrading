# Simple 2 MA I戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

Simple 2 MA I は、元の MetaTrader エキスパートアドバイザーの中核ロジックを再現するトレンドフォロー戦略です。典型価格で計算した 2 本の線形加重移動平均 (LWMA) を使い、支配的なトレンドを識別します。momentum 確認と MACD 方向フィルターにより弱いシグナルを除外します。戦略は任意で、自動 stop-loss、take-profit、break-even 移動、ローソク足ベースの trailing stop によってリスクを管理します。

## 取引ロジック

### ロングセットアップ

1. 高速 LWMA が低速 LWMA より上にあり、上昇トレンドを確認します。
2. 2 バー前のローソク足の安値が前のバーの高値より下にあり、新しい強気構造を示します。
3. 直近 3 つの変化率読み取り値の少なくとも 1 つが、設定された momentum しきい値を上回っています。
4. MACD ラインがシグナルラインより上にあります。
5. ネットポジション数量が `Max Net Volume` 制限未満です。

すべての条件が満たされると、戦略はショートエクスポージャー (あれば) を閉じ、成行で買います。

### ショートセットアップ

1. 高速 LWMA が低速 LWMA より下にあり、下降トレンドを確認します。
2. 前のバーの安値が 2 期間前のバーの高値より下にあり、弱気構造を示します。
3. 直近 3 つの変化率読み取り値の少なくとも 1 つが、momentum しきい値 (絶対値) を上回っています。
4. MACD ラインがシグナルラインより下にあります。
5. ネットポジション数量が `Max Net Volume` 未満です。

条件が成立すると、戦略はロング (あれば) をカバーし、成行で売ります。

### リスク管理

* **Stop-loss / take-profit:** エントリー価格に対するポイント単位で定義される任意の固定距離。
* **Break-even:** 価格が利益側のトリガー距離に達すると、ストップをエントリー ± オフセットに移動します。
* **ローソク足trailing:** 起動距離に到達した後、ストップは設定可能なバッファーを加えたローソク足の極値に追随します。
* ポジションが閉じられると、保護注文は自動的にキャンセルされます。

## パラメーター

| 名前 | 説明 | デフォルト |
| ---- | ---- | ---------- |
| Candle Type | インジケーター計算に使う時間枠。 | 15 分足 |
| Fast LWMA | 高速 LWMA の期間。 | 6 |
| Slow LWMA | 低速 LWMA の期間。 | 85 |
| Momentum Length | 変化率インジケーターのルックバック期間。 | 14 |
| Momentum Threshold | 必要な最小絶対変化率値。 | 0.3 |
| MACD Fast | MACD で使う高速 EMA 長。 | 12 |
| MACD Slow | MACD で使う低速 EMA 長。 | 26 |
| MACD Signal | MACD で使うシグナル EMA 長。 | 9 |
| Use Stop-Loss | stop-loss 注文の配置を有効にします。 | true |
| Stop-Loss (points) | エントリー価格から stop-loss までの距離。 | 20 |
| Use Take-Profit | take-profit 注文の配置を有効にします。 | true |
| Take-Profit (points) | エントリー価格から take-profit までの距離。 | 50 |
| Use Break-Even | 自動 break-even 移動を有効にします。 | true |
| Break-Even Trigger | break-even 前に必要な利益 (ポイント)。 | 30 |
| Break-Even Offset | break-even に移動するときに追加されるオフセット (ポイント)。 | 30 |
| Use Candle Trailing | ローソク足の極値に基づく trailing stop を有効にします。 | true |
| Trailing Activation | trailing が有効になる前に必要な利益 (ポイント)。 | 40 |
| Trailing Padding | ローソク足の極値に追加される余分な距離 (ポイント)。 | 10 |
| Max Net Volume | 許可される最大絶対ネット数量。 | 1 |

## 注意事項

* すべての価格距離は、銘柄の価格ステップ (ポイント) で表されます。戦略はパラメーター値に銘柄の tick サイズを自動的に掛けます。
* デフォルトの時間枠マッピングは元のエキスパートのデフォルトに従いますが、自由に調整できます。
* 戦略は確定ローソク足を前提とします。未確定バーは、元の EA と一貫させるため無視されます。
