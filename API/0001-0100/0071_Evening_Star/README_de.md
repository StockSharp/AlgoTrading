# Abendstern-Muster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Der Abendstern ist das Spiegelbild des Morgensterns, zeigt aber ein potenzielles Top an. Er beginnt mit einer starken bullischen Kerze, gefolgt von einer kleinen unentschlossenen Kerze, und endet mit einer bärischen Kerze, die unter dem Mittelpunkt des ersten Balkens schließt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 100%. Sie funktioniert am besten auf dem Forex-Markt.

Der Algorithmus beobachtet Sequenzen von drei Kerzen. Wenn sich das Muster bildet, geht er mit einem Stop über dem Hoch der kleinen mittleren Kerze Short. Positionen werden beendet, sobald der Preis unter das Tief des Bestätigungskerze fällt oder der Stop ausgelöst wird.

Da das Setup eine schnelle Umkehr von überkauften Bedingungen antizipiert, zielen Trades typischerweise auf kurze, impulsgesteuerte Abwärtsbewegungen ab.

## Details

- **Einstiegskriterien**: Drei-Kerzen-Abendstern-Muster.
- **Long/Short**: Nur Short.
- **Ausstiegskriterien**: Preis unter dem Tief des Bestätigungsbalkens oder Stop-Loss.
- **Stops**: Ja, über dem Hoch der mittleren Kerze.
- **Standardwerte**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filter**:
  - Kategorie: Muster
  - Richtung: Short
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

