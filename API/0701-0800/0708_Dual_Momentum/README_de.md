# Duales Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Wechselt zwischen einem riskanten und einem sicheren Asset anhand von dualem Momentum.
Die Strategie investiert nur dann in das riskante Asset, wenn dessen Momentum positiv und größer als das Momentum des sicheren Assets ist.

## Details

- **Einstiegskriterien**: Riskantes Momentum > 0 und > sicheres Momentum
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Wechsel zum sicheren Asset, wenn die Bedingung nicht erfüllt ist
- **Stops**: Nein
- **Standardwerte**:
  - `Period` = 12
  - `CandleType` = TimeSpan.FromDays(30).TimeFrame()
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Nur Long
  - Indikatoren: RateOfChange
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Monatlich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
