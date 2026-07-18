# Bollinger Band-Strategie für ausstehende Stopps
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Dieses Beispiel wandelt den ursprünglichen Expertenberater MQL „Bb_0_1“ in den hochrangigen StockSharp API um. Die Strategie überwacht ein Kerzenabonnement und verwendet Bollinger-Bänder, um den aktuellen Preis zu klammern. Wenn der Markt zwischen dem oberen und dem unteren Band liegt, platziert der Algorithmus dreischichtige Kauf-Stopp-Orders über dem Preis und dreischichtige Verkaufs-Stopp-Orders unter dem Preis. Jede Ebene ist mit individuellen Take-Profit-Abständen konfiguriert und nutzt dabei die gleiche Stopp-Referenz, die vom gegenüberliegenden Band übernommen wird.

## Handelslogik
- Abonnieren Sie den konfigurierten Zeitrahmen und berechnen Sie Bollinger Bänder mit dem gewünschten Zeitraum und der gewünschten Abweichung.
- Geben Sie innerhalb des Handelsfensters (`StartHour` < Stunde < `EndHour`) und während der Preis zwischen den Bändern bleibt, ausstehende Aufträge auf:
  - Drei Kaufstopps auf dem aktuellen oberen Bandniveau mit um `FirstTakeProfit`, `SecondTakeProfit` und `ThirdTakeProfit` Preisschritten über dem Einstiegspunkt verschobenen Take-Profits.
  - Drei Verkaufsstopps auf dem aktuellen unteren Bandniveau mit gespiegelten Take-Profits unterhalb des Einstiegs.
  - Alle Einträge übernehmen das entgegengesetzte Band als anfänglichen Schutzstopp.
- Ausstehende Aufträge werden automatisch neu registriert, wenn sich die Bänder dem Preis nähern, sodass die Aufträge den Indikatorumschlägen folgen.
- Sobald eine Stop-Order ausgeführt wird, registriert die Strategie explizite Stop-Loss- und Take-Profit-Orders für das ausgeführte Volumen.
- Der Trailing-Schutz ist optional: `UseBandTrailingStop` wählt das entgegengesetzte Band für das Trailing aus, andernfalls wird das mittlere Band (EMA) verwendet. Stopps fallen nur dann nach, wenn der Schlusskurs über den Einstiegspreis hinausgeht und der Indikatorwert ein besseres Niveau bietet.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen, der für die Bollinger-Bandberechnungen verwendet wird. |
| `BandPeriod` | Anzahl der von den Bands verwendeten Kerzen. |
| `BandDeviation` | Standardabweichungsmultiplikator für die Bänder. |
| `Volume` | Volumen jeder ausstehenden Ebene. |
| `StartHour` / `EndHour` | Stündliches Handelsfenster (exklusive Grenzen). |
| `FirstTakeProfit`, `SecondTakeProfit`, `ThirdTakeProfit` | Take-Profit-Abstände, ausgedrückt in Preisschritten für jede Ebene. |
| `UseBandTrailingStop` | Wählen Sie die nachgestellte Referenz aus: gegenüberliegendes Band (`true`) oder Bollinger Mittellinie (`false`). |

## Hinweise zur Implementierung
- Das Bestellvolumen spiegelt den ursprünglichen Expert Advisor wider, indem eine statische Größe (`Volume`) verwendet wird. Die risikobasierte Positionsgrößenbestimmung aus dem Code MQL ist nicht implementiert, da die Beispielumgebung StockSharp keinen Kontoverlauf bereitstellt.
- Indikatorverschiebungsparameter aus dem MQL-Skript werden nicht angezeigt, da die hohe Ebene API bereits ausgerichtete Werte für die aktuelle Kerze liefert.
- Schutzaufträge sind normale Stop- und Limit-Aufträge, die immer dann aktualisiert werden, wenn die bandbasierten Trailing-Bedingungen das Stop-Level verbessern.
