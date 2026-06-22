# Divergenz-Experte
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die RSI-Preisdivergенzen handelt. Erkennt eine bullische Divergenz, wenn der Preis ein niedrigeres Tief bildet, aber der RSI ein höheres Tief bildet, und eine bärische Divergenz, wenn der Preis ein höheres Hoch bildet, aber der RSI ein niedrigeres Hoch bildet. Eröffnet entsprechend Long- oder Short-Positionen und verwendet einen prozentualen Stop-Loss.

## Details

- **Einstiegskriterien:**
  - Long: Preis bildet ein neues Tief und RSI bildet ein höheres Tief (bullische Divergenz)
  - Short: Preis bildet ein neues Hoch und RSI bildet ein niedrigeres Hoch (bärische Divergenz)
- **Long/Short:** Beide
- **Ausstiegskriterien:**
  - Long: Preis erreicht Stop-Loss oder bärische Divergenz erscheint
  - Short: Preis erreicht Stop-Loss oder bullische Divergenz erscheint
- **Stops:** Prozent vom Einstiegspreis
- **Standardwerte:**
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter:**
  - Kategorie: Divergenz
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
