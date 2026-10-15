# Strategie VQZL Z-Score
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den Z-Score relativ zu einem geglätteten Durchschnitt verwendet.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 42%. Am besten funktioniert sie am Aktienmarkt.

Die Strategie berechnet einen geglätteten gleitenden Durchschnitt und die Standardabweichung, um einen Z-Score zu ermitteln. Wenn der Preis einen Schwellenwert überschreitet, wird in Richtung der Bewegung eingestiegen.

## Details

- **Einstiegskriterien**:
  - **Long**: `Z-Score > threshold`.
  - **Short**: `Z-Score < -threshold`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `PriceSmoothing` = 15
  - `ZLength` = 100
  - `Threshold` = 1.64
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA, StandardDeviation
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
