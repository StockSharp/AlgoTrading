# Hammer-Kerzen-Umkehr (Hammer Candle Reversal)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hammer-Kerzen markieren oft eine Intraday-Umkehr, nachdem der Verkaufsdruck nachlässt. Diese Strategie sucht nach dem Hammer-Muster und geht long, in der Erwartung einer Erholung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 64 %. Die Strategie eignet sich am besten für den Forex-Markt.

Das System erfordert einen unteren Docht von mindestens der doppelten Kerzenlänge und kaum einen oberen Docht. Nach der Erkennung kauft es mit der definierten Positionsgröße und wartet auf Gewinn oder Stop-Loss.

## Details

- **Einstiegskriterien**: Hammer-Kerze erkannt.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Stop-Loss oder diskretionärer Ausstieg.
- **Stops**: Ja.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Muster
  - Richtung: Nur Long
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
