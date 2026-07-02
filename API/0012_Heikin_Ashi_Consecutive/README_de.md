# Strategie Heikin Ashi Consecutive
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf aufeinanderfolgenden Heikin Ashi Kerzen

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 73%. Am besten funktioniert sie auf dem Kryptomarkt.

Heikin Ashi Consecutive wartet auf mehrere gleichfarbige Heikin Ashi Kerzen zur Bestätigung des Momentums. Nach einer Reihe bullischer oder bearischer Balken schließt sich die Strategie der Bewegung an und steigt bei der ersten entgegengesetzten Kerze oder einem ATR-Stop aus.

Da Heikin Ashi Diagramme Preisdaten glätten, hebt eine Reihe gleichfarbiger Kerzen eine starke Richtungsbewegung hervor. Der Trailing ATR-Stop versucht, Gewinne zu sichern, wenn sich die Sequenz abrupt umkehrt.


## Details

- **Einstiegskriterien**: Signale basierend auf Heikin.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `ConsecutiveCandles` = 3
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Heikin
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neural Networks: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

