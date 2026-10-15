# Synthetische Kreditzinsen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie nutzt Unterschiede zwischen synthetischen Kreditzinsen aus Derivatemärkten und On-Chain-Kreditrenditen. Durch Aufnahme von Krediten bei niedrigen Zinsen und Ausleihe bei hohen Zinsen wird der Spread zwischen beiden erfasst.

Positionen werden regelmäßig neu ausbalanciert, um Neutralität zu wahren. Das Risiko wird durch Zinsänderungsschwellen und Liquiditätsfilter gesteuert.

## Details

- **Daten**: Perpetual-Swap-Funding und DeFi-Kreditzinsen.
- **Einstieg**: Kredit aufnehmen am Niedrigzins-Venue und verleihen am Hochzins-Venue, wenn Spread > Schwellenwert.
- **Ausstieg**: Schließen, wenn der Spread zum Mittelwert zurückkehrt oder die Liquidität nachlässt.
- **Instrumente**: Perpetual Swaps und DeFi-Plattformen.
- **Risiko**: Spread-Obergrenze und Liquiditätsstopp.

