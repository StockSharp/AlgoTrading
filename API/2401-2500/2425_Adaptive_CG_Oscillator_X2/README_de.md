# Adaptiver CG-Oszillator X2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet den Adaptiven CG-Oszillator auf zwei verschiedenen Zeitrahmen.
Der höhere Zeitrahmen definiert den vorherrschenden Trend, während der niedrigere Zeitrahmen
tatsächliche Einstiege und Ausstiege basierend auf Oszillator-Crossovers verwaltet.

## Details

- **Einstiegskriterien**:
  - Long: Oszillator kreuzt seine Signallinie von oben nach unten, während der globale Trend aufwärts gerichtet ist
  - Short: Oszillator kreuzt seine Signallinie von unten nach oben, während der globale Trend abwärts gerichtet ist
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegensignal oder explizite Schließ-Flags
- **Stops**: Keine
- **Standardwerte**:
  - `TrendAlpha` = 0.07m
  - `SignalAlpha` = 0.07m
  - `TrendCandleType` = TimeSpan.FromHours(6).TimeFrame()
  - `SignalCandleType` = TimeSpan.FromMinutes(30).TimeFrame()
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Adaptive CG Oscillator
  - Stops: Keine
  - Komplexität: Mittel
  - Zeitrahmen: Multi-Zeitrahmen
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
