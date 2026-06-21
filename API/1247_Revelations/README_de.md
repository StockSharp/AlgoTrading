# Revelations-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Volatilitäts-Ausbruchsstrategie, die bei starken ATR-Spikes einsteigt, die durch lokale Extrema und einen Regime-Index bestätigt werden. Die Positionsgröße passt sich der Spike-Stärke an.

## Details

- **Einstiegskriterien**:
  - **Long**: ATR-Spike nach oben an einem lokalen Tief mit Regime-Bestätigung.
  - **Short**: ATR-Spike nach unten an einem lokalen Hoch mit Regime-Bestätigung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Take-Profit oder Stop-Loss erreicht.
- **Stops**: Feste Prozent-Stops.
- **Standardwerte**:
  - `ATR Fast` = 14
  - `ATR Slow` = 21
  - `ATR StdDev` = 12
  - `Spike Threshold` = 0.5
  - `Super Spike Mult` = 1.5
  - `Regime Window` = 8
  - `Regime Events` = 3
  - `Local Window` = 3
  - `Max Quantity` = 2
  - `Min Quantity` = 1
  - `Stop %` = 0.9
  - `Take Profit %` = 1.8
- **Filter**:
  - Kategorie: Volatilitäts-Ausbruch
  - Richtung: Long/Short
  - Indikatoren: ATR, SMA, Highest/Lowest
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
