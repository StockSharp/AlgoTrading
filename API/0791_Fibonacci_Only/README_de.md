# Nur-Fibonacci-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Nur-Fibonacci-Strategie verwendet benutzerdefinierte Retracement-Niveaus von 19% und 82,56%, abgeleitet aus den letzten 100 Kerzen. Die Strategie steigt ein, wenn der Kurs diese Niveaus berührt oder durchbricht und die Kerzenrichtung dies bestätigt. Sie unterstützt optionale Ausbruchseinstiege, ATR-basierten Stop-Loss, Trailing-Stop und sieben gestaffelte Take-Profits.

## Details

- **Einstiegskriterien**: Berührung oder Ausbruch der Fibonacci-Niveaus mit Bestätigung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss, Trailing-Stop oder Take-Profit-Ziele
- **Stops**: ATR oder Prozent
- **Standardwerte**:
  - `CandleType` = 15 Minuten
- **Filter**:
  - Kategorie: Fibonacci
  - Richtung: Beide
  - Indikatoren: Highest, Lowest, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
