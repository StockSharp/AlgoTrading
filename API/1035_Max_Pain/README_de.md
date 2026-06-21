# Max Pain-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet Long-Positionen, wenn sowohl Volumen als auch Kursbewegung konfigurierbare Schwellenwerte überschreiten und der VIX-Index unter einem bestimmten Niveau bleibt. Beim Einstieg wird ein volatilitätsbasierter Stop-Loss gesetzt und die Position nach einer festen Anzahl von Perioden geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: Volumen größer als Durchschnittsvolumen × `VolumeMultiplier` und Kursänderung größer als vorheriger Schlusskurs × `PriceChangeMultiplier` bei VIX unter `VixThreshold`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Stop-Loss bei `StopLossMultiplier` × Volatilität unterhalb des Einstiegspreises.
  - Position schließen nach `HoldPeriods` Bars.
- **Stops**: Ja.
- **Standardwerte**:
  - `LookbackPeriod` = 70.
  - `VolumeMultiplier` = 1.
  - `PriceChangeMultiplier` = 0.029.
  - `StopLossMultiplier` = 2.4.
  - `VixThreshold` = 44.
  - `HoldPeriods` = 8.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
  - `VixCandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Nur Long
  - Indikatoren: Volumen, Kursaktion, Volatilität
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
