# RVI Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die RVI Crossover-Strategie verwendet den Relative Vigor Index und einen Filter auf Basis gleitender Durchschnitte.
Kauft, wenn der RVI seine Signallinie nach oben kreuzt, während der Preis unter dem EMA liegt, und verkauft, wenn der RVI die Signallinie nach unten kreuzt, während der Preis über dem EMA liegt.

## Details

- **Einstiegskriterien**: RVI kreuzt seine Signallinie mit EMA vs VWMA-Filter
- **Long/Short**: Beide
- **Ausstiegskriterien**: gegensätzliches Signal
- **Stops**: Nein
- **Standardwerte**:
  - `RviLength` = 10
  - `SignalLength` = 10
  - `EmaLength` = 31
  - `VwmaLength` = 1
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: RVI, SMA, EMA, VWMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
