# Spazm-Volatilitäts-Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- Konvertierung des MetaTrader 4 Expert Advisors **Spazm (8683)** zum StockSharp High Level API.
- Handeln Sie adaptive Ausbrüche, indem Sie den letzten Schlusskurs mit Umschlägen in Volatilitätsgröße um das letzte Swing-Hoch und Swing-Tief vergleichen.
- Behält optionale Diagrammanmerkungen bei, die aufeinanderfolgende bullische und bärische Pivots verbinden, genau wie die ursprüngliche MQL-Visualisierung.

## Datenvorbereitung
1. Die Strategie abonniert die Kerzenserie, die durch den Parameter `CandleType` für das aktive Wertpapier angegeben wird.
2. Jede fertige Kerze liefert die Rohbereichsprobe, die zur Volatilitätsschätzung verwendet wird:
   - Standardmäßig beträgt der Bereich `High - Low`.
   - Wenn `UseOpenCloseRange` aktiviert ist, wird stattdessen die absolute Körpergröße `|Open - Close|` verwendet.
3. Das Bereichsbeispiel wird mithilfe des Instruments `PriceStep` in Preisschritte umgewandelt, sodass die Logik über alle Symbole hinweg invariant bleibt.
4. Der durch `UseWeightedVolatility` definierte Indikator verarbeitet die Folge von Bereichsproben:
   - Deaktiviert → einfacher gleitender Durchschnitt mit der Länge `VolatilityPeriod`.
   - Aktiviert → linear gewichteter gleitender Durchschnitt (mehr Gewicht für aktuelle Kerzen).
5. Der geglättete Bereich (ausgedrückt in Schritten) wird mit `VolatilityMultiplier` multipliziert und schließlich auf Preiseinheiten zurückskaliert. Der resultierende Wert ist die adaptive Breakout-Schwelle, die auf beide Seiten des Marktes angewendet wird.
6. Während der Aufwärmphase zeichnet die Strategie auch die jüngsten Extremhochs und Extremtiefs mit ihren Zeitstempeln auf. Sobald `VolatilityPeriod * 3` Kerzen verarbeitet sind, bestimmt das relative Timing dieser Extreme die anfängliche Trendrichtung.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Volume` | `1` | Das Auftragsvolumen wird immer dann gesendet, wenn die Strategie eine Position eröffnet oder umkehrt. |
| `VolatilityMultiplier` | `5` | Auf die durchschnittliche Volatilität angewendeter Multiplikator, um die Ausbruchsdistanz zu ermitteln. |
| `VolatilityPeriod` | `24` | Anzahl der Kerzen, die sowohl für den Volatilitätsschätzer als auch für das Setzen der anfänglichen Swing-Extreme verwendet werden. |
| `UseWeightedVolatility` | `false` | Schaltet den Volatilitätsschätzer von einem einfachen auf einen linear gewichteten gleitenden Durchschnitt um. |
| `UseOpenCloseRange` | `false` | Verwendet bei der Messung der Volatilität die absolute Eröffnungs-/Schlussbewegung anstelle des Hoch-Tief-Bereichs. |
| `StopLossMultiplier` | `0` | Auf den Ausbruchsschwellenwert angewendeter Multiplikator, um einen Schutzstoppabstand zu berechnen. Es werden mindestens drei Preisstufen durchgesetzt. Auf `0` setzen, um Stopps zu deaktivieren. |
| `DrawSwingLines` | `true` | Wenn die Strategie aktiviert ist, zeichnet sie eine Linie zwischen den neuesten bullischen und bärischen Pivots und ahmt die MQL-Objekte nach. |
| `CandleType` | `4 hour time frame` | Kerzentyp (Zeitrahmen oder anderer Datentyp), der die Berechnungen speist. |

## Handelslogik
1. **Initialisierung**
   - Während die ersten `VolatilityPeriod * 3`-Kerzen verarbeitet werden, aktualisiert die Strategie `_highestPrice`, `_lowestPrice`, `_highestTime` und `_lowestTime`, um die neuesten Extreme zu erfassen.
   - Nachdem genügend Kerzen eingetroffen sind, definiert das neuere der beiden Extreme den anfänglichen Trend: Wenn das letzte Tief neuer als das letzte Hoch ist, beginnt die Strategie im bullischen Modus, andernfalls beginnt sie im bärischen Modus.
   - Die Extremwerte werden auch als erstes Swing-Ankerpaar gespeichert, sodass Diagrammlinien unmittelbar nach dem Aufwärmen gezeichnet werden können.
2. **Volatilitätsverfolgung**
   - Jede fertige Kerze verschiebt ihren Bereich in den ausgewählten gleitenden Durchschnitt, um den adaptiven Schwellenwert zu erzeugen.
   - Der Schwellenwert liegt immer bei mindestens einem Preisschritt, um Null-Distanz-Umschläge zu vermeiden.
3. **Schaukelwartung**
   - Bei jeder Kerze aktualisiert der Algorithmus das gespeicherte Swing-Hoch und Swing-Tief, sobald ein neues absolutes Hoch oder Tief gedruckt wird.
   - Wenn sich der Trend umkehrt, wird das relevante Extrem als Pivot aufgezeichnet und, sofern die Diagrammerstellung aktiviert ist, durch eine Linie mit dem gegenüberliegenden Pivot verbunden.
4. **Breakout-Regeln**
   - Bullisches Regime (`_isTrendUp == true`): Ein Schlusskurs unter `_highestPrice - threshold` löst eine Umkehr zu Short aus. Die Ordergröße beträgt `Volume + |Position|`, sodass das bestehende Engagement abgeflacht und in einem Aufruf eine neue Short-Position eröffnet wird.
   - Bärisches Regime (`_isTrendUp == false`): Ein Schlusskurs über `_lowestPrice + threshold` spiegelt die Logik wider und kehrt sich in einen Long-Kurs um.
5. **Management stoppen**
   - Wenn `StopLossMultiplier` größer als Null ist, wird der Einstiegspreis um `threshold * StopLossMultiplier` ausgeglichen (begrenzt auf mindestens drei Preisschritte), um ein synthetisches Stop-Level abzuleiten.
   - Wenn eine Kerze mit ihrem Tief den Long-Stop oder mit ihrem Hoch den Short-Stop durchbricht, wird die Position durch eine Marktorder abgeflacht.
6. **Infrastruktur**
   - `StartProtection()` aktiviert integrierte StockSharp-Sicherheitsmechanismen, sobald die Strategie gestartet wird.
   - Alle Aktionen werden durch fertige Kerzen gesteuert, um den balkenweisen Neuberechnungszyklus des ursprünglichen Expertenberaters nachzuahmen.

## Unterschiede zur MQL-Version
- Der MetaTrader-Experte führt bei jedem Tick eine Neuberechnung durch, während dieser Port mit abgeschlossenen Kerzen arbeitet, da Kerzenabonnements die idiomatische Datenquelle auf der hohen Ebene API sind.
- Brokerspezifische Einschränkungen wie `MODE_STOPLEVEL` sind nicht verfügbar; Stattdessen wird der Stop-Offset durch drei Preisschritte begrenzt, um einen konservativen Fallback zu ermöglichen.
- Aufträge werden umgekehrt, indem die Schluss- und Eröffnungsbeträge in einem einzigen `BuyMarket`/`SellMarket`-Aufruf zusammengefasst werden, anstatt über bestehende Positionen zu iterieren.
- Die Visualisierung basiert auf StockSharp-Diagrammprimitiven (`DrawLine`) anstelle von Plattformobjekten, die Anordnung der Pivot-zu-Pivot-Linien entspricht jedoch der ursprünglichen Indikatorausgabe.

## Hinweise zur Verwendung
- Stellen Sie sicher, dass die ausgewählte Sicherheit einen gültigen `PriceStep` bereitstellt. Wenn der Code fehlt, lautet er standardmäßig `1`, was für bestimmte Instrumente möglicherweise angepasst werden muss.
- Da die Strategie von abgeschlossenen Kerzen abhängt, verringern extrem kleine Zeitrahmen die Zuverlässigkeit der Volatilitätsschätzung. Erwägen Sie, `CandleType` an den ursprünglich von EA verwendeten Zeitrahmen anzupassen (standardmäßig H4).
- Stopps sind optional. Wenn Sie `StopLossMultiplier` auf Null belassen, wird das unbegrenzte Risikomanagement aus dem Skript MQL repliziert.
- Der Algorithmus ist von Natur aus trendorientiert und schreibt keine Take-Profit-Ziele vor; Ausstiege erfolgen nur durch Regimeumkehr oder Stop-Loss-Aktivierung.
