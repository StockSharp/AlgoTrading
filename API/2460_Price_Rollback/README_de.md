# Preisrücksetzer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt tägliche Preisgaps.
Zu Beginn eines ausgewählten Wochentags vergleicht sie den letzten Schlusskurs mit dem Eröffnungspreis 24 Stunden zuvor.
Wenn der Gap größer als der Parameter **Corridor** ist, wird eine Position in Richtung des Rücksetzers eröffnet:

- Gap nach oben → verkaufen.
- Gap nach unten → kaufen.

Trades verwenden feste Stop-Loss- und Take-Profit-Abstände in Preiseinheiten.
Ein Trailing-Stop mit Schritt wird angewendet, nachdem die Position in den Gewinn läuft.
Alle Positionen werden gegen Ende des Tages (22:45) geschlossen.

## Parameter
- `Corridor` – Gap-Schwellenwert.
- `StopLoss` – fester Verlustabstand.
- `TakeProfit` – festes Gewinnziel.
- `TrailingStop` – Trailing-Abstand.
- `TrailingStep` – Bewegung zum Aktualisieren des Trailing.
- `TradingDay` – Wochentag für das Eröffnen von Trades (0=Sonntag).
- `CandleType` – Zeitrahmen für Berechnungen.
