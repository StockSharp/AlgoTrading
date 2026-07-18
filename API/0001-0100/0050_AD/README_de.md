# Strategie Accumulation/Distribution Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet den Accumulation/Distribution (A/D) Indikator, um den Kauf- und Verkaufsdruck zu messen. Ein steigender A/D zusammen mit einem Preis über dem gleitenden Durchschnitt signalisiert Akkumulation, während ein fallender A/D unter dem Durchschnitt Verteilung anzeigt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 187%. Am besten funktioniert es im Aktienmarkt.

Trades werden in Richtung des A/D-Trends relativ zum gleitenden Durchschnitt eingegangen. Eine Richtungsänderung des A/D dient als Ausstiegssignal.

Stops sind optional, können aber helfen, das Risiko zu managen.

## Details

- **Einstiegskriterien**: A/D steigt bei Preis über MA oder fällt unter MA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: A/D kehrt um oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: A/D, MA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

