# Strategie zum Schließen bei Kijun-Sen-Kreuzung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie dient als Handelsmanagement-Tool. Sie schließt bestehende Positionen, wenn der Schlusskurs die Kijun-sen-Linie des Ichimoku-Indikators kreuzt.

Während der Ausführung abonniert die Strategie Kerzen und berechnet den Kijun-sen-Wert. Wenn eine Long-Position vorhanden ist und der Preis unter die Kijun-Linie um einen konfigurierbaren Versatz fällt, wird die Position geschlossen. Wenn eine Short-Position offen ist und der Preis über die Linie steigt, wird die Position ebenfalls geschlossen. Die Strategie eröffnet keine neuen Trades.

## Details

- **Einstiegskriterien**: Die Strategie eröffnet keine neuen Trades; sie verwaltet nur bestehende Positionen.
- **Long/Short**: Beide (Schließen).
- **Ausstiegskriterien**: Schlusskurs kreuzt die Kijun-sen-Linie um den angegebenen Versatz.
- **Stops**: Keine.
- **Standardwerte**:
  - `KijunPeriod` = 50
  - `PointsToCross` = 0
  - `CandleType` = 5 Minuten
- **Filter**:
  - Kategorie: Handelsmanagement
  - Richtung: Beide
  - Indikatoren: Ichimoku
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
