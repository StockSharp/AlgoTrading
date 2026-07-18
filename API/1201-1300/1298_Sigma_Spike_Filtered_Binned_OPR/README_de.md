# Sigma Spike Gefilterter Binned OPR — Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sigma Spike Filtered Binned OPR sammelt die Verteilung der Open-Position-Ratio (OPR) und handelt, wenn die OPR nach einem Sigma-Spike in den Renditen extreme Bins erreicht.

## Details

- **Einstiegskriterien**: OPR in extremen Bins (<= `OprThreshold` oder >= `100 - OprThreshold`) mit optionalem Sigma-Spike-Filter
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegensätzliches Signal
- **Stops**: Nein
- **Standardwerte**:
  - `SigmaSpikeLength` = 20
  - `FilterBySigmaSpike` = true
  - `SigmaSpikeThreshold` = 2
  - `OprThreshold` = 10
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: StandardDeviation
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
