# Mean-Reversion-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Mean Reversion-Strategie ist eine direkte Portierung des MetaTrader-Expertenberaters *Mean reversion.mq4*. Die StockSharp-Version behält die ursprüngliche Handelsidee bei: Kauf nach einer längeren Serie fallender Schlusskurse und Verkauf nach einem ähnlichen Aufwärtstrend. Einträge werden durch Trendausrichtung mit zwei linear gewichteten gleitenden Durchschnitten, Momentumstärke in einem höheren Zeitrahmen und einem monatlichen MACD-Filter bestätigt.

Sobald die Strategie in Position ist, werden die Money-Management-Regeln der MQL-Version neu erstellt: konfigurierbarer Stop-Loss und Take-Profit in Pips, optionale Break-Even-Verschiebung und ein Trailing-Stop, der Gewinne sichert, wenn sich der Markt zu Gunsten des Handels bewegt.

## Handelslogik
1. **Signalzeitrahmen** – die Strategie arbeitet mit der ausgewählten Kerzenserie (Standard 15 Minuten).
2. **Erschöpfungserkennung** – es werden die letzten `BarsToCount` Schließungen erfasst. Bei einem Long-Setup muss der letzte Schlusskurs niedriger sein als jeder der vorherigen Schlusskurse, was einen Ausverkauf signalisiert. Ein kurzes Setup erfordert die gegenteilige Bedingung.
3. **Trendfilter** – Der schnelle LWMA (Länge `FastMaLength`) muss für Long-Positionen über dem langsamen LWMA (`SlowMaLength`) und für Short-Positionen darunter liegen.
4. **Momentum-Filter** – der Momentum-Indikator (Periode `MomentumLength`) wird auf dem MetaTrader-ähnlichen höheren Zeitrahmen berechnet (M15 → H1, H1 → D1 usw.). Mindestens einer der letzten drei Impulswerte muss um mehr als `MomentumThreshold` von 100 abweichen.
5. **MACD-Bestätigung** – bei einem monatlichen MACD (26.12.9) muss die Hauptlinie für Long-Positionen über der Signallinie und für Short-Positionen darunter liegen.

Wenn alle Bedingungen erfüllt sind, eröffnet die Strategie eine Position mit `OrderVolume`. Gegensätzliche Trades glätten die aktuelle Position, bevor sie sich umkehren.

## Positionsmanagement
- **Stop-Loss und Take-Profit** – konfiguriert in Pips über `StopLossPips` und `TakeProfitPips`.
- **Break-even** – wenn aktiviert, wird der Stop auf den Einstiegspreis plus `BreakEvenOffsetPips` verschoben, nachdem der Preis um `BreakEvenTriggerPips` gestiegen ist.
- **Trailing Stop** – wenn `EnableTrailing` wahr ist und der nicht realisierte Gewinn `TrailingStopPips` übersteigt, folgt der Stop dem Preis mit Schritt `TrailingStepPips`.

Bei allen Preisumrechnungen wird die Pip-Größe des Instruments verwendet, um das Verhalten von MetaTrader abzugleichen.

## Parameter
| Name | Beschreibung | Standard |
|------|-------------|---------|
| `OrderVolume` | Ordergröße, die für Markteintritte verwendet wird. | `1` |
| `CandleType` | Primäre Kerzenserie, die für Signale verwendet wird. | `M15` |
| `BarsToCount` | Anzahl der vorherigen Abschlüsse, die auf Erschöpfung überprüft wurden. | `10` |
| `FastMaLength` | Schnelle LWMA-Periode. | `6` |
| `SlowMaLength` | Langsame LWMA-Periode. | `85` |
| `MomentumLength` | Momentum-Periode im höheren Zeitrahmen. | `14` |
| `MomentumThreshold` | Minimale absolute Abweichung von 100 zur Impulsbestätigung. | `0.3` |
| `StopLossPips` | Stop-Loss-Distanz in Pips. | `20` |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. | `50` |
| `UseBreakEven` | Aktivieren Sie die Stop-Verlagerung zum Break-Even. | `false` |
| `BreakEvenTriggerPips` | Der Gewinn in Pips ist erforderlich, bevor der Stopp verschoben wird. | `30` |
| `BreakEvenOffsetPips` | Beim Erreichen der Gewinnschwelle werden zusätzliche Pips hinzugefügt. | `30` |
| `EnableTrailing` | Aktivieren Sie die Trailing-Stop-Verwaltung. | `true` |
| `TrailingStopPips` | Der Gewinn in Pips ist erforderlich, um mit dem Trailing zu beginnen. | `40` |
| `TrailingStepPips` | Vom Trailing Stop aufrechterhaltener Abstand. | `40` |

## Notizen
- Der höhere Zeitrahmen für den Impuls folgt MetaTrader Schritten: M1→M15, M5→M30, M15→H1, M30→H4, H1→D1, H4→W1, D1→MN1, W1→MN1.
- Die MACD-Bestätigung verwendet immer den monatlichen Zeitrahmen (MN1).
- Die Strategie erwartet zeitrahmenbasierte Kerzentypen; Tick- oder Range-Kerzen werden nicht unterstützt.
