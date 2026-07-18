# DSL-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kombiniert Discontinued Signal Lines (DSL) mit ATR-Bändern und einem Beluga-Oszillator. Eine Long-Position wird eröffnet, wenn der Preis drei Bars lang über der DSL-Linie bleibt und der Oszillator seine untere DSL-Linie von unten kreuzt. Short-Positionen werden bei entgegengesetzten Bedingungen eröffnet. Jeder Trade verwendet das entsprechende DSL-Band als Stop und ein Risiko-Ertrags-Ziel für den Take Profit.

## Details

- **Einstiegskriterien**:
  - Oberes DSL-Band über der unteren Linie für Longs; unteres Band unter der oberen Linie für Shorts.
  - Kerzeneröffnung und -schluss über (oder unter) der DSL-Linie für drei aufeinanderfolgende Bars.
  - DSL-Beluga-Oszillator-Kreuzsignal.
- **Long/Short**: Long und Short.
- **Ausstiegskriterien**:
  - Stop-Loss am DSL-Band.
  - Take Profit beim Risiko-Ertrags-Vielfachen.
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `Length` = 34
  - `Offset` = 30
  - `BandsWidth` = 1
  - `RiskReward` = 1.5
  - `BelugaLength` = 10
  - `DslFastMode` = true
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: DSL, ATR, RSI
  - Stops: Ja
  - Komplexität: Hoch
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
