# GLFX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

MetaTrader 4 Fachberater **GLFX** für StockSharps High-Level-API neu geschrieben. Der Port behält die ursprüngliche Idee bei, Bestätigungen mit höherem Zeitrahmen mit strengen Geldverwaltungs-Gates zu kombinieren und gleichzeitig die riesige Sammlung selten verwendeter Filter zu entfernen, die von externen Indikatoren abhingen.

## Handelslogik

1. Die Strategie arbeitet mit einem primären Zeitrahmen (Standard **M15**) und erstellt optional einen Bestätigungszeitrahmen, indem sie die klassische MetaTrader-Leiter (`M15 → M30 → H1 → H4 → D1 → W1 → MN`) hinaufsteigt.
2. Ein höherer Zeitrahmen **RSI** (Standardzeitraum 57) verfolgt, ob die Dynamik steigt oder fällt. Eine Kaufbestätigung erscheint, wenn RSI steigt, aber unter der konfigurierten Überkauft-Obergrenze bleibt. Eine Verkaufsbestätigung erfordert, dass RSI nach unten tickt und dabei über der überverkauften Untergrenze bleibt.
3. Ein **einfacher gleitender Durchschnitt** mit einem höheren Zeitrahmen (Standardzeitraum 60) erkennt, ob sich der Preis vom Mittelwert entfernt. Für eine zinsbullische Bestätigung muss der MA steigen und gleichzeitig über dem aktuellen Schlusskurs bleiben (der Preis zieht sich zurück in einen Aufwärtstrend). Eine bärische Bestätigung spiegelt diese Logik wider.
4. Jeder aktivierte Filter trägt `+1` für eine bullische oder `-1` für eine bärische Stimmung bei. Die Gesamtzahl muss die Anzahl der aktiven Filter erreichen, um als gültiges Signal zu gelten. Zähler merken sich, wie viele aufeinanderfolgende Signale voller Stärke aufgetreten sind (`SignalsRepeat`). Wenn die kombinierte Stärke unter den Schwellenwert fällt und `SignalsReset` aktiviert ist, werden die Zähler zurückgesetzt.
5. Wenn die Strategie flach ist und die Long/Short-Einstiegsschalter dies zulassen, löst der nächste abgeschlossene Zähler eine Marktorder mit dem konfigurierten `Volume` aus. Statische Stop-Loss- und Take-Profit-Level werden mithilfe der Tick-Größe des Instruments von Pips in Preis-Offsets umgewandelt.
6. Wenn eine Position bereits offen ist, können starke gegenläufige Signale sie vorzeitig schließen (`AllowLongExit` / `AllowShortExit`). Andernfalls stützen sich Exits auf den von `StartProtection()` verwalteten Stopp oder das Ziel.

Der Port reproduziert **nicht** die optionalen Funktionen Quantum, Twitter Sentiment, Open-Bar-Korrelation, Set-Testing oder erweiterte Geldverwaltungsleitern des Originals EA. Für diese Module waren zusätzliche benutzerdefinierte Indikatoren oder Broker-Status erforderlich, die in StockSharp nicht vorhanden sind.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | M15 | Arbeitszeitrahmen für die Preisbewertung. |
| `HigherTimeFrameShift` | 1 | Anzahl der MT4-Schritte, die zum Aufbau des Bestätigungszeitrahmens verwendet wurden. `0` behält den aktuellen Zeitrahmen bei. |
| `UseRsiSignal` | wahr | Aktivieren Sie die Bestätigung für einen höheren Zeitrahmen RSI. |
| `RsiPeriod` | 57 | Zeitraum der Bestätigung RSI. |
| `RsiUpperThreshold` | 65 | Deaktivieren Sie neue Longs, sobald RSI diesen Wert überschreitet. |
| `RsiLowerThreshold` | 25 | Deaktivieren Sie neue Shorts, sobald RSI unter diesen Wert fällt. |
| `UseMaSignal` | wahr | Aktivieren Sie die Bestätigung des gleitenden Durchschnitts für einen höheren Zeitrahmen. |
| `MaPeriod` | 60 | Zeitraum des gleitenden Bestätigungsdurchschnitts. |
| `SignalsRepeat` | 1 | Anzahl der aufeinanderfolgenden Signale voller Stärke, die vor der Eröffnung eines Handels erforderlich sind. |
| `SignalsReset` | wahr | Setzen Sie die Zähler zurück, wenn das kombinierte Signal an Dynamik verliert. |
| `TakeProfitPips` | 308 | Take-Profit-Distanz, ausgedrückt in Pips. Zum Deaktivieren auf `0` setzen. |
| `StopLossPips` | 290 | Stop-Loss-Distanz, ausgedrückt in Pips. Zum Deaktivieren auf `0` setzen. |
| `Volume` | 0,1 | Auftragsgröße für neue Positionen (Lots). |
| `AllowLongEntry` / `AllowShortEntry` | wahr | Berechtigungsschalter zum Öffnen von Long- oder Short-Trades. |
| `AllowLongExit` / `AllowShortExit` | wahr | Ermöglicht das automatische Schließen bestehender Belichtungen bei entgegengesetzten Signalen. |

## Nutzungshinweise

- Wählen Sie Instrumente mit einer zuverlässigen Tick-Größe, damit die Pip-Umrechnung genau bleibt. Forex-Paare mit drei oder fünf Dezimalstellen werden automatisch MetaTrader „Punkten“ zugeordnet, indem der Preisschritt mit zehn multipliziert wird.
- Setzen Sie `HigherTimeFrameShift` auf `0`, wenn Sie alles im gleichen Zeitrahmen ausführen möchten. In diesem Fall werden die Indikatoren vom primären Kerzenstrom gespeist, um doppelte Abonnements zu vermeiden.
- Wenn Sie das alte Verhalten benötigen, Trades unabhängig von entgegengesetzten Signalen offen zu halten, deaktivieren Sie das entsprechende Flag `Allow*Exit`.
- Auf die Skalierung des Geldmanagements (`MMC_*`-Einstellungen), nachgestellte Module und exotische Exit-Filter aus dem ursprünglichen Skript wurde bewusst verzichtet. Implementieren Sie sie bei Bedarf auf diesem sauberen Kern.

## Unterschiede zum Original EA

| Funktionsgruppe | MetaTrader EA | StockSharp-Port |
|---------------|---------------|-----------------|
| Bestätigungsfilter | RSI, MA, optional Quantum, TSI, Mehrwährungskorrelation | Nur RSI und MA (Kernverhalten) |
| Eingangstor | Signalwiederholung plus zeitliche Filter | Signalwiederholung plus optionaler Reset |
| Risikokontrolle | Statisches TP/SL mit großer nachgestellter Modulbibliothek | Statisches TP/SL über `StartProtection()` |
| Geldmanagement | Inkrementelle Losskalierung und Verlustleitern | Fester Lautstärkeparameter |
| Externe Abhängigkeiten | Benutzerdefinierte Indikatoren (`Quantum`, `TSI`, dateibasiertes Laden von Sätzen) | Keine |

Das Ergebnis ist eine kompakte, wartbare Strategie, die das erkennbare GLFX-Verhalten beibehält – Warten auf Trendbestätigung auf einem langsameren Chart und Einstieg erst nach wiederholter Zustimmung – und sich gleichzeitig mithilfe des StockSharp-Frameworks leicht erweitern lässt.
