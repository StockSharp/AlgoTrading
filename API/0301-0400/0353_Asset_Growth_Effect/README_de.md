# Strategie des Vermögenswachstumseffekts
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht long bei Unternehmen mit dem geringsten Wachstum der Gesamtaktiva und short bei jenen mit dem höchsten Vermögenswachstum. Jedes Jahr im Juli wird das Portfolio anhand der aktuellsten Fundamentaldaten neu gewichtet.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 15%. Sie funktioniert am besten auf dem Aktienmarkt.

Das Vermögenswachstum wird aus den in Unternehmensberichten ausgewiesenen Gesamtaktiva berechnet. Aktien werden in Quantile eingestuft, das unterste Quantil wird gekauft, das oberste wird leerverkauft. Positionen werden für einen Ziel-Hebel dimensioniert und jährlich angepasst.

## Details

- **Einstiegskriterien**:
  - Long: Aktie im untersten Quantil des Vermögenswachstums.
  - Short: Aktie im obersten Quantil des Vermögenswachstums.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Positionen bei jährlicher Neugewichtung angepasst.
- **Stops**: Nein.
- **Standardwerte**:
  - `Quantiles` = 10
  - `Leverage` = 1m
  - `MinTradeUsd` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Fundamental
  - Richtung: Beide
  - Indikatoren: Fundamentals
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Langfristig
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
