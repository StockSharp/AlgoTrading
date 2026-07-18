# NRTR Extr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert den **Nick Rypock Trailing Reverse** (NRTR)-Algorithmus mit zusätzlichen Signalpfeilen. Es handelt sich um eine Konvertierung des originalen MQL5-Beispiels „Exp_NRTR_extr" zur StockSharp High-Level-API.

## Funktionsweise

- Der benutzerdefinierte `NrtrExtrIndicator` berechnet eine durchschnittliche Spanne über einen konfigurierbaren Zeitraum und zeichnet ein Trailing-Level, das dem Preis folgt.
- Wenn der Preis über dieses Level hinaus umkehrt, wechselt der Indikator die Richtung und sendet ein Kauf- oder Verkaufssignal.
- Die Strategie eröffnet bei einem Kaufsignal eine Long-Position und bei einem Verkaufssignal eine Short-Position.
- Bestehende Positionen werden bei entgegengesetztem Signal oder beim Erreichen der definierten Stop-Loss- oder Take-Profit-Levels geschlossen.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `Period` | Anzahl der Kerzen für die Berechnung der durchschnittlichen Spanne. |
| `Digits Shift` | Zusätzliche Präzisionsanpassung des Spannenfaktors. |
| `Stop Loss` | Schutz-Stop in Preispunkten. |
| `Take Profit` | Gewinnziel in Preispunkten. |
| `Enable Buy Open` / `Enable Sell Open` | Öffnen von Long- oder Short-Positionen erlauben. |
| `Enable Buy Close` / `Enable Sell Close` | Schließen bestehender Positionen bei entgegengesetzten Signalen erlauben. |
| `Candle Type` | Zeitrahmen der für den Indikator verwendeten Kerzen. |

## Hinweise

Der Indikator basiert auf dem Average True Range zur Schätzung der Marktvolatilität. Zur Visualisierung zeichnet die Strategie automatisch Kerzen und ausgeführte Trades im Chartbereich.

