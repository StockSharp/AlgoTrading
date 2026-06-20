# Trendfolge-Strategie nach Anlageklassen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie folgt Trends über mehrere Anlageklassen hinweg. Sie wendet einen einfachen gleitenden Durchschnittsfilter auf jedes Wertpapier im Universum an und gewichtet das Portfolio am ersten Handelstag jedes Monats neu. Positionen werden nur dann eingegangen, wenn der Preis über dem gleitenden Durchschnitt liegt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 15%. Sie funktioniert am besten bei diversifizierten Futures-Portfolios.

Zu Beginn jedes Monats erhalten Wertpapiere, die über ihrem SMA handeln, eine gleichmäßige Kapitalzuweisung. Positionen werden geschlossen, wenn der Preis unter den SMA fällt oder wenn das Kapital bei der nächsten Neugewichtung umverteilt wird.

## Details

- **Einstiegskriterien**: `Close > SMA`
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: `Close <= SMA` oder Entfernung bei Neugewichtung
- **Stops**: Keine; Kapital wird monatlich umverteilt
- **Standardwerte**:
  - `SmaLength` = 210
  - `MinTradeUsd` = 50
  - `CandleType` = daily
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Langfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
