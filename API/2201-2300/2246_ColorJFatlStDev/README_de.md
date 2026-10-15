# ColorJFatl StDev-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Übersetzung des **ColorJFatl_StDev**-Expertenberaters von MQL5 in die StockSharp API. Sie kombiniert den Jurik Moving Average (JMA) mit Standardabweichungsbändern, um Handelssignale zu generieren.

## Strategielogik

1. Berechnung des JMA auf Schlusskursen.
2. Berechnung der Standardabweichung über einen konfigurierbaren Zeitraum.
3. Aufbau von zwei Sätzen dynamischer Bänder mit den Multiplikatoren `K1` und `K2`:
   - `upper1 = JMA + K1 * StdDev`
   - `upper2 = JMA + K2 * StdDev`
   - `lower1 = JMA - K1 * StdDev`
   - `lower2 = JMA - K2 * StdDev`
4. Abhängig vom ausgewählten Signalmodus öffnet oder schließt die Strategie Positionen:
   - **Point** – wird ausgelöst, wenn der Preis die Bänder kreuzt.
   - **Direct** – verwendet Wendepunkte der JMA-Linie.
   - **Without** – deaktiviert das entsprechende Signal.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `CandleTimeFrame` | Zeitrahmen für Kerzendaten. |
| `JmaLength` | Periode des Jurik Moving Average. |
| `JmaPhase` | Phase für die JMA-Berechnung. |
| `StdPeriod` | Periode für die Standardabweichung. |
| `K1` | Erster Abweichungsmultiplikator. |
| `K2` | Zweiter Abweichungsmultiplikator. |
| `BuyOpenMode` | Modus zum Öffnen von Long-Positionen. |
| `SellOpenMode` | Modus zum Öffnen von Short-Positionen. |
| `BuyCloseMode` | Modus zum Schließen von Long-Positionen. |
| `SellCloseMode` | Modus zum Schließen von Short-Positionen. |

## Verwendung

Die Strategie abonniert Kerzen des angegebenen Zeitrahmens, verarbeitet JMA- und Standardabweichungswerte und übermittelt automatisch Marktorders basierend auf den definierten Modi.

Diese Implementierung konzentriert sich auf Klarheit und kann als Ausgangspunkt für weitere Verbesserungen oder benutzerdefiniertes Risikomanagement dienen.
