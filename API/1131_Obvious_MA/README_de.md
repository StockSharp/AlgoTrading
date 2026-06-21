# OBVious MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie eröffnet eine Long-Position, wenn der OBV seinen gleitenden Durchschnitt für den Long-Einstieg nach oben kreuzt, und schließt sie, wenn der OBV den Ausstiegsdurchschnitt nach unten kreuzt. Short-Positionen werden eröffnet, wenn der OBV den Einstiegsdurchschnitt nach unten kreuzt, und geschlossen, wenn er den Ausstiegsdurchschnitt nach oben kreuzt. Ein Richtungsfilter erlaubt es, nur Long- oder nur Short-Trades zu aktivieren.

## Details

- **Einstiegskriterien**:
  - **Long**: OBV kreuzt den Long-Einstiegs-MA nach oben und die Richtung ist nicht Short.
  - **Short**: OBV kreuzt den Short-Einstiegs-MA nach unten und die Richtung ist nicht Long.
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: OBV kreuzt den Long-Ausstiegs-MA nach unten.
  - Short: OBV kreuzt den Short-Ausstiegs-MA nach oben.
- **Stops**: Keine.
- **Standardwerte**:
  - `LongEntryLength` = 190
  - `LongExitLength` = 202
  - `ShortEntryLength` = 395
  - `ShortExitLength` = 300
  - `TradeDirection` = "Long"
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: OBV, SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
