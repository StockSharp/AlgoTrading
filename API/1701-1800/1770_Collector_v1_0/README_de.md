# Collector v1.0-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie eröffnet Marktorders, wenn der Preis dynamische Kauf- oder Verkaufsniveaus erreicht, die durch einen festen Abstand getrennt sind. Das Volumen erhöht sich nach einer bestimmten Anzahl von Trades. Alle Positionen werden geschlossen, sobald der kumulierte Gewinn einen Schwellenwert überschreitet.

## Details

- **Einstiegskriterien**:
  - Long: Schlusskurs >= Kaufniveau
  - Short: Schlusskurs <= Verkaufsniveau
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Alles schließen, wenn Gesamtgewinn >= ProfitClose
- **Stops**: Keine
- **Standardwerte**:
  - `Distance` = 10m
  - `InitialVolume` = 0.01m
  - `VolumeStep` = 0.01m
  - `IncreaseTrade` = 3
  - `MaxTrades` = 200
  - `ProfitClose` = 500000m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Grid
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
