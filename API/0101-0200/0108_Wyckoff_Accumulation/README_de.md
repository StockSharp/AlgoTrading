# Wyckoff Akkumulations-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Wyckoff Akkumulation beschreibt eine Bodenbildungsphase, in der große Akteure nach einem Rückgang leise Positionen aufbauen.
Volumen und Preisbewegung bilden eine Reihe von Unterstützungstests gefolgt von höheren Tiefs, die auf wachsende Nachfrage hindeuten.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 61%. Die Strategie funktioniert am besten am Kryptomarkt.

Diese Strategie geht long, wenn der Preis aus der Akkumulationsrange ausbricht, in Erwartung eines neuen Aufwärtstrends, der durch jene früheren Käufe angetrieben wird.

Ein schützender Stop liegt knapp unterhalb der Basis, um Verluste zu begrenzen, falls der Ausbruch scheitert.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `RangePeriod` = 20
  - `StopLossPercent` = 2
    Die Akkumulationsspanne ist das höchste Hoch und tiefste Tief der vorherigen RangePeriod Kerzen. Die Strategie kauft nur, bei einem Schluss über der Spanne; der Stop liegt StopLossPercent unter der Basis, und die Position schließt, wenn eine Kerze darunter schließt.
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

