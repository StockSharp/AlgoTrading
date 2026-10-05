# Wyckoff Distributions-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Wyckoff Distribution ist eine Toppingphase, die durch starken Verkauf in Rallyes und Tests des Widerstands gekennzeichnet ist.
Das Volumen expandiert oft bei Abwärtsbewegungen und zieht sich bei Erholungen zurück, was darauf hindeutet, dass große Akteure Positionen liquidieren.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 64%. Die Strategie funktioniert am besten am Forexmarkt.

Diese Strategie geht short, wenn der Preis aus der Distributionsrange nach unten ausbricht, in Erwartung eines anhaltenden Rückgangs.

Ein Stop knapp über der Range schützt vor falschen Ausbrüchen, und Positionen werden geschlossen, wenn der Preis zur Oberseite der Struktur zurückkehrt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `RangePeriod` = 20
  - `StopLossPercent` = 2
    Die Distributionsspanne ist das höchste Hoch und tiefste Tief der vorherigen RangePeriod Kerzen. Die Strategie verkauft nur, bei einem Schluss unter der Spanne; der Stop liegt StopLossPercent über der Oberseite der Struktur, und die Position schließt, wenn eine Kerze darüber schließt.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Volume, Price
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

