# Liquiditäts-Sweep-Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Trendfolge-Strategie verwendet Bollinger-Bänder zur Erkennung der Marktrichtung und überwacht das Volumen auf potenzielle Liquiditäts-Sweeps. Eine Position wird eröffnet, wenn der Trend bullisch oder bärisch wird, abhängig vom gewählten Handelsmodus.

## Details

- **Einstiegskriterien**:
  - **Long**: Trend wird bullisch und Modus erlaubt Long-Trades.
  - **Short**: Trend wird bärisch und Modus erlaubt Short-Trades.
- **Long/Short**: Konfigurierbar über den Handelsmodus.
- **Ausstiegskriterien**:
  - **Long**: Trend wird bärisch oder Modus verbietet Long.
  - **Short**: Trend wird bullisch oder Modus verbietet Short.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 12.
  - `Multiplier` = 2.0.
  - `Major Sweep Threshold` = 50.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

