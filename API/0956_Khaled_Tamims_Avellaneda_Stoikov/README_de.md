# Khaled Tamims Avellaneda-Stoikov-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert das Avellaneda-Stoikov Market-Making-Modell. Die Strategie berechnet Geld- und Briefkurse aus den letzten beiden Schlusskursen und platziert Marktorders, wenn der Kurs die konfigurierbaren Margen überschreitet.

## Details

- **Einstiegskriterien**:
  - **Long**: `close < bidQuote - M`
  - **Short**: `close > askQuote + M`
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `Gamma` = 2
  - `Sigma` = 8
  - `T` = 0.0833
  - `K` = 5
  - `M` = 0.5
  - `Fee` = 0
- **Filter**:
  - Kategorie: Market Making
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
