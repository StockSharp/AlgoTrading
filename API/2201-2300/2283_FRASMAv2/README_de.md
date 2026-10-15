# Strategie FRASMAv2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Fractal Adaptive Simple Moving Average (FRASMAv2).

Diese Strategie berechnet einen fraktal-adaptiven einfachen gleitenden Durchschnitt unter Verwendung des Fractal-Dimension-Indikators. Die Indikatorfarbe ändert sich je nach Steigung: grün für steigend, grau für seitwärts, magenta für fallend. Die Strategie überwacht Farbwechsel bei der letzten abgeschlossenen Kerze:

- Wenn der Indikator auf dem vorherigen Balken grün war und auf dem letzten Balken nicht grün (grau oder magenta) wird, schließt die Strategie Short-Positionen und eröffnet eine neue Long-Position.
- Wenn der Indikator magenta war und nicht mehr magenta wird, schließt die Strategie Long-Positionen und eröffnet eine neue Short-Position.

Das Risikomanagement verwendet Stop-Loss- und Take-Profit-Parameter, die in Punkten angegeben werden.

## Details

- **Einstiegskriterien**: Farbwechsel von FRASMAv2.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Farbwechsel.
- **Stops**: Take-Profit und Stop-Loss über das Schutzmodul.
- **Standardwerte**:
  - `Period` = 30
  - `TakeProfit` = 2000 Punkte
  - `StopLoss` = 1000 Punkte
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trendumkehr
  - Richtung: Beide
  - Indikatoren: FractalDimension, FRASMAv2
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: 4h
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
