# Big-Runner-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Big-Runner-Strategie handelt, wenn der Schlusskurs und ein schneller SMA beide in Richtung eines langsameren SMA kreuzen, was auf starken Momentum hindeutet. Die Positionsgröße wird als Prozentsatz des Portfoliowertes multipliziert mit dem Hebel berechnet. Optionale Stop-Loss- und Take-Profit-Niveaus steuern das Risiko.

## Details

- **Einstiegskriterien**:
  - Kaufen, wenn der Schlusskurs den schnellen SMA von unten kreuzt und der schnelle SMA den langsamen SMA von unten kreuzt.
  - Verkaufen, wenn der Schlusskurs den schnellen SMA von oben kreuzt und der schnelle SMA den langsamen SMA von oben kreuzt.
- **Long/Short**: Long und Short.
- **Ausstiegskriterien**:
  - Optionaler Stop-Loss und Take-Profit basierend auf dem Einstiegspreis.
  - Das entgegengesetzte Signal schließt die bestehende Position.
- **Stops**: Konfigurierbare Stop-Loss- und Take-Profit-Prozentsätze.
- **Standardwerte**:
  - `FastLength` = 5
  - `SlowLength` = 20
  - `TakeProfitLongPercent` = 4
  - `TakeProfitShortPercent` = 7
  - `StopLossLongPercent` = 2
  - `StopLossShortPercent` = 2
  - `PercentOfPortfolio` = 10
  - `Leverage` = 1
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: SMA
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
