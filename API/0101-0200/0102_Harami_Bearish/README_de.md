# Bearish Harami Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Der Bearish Harami ist die Umkehrung der bullischen Version und erscheint nach einem Aufwärtsschwung.
Eine kleine Kerze bildet sich vollständig innerhalb des vorangegangenen bullischen Balkens und deutet darauf hin, dass der Aufwärtsimpuls nachlässt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 43%. Die Strategie funktioniert am besten am Aktienmarkt.

Die Strategie geht short, wenn diese Innenkerze schließt, und wettet auf eine Umkehr, da die Käufer ihre Überzeugung verlieren.

Ein prozentualer Stop über dem Muster-Hoch begrenzt das Risiko, und der Trade wird beendet, wenn der Preis auf neue Hochs ausbricht.

## Details

- **Einstiegskriterien**: Mustererkennung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

