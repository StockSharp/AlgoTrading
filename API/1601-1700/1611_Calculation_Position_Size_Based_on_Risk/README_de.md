# Strategie zur Berechnung der Positionsgröße basierend auf Risiko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Demonstriert die Berechnung der Handelsgröße basierend auf dem Kontorisiko und einem Stop-Loss-Prozentsatz. Die Einstiege sind zufällig, um die Logik der Positionsgrößenberechnung zu zeigen.

## Details

- **Einstiegskriterien**:
  - **Long**: jede 333. Kerze.
  - **Short**: jede 444. Kerze.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Nur Stop Loss.
- **Stops**: Stop Loss.
- **Standardwerte**:
  - `Stop Loss %` = 10
  - `Risk Value` = 2
  - `Risk Is Percent` = true
  - `Long Period` = 333
  - `Short Period` = 444
- **Filter**:
  - Kategorie: Risk Management
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
