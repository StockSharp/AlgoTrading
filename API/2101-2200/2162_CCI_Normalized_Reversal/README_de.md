# CCI Normalisierte Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Commodity Channel Index (CCI), um Umkehrungen zu erkennen, nachdem der Indikator extreme Zonen verlässt.

## Überblick

Der Indikator wird auf 8-Stunden-Kerzen mit einer konfigurierbaren Periode berechnet. Zwei Schwellenwerte definieren überkaufte und überverkaufte Bereiche. Wenn der CCI nach Erreichen eines Extremwerts wieder in diese Grenzen zurückkehrt, eröffnet die Strategie eine Position in entgegengesetzter Richtung und erwartet eine Mean Reversion.

## Handelsregeln

- **Long-Einstieg**: Vor zwei Bars lag der CCI über dem hohen Niveau und der vorherige Bar fiel darunter.
- **Short-Einstieg**: Vor zwei Bars lag der CCI unter dem niedrigen Niveau und der vorherige Bar stieg darüber.
- **Long schließen**: Der CCI des vorherigen Bars lag unter dem mittleren Niveau.
- **Short schließen**: Der CCI des vorherigen Bars lag über dem mittleren Niveau.

## Parameter

- `CciPeriod` – Rückblickperiode für den CCI.
- `HighLevel` – oberer CCI-Schwellenwert, der als überkauft gilt.
- `MiddleLevel` – mittlerer Schwellenwert zum Schließen von Positionen.
- `LowLevel` – unterer CCI-Schwellenwert, der als überverkauft gilt.
- `CandleType` – Kerzenserie für die Berechnungen (Standard 8 Stunden).

## Hinweise

Die Strategie öffnet maximal eine Position gleichzeitig und verwendet Marktaufträge. Das Standard-Risikomanagement wird über `StartProtection` aktiviert.
