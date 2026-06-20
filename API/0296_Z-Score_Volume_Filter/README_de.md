# Z-Score Strategie mit Volumen-Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Z-Score Strategie mit Volumen-Filter verwendet den Z-Score zusammen mit Volatilitätsfiltern. Trades werden nur dann eingegangen, wenn bestimmte Bedingungen erfüllt sind.

Signale erfordern, dass der Indikator einen Schwellenwert überschreitet, während die Volatilität vordefinierten Kriterien entspricht. Positionen können Long oder Short sein und verfügen über integrierte Stops.

Konzipiert für Trader, die Risikokontrolle schätzen, schließt die Strategie, sobald der Indikator zur Mitte zurückkehrt oder sich die Volatilität ändert. Starteinstellung `LookbackPeriod` = 20.

## Details

- **Einstiegskriterien**: Der Indikator kreuzt zurück in Richtung Mittelwert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Der Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `LookbackPeriod` = 20
  - `ZScoreThreshold` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Z-Score
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
