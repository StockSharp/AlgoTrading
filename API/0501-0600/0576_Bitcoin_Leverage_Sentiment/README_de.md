# Bitcoin Hebel-Sentiment-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie analysiert den Z-Score des Verhältnisses zwischen Bitcoin-Long- und Short-Positionen. Ein Long-Trade wird eröffnet, wenn der Z-Score einen konfigurierbaren Schwellenwert nach oben kreuzt, und geschlossen, wenn er unter das Long-Exit-Niveau fällt. Short-Trades verwenden gespiegelte Schwellenwerte. Die Handelsrichtung kann auf long, short oder beides beschränkt werden.

## Details

- **Einstiegskriterien**:
  - Z-Score kreuzt den Long-Einstiegsschwellenwert nach oben → long.
  - Z-Score kreuzt den Short-Einstiegsschwellenwert nach unten → short.
- **Long/Short**: Konfigurierbar
- **Ausstiegskriterien**:
  - Z-Score kreuzt den Long-Ausstiegsschwellenwert nach unten.
  - Z-Score kreuzt den Short-Ausstiegsschwellenwert nach oben.
- **Stops**: Keine
- **Standardwerte**:
  - Z-Score-Länge = 252
  - Long-Einstieg = 1.0
  - Long-Ausstieg = -1.618
  - Short-Einstieg = -1.618
  - Short-Ausstieg = 1.0
  - Kerzentyp = 1 Tag
- **Filter**:
  - Kategorie: Sentiment
  - Richtung: Beide
  - Indikatoren: SMA, StdDev
  - Stops: Keine
  - Komplexität: Niedrig
  - Zeitrahmen: Langfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
