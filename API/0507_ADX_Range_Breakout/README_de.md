# ADX-Bereichsausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet Long-Positionen, wenn der Schlusskurs den höchsten Schlusskurs eines Rückblickzeitraums überschreitet, während der ADX unter einem festgelegten Schwellenwert bleibt, was auf einen ruhigen Markt hinweist. Der Handel ist auf eine definierte Sitzung und eine maximale Anzahl von Trades pro Tag begrenzt. Ein fester Stop-Loss in Preiseinheiten schützt jede Position.

## Details

- **Einstiegskriterien**: `Close >= previous highest close` und `ADX < threshold` innerhalb der Sitzung
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Stop-Loss oder Sitzungsende
- **Stops**: Ja
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `HighestPeriod` = 34
  - `AdxThreshold` = 17.5
  - `StopLoss` = 1000
  - `MaxTradesPerDay` = 3
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Nur Long
  - Indikatoren: ADX
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
