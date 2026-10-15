# Morgenstern-Muster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Der Morgenstern ist eine bullische Kerzenformation, die nach einem Rückgang einen potenziellen Boden signalisiert. Er besteht aus einer großen bärischen Kerze, einer kleinen unentschlossenen Kerze und einer starken bullischen Kerze, die über dem Mittelpunkt des ersten Balkens schließt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 97%. Sie funktioniert am besten auf dem Kryptomarkt.

Diese Strategie verfolgt Sequenzen von drei Kerzen. Wenn das Muster erscheint, wird eine Long-Position mit einem Stop unterhalb der kleinen mittleren Kerze eröffnet. Ausstiege erfolgen, sobald der Preis über das Hoch des Bestätigungsbalkens steigt oder der Stop erreicht wird.

Da das Muster oft schnelle Erholungen aus überverkauften Bedingungen auslöst, sind Trades meist kurzlebig und erfassen den anfänglichen Aufwärtsschub.

## Details

- **Einstiegskriterien**: Drei-Kerzen-Morgenstern-Muster.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Preis über dem Hoch des Bestätigungsbalkens oder Stop-Loss.
- **Stops**: Ja, unterhalb des Tiefs der mittleren Kerze.
- **Standardwerte**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filter**:
  - Kategorie: Muster
  - Richtung: Long
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

