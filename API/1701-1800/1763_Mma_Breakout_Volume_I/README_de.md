# MMA Ausbruch-Volumen-I-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche, wenn der Schlusskurs eine langfristige geglättete Gleitende Durchschnitt (SMMA) kreuzt.
Eine Long-Position wird eröffnet, wenn der Kurs über die SMMA steigt, und eine Short-Position, wenn er darunter fällt.
Positionen werden geschlossen, wenn sich der Kurs gegen den Trade bewegt und einen Exponentiellen Gleitenden Durchschnitt (EMA) kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs kreuzt SMMA(200) von unten.
  - **Short**: Schlusskurs kreuzt SMMA(200) von oben.
- **Ausstiegskriterien**:
  - **Long**: Schlusskurs fällt unter EMA(5).
  - **Short**: Schlusskurs steigt über EMA(5).
- **Long/Short**: Beide.
- **Stops**: Kein fester Stop-Loss, der Ausstieg wird durch das EMA-Signal gesteuert.
- **Standardwerte**:
  - `SMMA period` = 200
  - `EMA period` = 5
  - `Candle type` = 5-Minuten-Kerzen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Gleitende Durchschnitte
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
