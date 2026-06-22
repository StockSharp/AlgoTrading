# TMA-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie nutzt Ausbrüche relativ zu einem Triangular Moving Average (TMA). Sie beobachtet eine konfigurierbare Kerzenserie und vergleicht den Schlusskurs der vorherigen Kerze mit dem TMA-Wert zuzüglich oder abzüglich benutzerdefinierter Offsets. Eine Long-Position wird eröffnet, wenn der vorherige Schlusskurs über `TMA + UpLevel` liegt, und eine Short-Position wird eröffnet, wenn er unter `TMA - DownLevel` liegt. Entgegengesetzte Signale kehren die Position um.

## Parameter

- **TMA Length** – Periode zur Berechnung des Triangular Moving Average.
- **Upper Level** – Preisoffset, der zum TMA addiert wird, um Long-Signale zu erkennen.
- **Lower Level** – Preisoffset, der vom TMA subtrahiert wird, um Short-Signale zu erkennen.
- **Candle Type** – Zeitrahmen der von der Strategie verwendeten Kerzen.

## Funktionsweise

1. Abonniert die ausgewählte Kerzenserie.
2. Bindet einen Triangular Moving Average-Indikator an die Kerzen.
3. Bei jeder abgeschlossenen Kerze:
   - Speichert die vorherigen TMA- und Schlusskurswerte.
   - Prüft, ob der vorherige Schlusskurs das obere oder untere Niveau überschritten hat.
   - Sendet Marktorders, um Positionen entsprechend zu eröffnen oder umzukehren.
4. Zeichnet Kerzen, Indikatorlinie und eigene Trades für die visuelle Analyse.

## Hinweise

Die Strategie verwendet Marktorders ohne Stop-Loss- oder Take-Profit-Verwaltung. Sie ist für Bildungszwecke gedacht und sollte vor dem Live-Trading mit geeigneten Risikokontrollen erweitert werden.
