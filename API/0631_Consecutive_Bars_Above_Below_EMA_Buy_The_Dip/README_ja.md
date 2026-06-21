# EMA上下連続バー押し目買い戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、移動平均線を下回る連続した終値が続いた後に買いを入れ、価格が前のローソク足の高値を上回って終値をつけたときにポジションを手仕舞います。

## 詳細

- **エントリー**: N本連続でSMAまたはEMAを下回る終値が続いたときに買い。
- **エグジット**: 終値が前のローソク足の高値を上回ったときにポジションをクローズ。
- **インジケーター**: SMAまたはEMA。
