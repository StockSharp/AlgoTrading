# Bitcoin CME-Spot-Spread
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt den Spread zwischen CME-Bitcoin-Futures und Bitfinex-BTCUSD-Spot mithilfe von Bollinger-Bändern.
Long, wenn der Spread unter das untere Band fällt; Short, wenn er über das obere Band steigt.
Positionen werden auf vier Take-Profit-Niveaus skaliert und nach einer festen Anzahl von Bars geschlossen.

## Details

- **Daten**: CME-Bitcoin-Futures und Bitfinex-BTCUSD-Spot.
- **Einstieg**: Long bei überverkauftem Spread, Short bei überkauftem Spread.
- **Ausstieg**: Gestaffelte Take-Profits oder Schließen nach Haltebars.
- **Instrumente**: Bitcoin-Futures.
- **Risiko**: Teilausstiege und zeitlich begrenztes Halten.
