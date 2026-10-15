# MAVA Xonax-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet exponentielle gleitende Durchschnitte von Eröffnungs- und Schlusskursen, um Richtungsänderungen zu erkennen. Stop-Loss- und Take-Profit-Abstände werden aus den Hoch- und Tief-EMAs abgeleitet, sodass Trades vordefinierte Risiko- und Ertragsniveaus haben.

## Details

- **Einstiegskriterien**:
  - **Long**: EMA der Eröffnung kreuzt EMA des Schlusses von unten, basierend auf den letzten zwei abgeschlossenen Kerzen.
  - **Short**: EMA der Eröffnung kreuzt EMA des Schlusses von oben, basierend auf den letzten zwei abgeschlossenen Kerzen.
- **Long/Short**: Beide
- **Stops**: Fester Stop-Loss und Take-Profit basierend auf EMA-Bereichen.
- **Standardwerte**:
  - `EmaPeriod` = 6
  - `CandleType` = TimeSpan.FromMinutes(240).TimeFrame()
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Langfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
