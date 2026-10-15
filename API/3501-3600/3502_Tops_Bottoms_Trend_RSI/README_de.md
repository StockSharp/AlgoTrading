# Tops Bottoms Trend RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader-Expertenberaters „Tops Bottoms Trend und RSI EA“. Es überwacht abgeschlossene Kerzen des ausgewählten Zeitrahmens, sucht innerhalb eines konfigurierbaren Lookback-Fensters nach aufkommenden Trendhochs oder -tiefs und bestätigt jede Gelegenheit mit einem Relative-Stärke-Index-Filter (RSI). Wenn die Kriterien erfüllt sind, eröffnet die Strategie eine einzelne Marktorder und weist sofort schützende Stop-Loss- und Take-Profit-Levels zu, die aus Pip-basierten Distanzen abgeleitet werden.

## Handelslogik
- **Datenquelle** – der Algorithmus abonniert den konfigurierten Kerzentyp und wertet nur fertige Kerzen aus, um die Verwendung unvollständiger Daten zu vermeiden.
- **Bottom-Erkennung (langes Setup)** – der Schlusskurs der letzten Kerze muss mindestens `BuyTrendPips` Pips unter dem Hoch der Kerze vor `BuyTrendCandles` Balken liegen. Alle Zwischentiefs müssen über dem aktuellen Schlusskurs bleiben und der Qualitätsfilter (`BuyTrendQuality`) erfordert, dass die jüngsten Höchststände nicht zu stark vom Referenzhoch abweichen. Wenn sich diese Struktur bildet und der RSI-Wert der vorherigen Kerze unter `BuyRsiThreshold` liegt, eröffnet die Strategie eine Long-Position mit einem Volumen von `BuyVolume`.
- **Top-Erkennung (kurzes Setup)** – der Schlusskurs der letzten Kerze muss mindestens `SellTrendPips` Pips über dem Tiefststand der Kerze vor `SellTrendCandles` Balken liegen. Zwischenhochs müssen unter dem aktuellen Schlusskurs bleiben, während der Qualitätsfilter (`SellTrendQuality`) die jüngsten Tiefststände nahe am Referenztief hält. Wenn der RSI-Wert der vorherigen Kerze `SellRsiThreshold` überschreitet, eröffnet die Strategie eine Short-Position mit einem Volumen von `SellVolume`.
- **Risikomanagement** – nach jedem Eintrag speichert die Strategie den Füllpreis und berechnet Pip-basierte Schutzniveaus. Stop-Loss-Offsets verwenden `BuyStopLossPips` oder `SellStopLossPips`. Take-Profit-Entfernungen werden hauptsächlich vom Stopp über `BuyTakeProfitPercentOfStop` oder `SellTakeProfitPercentOfStop` abgeleitet. Wenn der Long-Take-Profit-Prozentsatz deaktiviert ist (`0`), wird stattdessen die feste `BuyTakeProfitPips`-Distanz verwendet. Immer wenn nachfolgende Kerzen die entsprechenden Stop- oder Take-Profit-Levels berühren, wird die Position mit einer Marktorder geschlossen.
- **Positionskontrolle** – das System hält maximal eine offene Position. Neue Signale werden ignoriert, solange eine Position oder eine aktive Order besteht. Die RSI-Bestätigung basiert immer auf der vorherigen Kerze (Verschiebung um einen Balken) und spiegelt die ursprüngliche EA wider.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `BuyVolume` | Auftragsvolumen, das für Long-Positionen verwendet wird. | `0.01` |
| `BuyStopLossPips` | Stop-Loss-Distanz für Long-Trades in Pips. | `20` |
| `BuyTakeProfitPips` | Die Take-Profit-Distanz in Pips für Long-Positionen wurde korrigiert, wenn der Prozentmodus deaktiviert ist. | `5` |
| `BuyTakeProfitPercentOfStop` | Take-Profit als Prozentsatz der langen Stop-Loss-Distanz. | `100` |
| `SellVolume` | Auftragsvolumen, das für Short-Positionen verwendet wird. | `0.01` |
| `SellStopLossPips` | Stop-Loss-Distanz für Short-Trades in Pips. | `20` |
| `SellTakeProfitPercentOfStop` | Take-Profit als Prozentsatz der Short-Stop-Loss-Distanz. | `100` |
| `SellTrendCandles` | Anzahl der bei der Suche nach neuen Oberteilen überprüften Kerzen. | `10` |
| `SellTrendPips` | Mindestvorsprung über das Referenztief, der für ein Short-Setup erforderlich ist (Pips). | `20` |
| `SellTrendQuality` | Trendqualitätsfilter für kurze Setups (begrenzt auf den Bereich 1–9). | `5` |
| `BuyTrendCandles` | Anzahl der bei der Suche nach neuen Böden überprüften Kerzen. | `10` |
| `BuyTrendPips` | Minimaler Rückgang unter das Referenzhoch, der für ein Long-Setup (Pips) erforderlich ist. | `20` |
| `BuyTrendQuality` | Trendqualitätsfilter für lange Setups (begrenzt auf den Bereich 1–9). | `5` |
| `BuyRsiPeriod` | RSI Zeitraum für lange Bestätigungen verwendet. | `14` |
| `BuyRsiThreshold` | RSI überverkaufter Schwellenwert, der von oben überschritten werden muss, um Long-Einträge zu ermöglichen. | `40` |
| `SellRsiPeriod` | RSI Zeitraum, der für kurze Bestätigungen verwendet wird. | `14` |
| `SellRsiThreshold` | RSI überkaufter Schwellenwert, der von unten überschritten werden muss, um Short-Einstiege zu ermöglichen. | `60` |
| `CandleType` | Zeitrahmen der von der Strategie verarbeiteten Kerzen. | `30-minute time frame` |

## Notizen
- Pip-Abstände werden mithilfe des `PriceStep` des Wertpapiers in Preise umgerechnet. Fünfstellige und gebrochene Pip-Forex-Kurse werden auf die klassische Pip-Größe normalisiert und replizieren die Umrechnungsregeln des Originals EA.
- Da die RSI-Bestätigung die vorherige Kerze verwendet (Verschiebung = 1), benötigt die Strategie mindestens einen vollständig gebildeten RSI-Wert, bevor sie gehandelt werden kann. Die ersten Kerzen nach dem Start werden daher ignoriert.
- Die Logik hebt alle Schutzstufen auf, wenn eine Position vollständig geschlossen wird, und stellt so sicher, dass der nächste Eintrag mit neuen Risikoparametern beginnt.
