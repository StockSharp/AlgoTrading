# 強気ダイバージェンス短期ロングトレード発見戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は価格とRSI間の強気ダイバージェンスを探します。価格が安値を更新する一方でRSIが指定のピボット範囲内でより高い安値を形成し、時間足RSIが40を下回ったとき、戦略はロングポジションをエントリーします。RSIが閾値を上回るか、弱気ダイバージェンスが現れるか、ストップロスが発動したときにポジションをクローズします。

- **エントリー条件**:
  - 現在の安値が前のピボット安値の価格を下回る。
  - RSIが`RsiBullConditionMin`を下回りより高い安値を形成し、前のピボットが5–50本以内に現れる。
  - 時間足RSIが`RsiHourEntryThreshold`を下回る。
  - 終値が前のピボット安値の価格を下回る。
- **エグジット条件**:
  - RSIが`SellWhenRsi`を上抜け。
  - 弱気ダイバージェンス：価格がより高い高値を付ける一方でRSIがより低い高値を付ける。
  - `StopLossPercent`における`StartProtection`によりストップロスが発動。
- **インジケーター**: RSI。
