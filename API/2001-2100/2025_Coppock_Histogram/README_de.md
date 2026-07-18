# Coppock-Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Umkehrungen des Coppock-Histogramms. Der Indikator summiert zwei Rate of Change-Werte und glättet das Ergebnis mit einem gleitenden Durchschnitt. Wenn der Momentum nach oben dreht, eröffnet die Strategie Long-Positionen und schließt Shorts. Eine Abwärtsdrehung schließt Longs und eröffnet Shorts. Signale werden nur auf abgeschlossenen Kerzen ausgewertet.

## Details

- **Einstiegskriterien**: Coppock-Histogramm mit Aufwärtsneigung für Käufe oder Abwärtsneigung für Verkäufe.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal schließt offene Positionen.
- **Stops**: Kein expliziter Stop-Loss oder Take-Profit.
- **Standardwerte**:
  - `Roc1Period` = 14
  - `Roc2Period` = 11
  - `SmoothPeriod` = 3
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromHours(8)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RateOfChange, SimpleMovingAverage
  - Stops: Keine
  - Komplexität: Grundlegend
  - Zeitrahmen: 8H
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
