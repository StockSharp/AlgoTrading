# ColorMETRO戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はColorMETROインジケーターに基づいて取引します。このインジケーターはRSIの周辺に速い線と遅い線のステップラインを構築します。
速い線が遅い線を上抜けするとロングポジションを開きます。速い線が遅い線を下抜けするとショートポジションを開きます。反対のポジションは同じシグナルでクローズされます。

## パラメーター
- **Candle Type** – 計算に使用するローソク足の種類。
- **RSI Period** – RSI計算の期間。
- **Fast Step** – 速い線のステップサイズ。
- **Slow Step** – 遅い線のステップサイズ。
- **Stop Loss** – ストップロス保護のポイント距離。
- **Take Profit** – テイクプロフィット保護のポイント距離。
- **Allow Buy** – ロングポジションのオープン許可。
- **Allow Sell** – ショートポジションのオープン許可。
- **Close Long** – ロングポジションのクローズ許可。
- **Close Short** – ショートポジションのクローズ許可。

戦略はストップロスとテイクプロフィットレベルを管理するために `StartProtection` を使用します。
