# OBV トラフィックライト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Heikin Ashi ベースの On-Balance Volume と信号機のように色付けされた3本のEMAを使用します。OBVと速いEMAが遅いEMAを上回るときにロング、両方が下回るときにショート。条件が消えたときにポジションをクローズします。

- **エントリー条件**: ロングは OBV > 遅いEMA かつ 速いEMA > 遅いEMA；ショートは OBV < 遅いEMA かつ 速いEMA < 遅いEMA。
- **エグジット条件**: 逆シグナルまたは一致の喪失。
- **インジケーター**: OBV, EMA, Highest/Lowest
