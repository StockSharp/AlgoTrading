# DigVariation-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist vom MQL5-Beispiel *DigVariation* inspiriert. Sie approximiert den Indikator mit einem einfachen gleitenden Durchschnitt (SMA) und eröffnet Trades, wenn der SMA die Richtung ändert.

## Logik
- Der SMA wird auf eingehenden Kerzen berechnet.
- Wenn die vorherigen SMA-Werte einen Aufwärtstrend zeigen und der aktuelle Wert weiter steigt, eröffnet die Strategie eine Long-Position.
- Wenn die vorherigen SMA-Werte einen Abwärtstrend zeigen und der aktuelle Wert weiter fällt, eröffnet die Strategie eine Short-Position.
- Bestehende Positionen werden bei Trendumkehr geschlossen.

## Parameter
- **Period** – SMA-Berechnungsperiode.
- **BuyOpen** – Long-Einstiege aktivieren.
- **SellOpen** – Short-Einstiege aktivieren.
- **BuyClose** – Schließen von Long-Positionen erlauben.
- **SellClose** – Schließen von Short-Positionen erlauben.
- **StopLoss** – Verlustschutzelement (wird an `StartProtection` übergeben).
- **TakeProfit** – Gewinnzielwert (wird an `StartProtection` übergeben).

## Hinweise
Dies ist eine vereinfachte Konvertierung. Es wird ein Standard-SMA anstelle des originalen benutzerdefinierten DigVariation-Indikators verwendet.
