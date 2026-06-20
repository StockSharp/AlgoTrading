# Ausbrüche und Retests
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die bei Ausbrüchen aus jüngsten Hochs oder Tiefs und optionalen Retests mit Trailing-Stop-Management einsteigt.

Der Ansatz verfolgt Unterstützung und Widerstand, definiert durch die höchsten und niedrigsten Schlusskurse über ein Rückblickfenster. Ausbrüche öffnen Positionen in der Ausbruchsrichtung oder warten auf einen Retest des gebrochenen Niveaus. Ausstiege verwenden einen anfänglichen Stop-Loss, der sich in einen Trailing-Stop verwandelt, sobald der Gewinn einen Schwellenwert erreicht.

## Details

- **Einstiegskriterien**: Ausbruch über Widerstand oder unter Unterstützung, optionaler Retest.
- **Long/Short**: Konfigurierbar.
- **Ausstiegskriterien**: Trailing-Stop oder entgegengesetzter Ausbruch.
- **Stops**: Anfänglicher Stop-Loss und Trailing-Stop.
- **Standardwerte**:
  - `LookbackPeriod` = 20
  - `RetestBarsSinceBreakout` = 2
  - `RetestDetectionLimit` = 2
  - `ProfitThresholdPercent` = 5m
  - `TrailingStopGapPercent` = 1m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Highest, Lowest
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
