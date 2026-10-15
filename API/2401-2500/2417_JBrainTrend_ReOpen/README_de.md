# JBrainTrend ReOpen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Implementierung, inspiriert vom MQL5-Beispiel "JBrainTrend1Stop_ReOpen".  
Sie verwendet den Stochastik-Oszillator zur Bestimmung von Überkauf- und Überverkaufbedingungen und unterstützt Pyramidisierung durch erneutes Öffnen von Positionen, wenn der Kurs um einen festgelegten Schritt vorgerückt ist.

## Logik
- Kerzen des ausgewählten Zeitrahmens abonnieren.
- Stochastik-Oszillator (%K und %D) berechnen.
- Long einsteigen, wenn %K unter 20 fällt, und Short, wenn %K über 80 steigt.
- Positionen werden geschlossen, wenn das entgegengesetzte Extrem erreicht wird.
- Nach einem Einstieg werden weitere Positionen hinzugefügt, wenn der Kurs `PriceStep` in die Richtung des Trades läuft, bis zu `MaxPositions`.
- Stop-Loss und Take-Profit werden in absoluten Preiseinheiten angewendet.

## Parameter
- `StochPeriod` – Hauptperiode des Stochastik-Oszillators.
- `KPeriod` / `DPeriod` – Glättungsperioden für die %K- und %D-Linien.
- `CandleType` – Zeitrahmen für die Analyse.
- `StopLoss` – Stop-Loss-Abstand in Preiseinheiten.
- `TakeProfit` – Take-Profit-Abstand in Preiseinheiten.
- `PriceStep` – Preisbewegung zum Wiederöffnen einer Position.
- `MaxPositions` – maximale Anzahl von Einstiegen in eine Richtung.
- `BuyEnabled` / `SellEnabled` – Long-/Short-Trades aktivieren oder deaktivieren.

## Hinweise
Das ursprüngliche MQL5-Skript verwendete einen benutzerdefinierten Indikator namens *JBrainTrend1Stop*.  
Dieser C#-Port approximiert das Handelskonzept mit integrierten Indikatoren von StockSharp für eine einfachere Integration.
