# 1ポイントあたりの出来高戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は各ローソク足の価格ポイントあたりの出来高を計算します。ローソク足のレンジが縮小しながら出来高が増加し、RSIフィルター（有効時）がシグナルを確認した場合にロングポジションを開きます。レンジが拡大しながら出来高が収縮した場合にショートポジションを開きます。

## パラメーター
- **RSI Length** – RSI計算の期間。
- **RSI Above/Below** – オプションのRSIフィルターの閾値。
- **Use RSI Filter** – RSIフィルタリングを有効または無効にする。
- **Candle Type** – 入力ローソク足の時間軸。
