# Williams Alligator ATR 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は Williams Alligator インジケーターと ATR ベースのストップロスを組み合わせて使用します。Lips ラインが Jaw ラインを上抜けしたときにロングポジションを開きます。Lips が Jaw を下抜けするか、価格が ATR ベースのストップレベルまで下落したときにポジションを閉じます。

## 詳細
- **エントリー条件**: Lips が Jaw を上抜け。
- **エグジット条件**: Lips が Jaw を下抜けまたは ATR ストップロス。
- **インジケーター**: Smoothed Moving Averages, Average True Range。
- **タイプ**: ロングのみ。
