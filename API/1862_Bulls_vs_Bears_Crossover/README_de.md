# Bulls vs Bears Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie implementiert ein Crossover-System basierend auf dem **Bulls vs Bears (BvsB)**-Indikator. Der Indikator misst den Abstand zwischen dem Hoch- und Tiefstkurs einer Kerze und einem gleitenden Durchschnitt. Wenn der bullische Abstand unter den bärischen Abstand fällt, deutet dies auf nachlassenden Aufwärtsdruck hin und die Strategie eröffnet eine Long-Position. Umgekehrt wird eine Short-Position eröffnet, wenn der bullische Abstand über den bärischen Abstand steigt. Bestehende Positionen werden beim entgegengesetzten Signal oder beim Erreichen von Gewinn- oder Verlustzielen geschlossen.

Der gleitende Durchschnittstyp und die Länge sind konfigurierbar, sodass sich die Strategie an verschiedene Märkte und Zeitrahmen anpassen lässt. Das Risikomanagement wird durch feste Stop-Loss- und Take-Profit-Niveaus in Preisschritten gesteuert.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `MaType` | Berechnungsmethode des gleitenden Durchschnitts (SMA, EMA, SMMA, WMA). |
| `MaLength` | Periode des gleitenden Durchschnitts. |
| `StopLoss` | Stop-Loss-Abstand in Preisschritten. |
| `TakeProfit` | Take-Profit-Abstand in Preisschritten. |
| `OpenLong` | Long-Positionseröffnung bei bullischem Crossover erlauben. |
| `OpenShort` | Short-Positionseröffnung bei bärischem Crossover erlauben. |
| `CloseLong` | Long-Positionsschließung bei bärischem Crossover erlauben. |
| `CloseShort` | Short-Positionsschließung bei bullischem Crossover erlauben. |
| `CandleType` | Zeitrahmen der verarbeiteten Kerzen. |

## Funktionsweise

1. Die angegebene Kerzenreihe abonnieren und einen gleitenden Durchschnitt berechnen.
2. Für jede abgeschlossene Kerze bullischen und bärischen Abstand berechnen:
   - **Bull** = `(HighPrice - MA) / PriceStep`
   - **Bear** = `(MA - LowPrice) / PriceStep`
3. Kreuzungen zwischen Bull- und Bear-Werten erkennen.
4. Positionen gemäß Kreuzungsrichtung und aktivierten Optionen öffnen oder schließen.
5. Risiko mit den konfigurierten Stop-Loss- und Take-Profit-Niveaus verwalten.

Dieser einfache, aber flexible Ansatz kann auf viele Instrumente angewendet werden, um das Gleichgewicht zwischen bullischen und bärischen Kräften zu messen.
