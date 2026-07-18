# ARD-Orderverwalttungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den DeMarker-Indikator verwendet, der einen Schwellenwert von 0.5 kreuzt, um Positionen zu eröffnen.

Wenn DeMarker nach einem Rückgang unter den Schwellenwert fällt, kauft die Strategie. Wenn DeMarker nach einem Anstieg über den Schwellenwert steigt, verkauft sie. Der Ausstieg erfolgt beim entgegengesetzten Signal. Es werden weder Stop-Loss noch Take-Profit verwendet.

## Details

- **Einstiegskriterien**:
  - Long: `DeMarker kreuzt Threshold von oben nach unten`
  - Short: `DeMarker kreuzt Threshold von unten nach oben`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `DeMarkerPeriod` = 2
  - `Threshold` = 0.5
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Indikator
  - Richtung: Beide
  - Indikatoren: DeMarker
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
