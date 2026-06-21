# Kloss-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Kloss-Strategie kombiniert einen gewichteten gleitenden Durchschnitt (WMA), den Commodity Channel Index (CCI) und den Stochastischen Oszillator. Alle Indikatoren werden auf verschobenen historischen Werten ausgewertet, sodass Signale auf dem vergangenen Marktkontext basieren können. Eine Long-Position wird eröffnet, wenn der CCI unter eine negative Schwelle fällt, die Hauptlinie des Stochastischen Oszillators unter eine Abweichung vom neutralen 50-Niveau fällt und der verschobene Preis über dem verschobenen WMA liegt. Eine Short-Position wird bei umgekehrten Bedingungen eröffnet. Das optionale Umkehrschließen beendet eine bestehende Position, wenn das entgegengesetzte Signal erscheint. Stop-Loss und Take-Profit werden in Punkten vom Einstiegspreis festgelegt.

## Details

- **Einstiegskriterien**:
  - **Long**: Verschobener CCI unter `-CciDiffer`, verschobener Stochastik unter `50 - StochDiffer`, und verschobener Preis über dem verschobenen WMA.
  - **Short**: Verschobener CCI über `CciDiffer`, verschobener Stochastik über `50 + StochDiffer`, und verschobener Preis unter dem verschobenen WMA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Umkehrsignal, wenn `RevClose` aktiviert ist, oder Stop-Loss / Take-Profit-Niveaus.
- **Stops**: Absoluter Stop-Loss und Take-Profit in Punkten.
- **Filter**:
  - Indikator- und Preisverschiebungen über `CommonShift` ermöglichen die Signalgenerierung aus vergangenen Balken.
