# StockSharp Strategy Designer における安値ブレイク・ストップ戦略の例
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

この例では、StockSharp Strategy Designer に設定された「安値ブレイク・ストップ」取引戦略を示します。この戦略は、特定の安値ブレイク条件に基づいて取引を実行するよう設計されており、リスク管理のためにストップロスパラメーターを組み込んでいます。リアルタイム市場データを活用して、証券の価格が一定期間にわたって事前定義した安値を下回るタイミングを特定し、設定されたストップ条件で取引を開始します。

![schema](schema.png)

## スキーマの説明

JSON ファイルで提供されるスキーマは、価格の動きと歴史的安値の関係に基づいて取引する詳細なワークフローを概説しています。

1. **証券ノード**: 主要な入力ノードで、市場価格データ入力の基盤として[対象証券を定義](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)します。

2. **TimeFrameCandle ノード**: 受信した市場データを処理して[ローソク足](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)を生成します。特定の時間間隔における価格変動の分析に不可欠です。

3. **最安値インジケーターノード**: 指定した期間の[最安値を計算](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)し、取引開始のための潜在的なブレイクアウトレベルを特定します。

4. **比較ノード**: 現在の価格を歴史的安値と[比較](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)し、現在価格が設定した閾値を下回ったときに取引シグナルを生成します。これは弱気ブレイクアウトを示します。

5. **チャートパネルノード**: 取引データと指標を視覚化し、戦略の操作を[グラフで表示](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)します。リアルタイムの監視と戦略調整に不可欠です。

6. **取引実行ノード（買い/売り）**: 戦略のロジックに基づいて[取引を実行](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)します。このケースでは、予想される下落を利用するために売り注文が実行される場合があります。

7. **ストップ注文ノード**: リスクを効果的に管理するために[ストップロス](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)条件を実装します。事前定義した損失閾値で取引を決済し、大きな不利な動きから保護します。

## ワークフロー

- **証券ノード**が戦略に必要な市場データを提供します。
- このデータは **TimeFrameCandle ノード**に流れ、利用可能なローソク足フォーマットに変換されます。
- **最安値インジケーターノード**がこれらのローソク足を分析して歴史的安値を決定します。
- **比較ノード**が現在の市場価格をこれらの安値と比較し、価格が歴史的安値を下回ったときに取引を有効化します。
- **取引実行ノード**がこれらのシグナルを使用して、下降トレンドの継続を想定した売り注文を実行します。
- 同時に、**ストップ注文ノード**が事前定義した基準に基づいてストップロス注文を設定し、潜在的な損失を管理します。
- **チャートパネルノード**がすべての取引と価格の動きを表示し、戦略のパフォーマンスを視覚的にフィードバックします。

## 実際の応用

この設定は、重要な価格変動を認識して対応することで収益機会を得られるブレイクアウト戦略に注力するトレーダーに特に有用です。この戦略は以下に適しています。
- 価格の大きな変動が取引機会をもたらす高ボラティリティ市場。
- 迅速な価格変動を活用し、リスクを効果的に管理するための堅牢なメカニズムを必要とするデイトレーダー。

## 結論

StockSharp Strategy Designer 内の「安値ブレイク・ストップ」戦略の例は、リアルタイムデータ処理と高度なリスク管理技術を組み合わせることで、アルゴリズム取引の先進的なアプローチを示しています。この戦略は、リスクパラメーターを厳守しながら価格ブレイクアウトを活用するための動的なフレームワークを提供し、精密かつ管理された取引手法によってリターンを最大化しようとするトレーダーに不可欠なツールです。
