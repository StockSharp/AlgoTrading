# Nur-Fibonacci-Strategie V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt an den Fibonacci-Retracement-Niveaus 19% und 82,56%, berechnet über 93 Kerzen. Einstiege erfolgen, wenn der Kurs diese Niveaus berührt oder durchbricht und eine Kerzenbestätigung vorliegt. Das Risiko wird über einen optionalen ATR-basierten Stop-Loss und einen Trailing-Stop gesteuert.

## Details

- **Einstiegskriterien**: Berührung oder Ausbruch der Fibonacci-Niveaus 19% / 82,56% mit Kerzenbestätigung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder Trailing-Stop
- **Stops**: Ja
- **Standardwerte**:
  - `CandleType` = 15 Minuten
- **Filter**:
  - Kategorie: Fibonacci-Ausbruch
  - Richtung: Beide
  - Indikatoren: ATR, Highest, Lowest
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
