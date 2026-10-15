# Graal-Fraktalkanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Graal Fractal Channel Strategy** ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters „Graal-003“. Der Algorithmus beobachtet Fraktalmuster mit fünf Kerzen und bestätigt Ausbrüche mithilfe adaptiver Preiskanäle. Wenn ein gültiges bullisches oder bärisches Fraktal erscheint, wertet die Strategie mehrere Filter aus (Fraktaltunnel, Schlusspreishülle und optionale Unterdrückung des flachen Marktes), bevor sie in die Ausbruchsrichtung eintritt. Ein optionales Williams %R-Overlay repliziert die manuelle Exit-Logik des ursprünglichen Roboters, während Hedge-Stop-Orders inszeniert werden können, um den Gegentrendschutz des EA zu emulieren.

## Datenfluss und Indikatoren
* Abonniert die konfigurierten `CandleType` (standardmäßig stündliche Kerzen).
* Erstellt eine fortlaufende Warteschlange der letzten `ChannelPeriod`-Kerzen, um einen Donchian-ähnlichen Schlusskurskanal zu berechnen, der für flache Filter und Orientierungsprüfungen verwendet wird.
* Erkennt Fraktalhochs und -tiefs mit fünf Balken direkt aus dem Kerzenstrom.
* Füttert den integrierten `WilliamsPercentRange`-Indikator, um optionale Ausgangssignale zu überwachen.

## Handelsablauf
1. **Fraktale Erkennung** – die Strategie verfolgt fünf aufeinanderfolgende fertige Kerzen. Wenn das Hoch/Tief des mittleren Balkens im Vergleich zu seinen beiden Vorgängern und zwei Followern das Extrem ist, registriert es ein oberes oder unteres Fraktal und markiert ein ausstehendes kurzes oder langes Signal.
2. **Signalalterung** – jede neue Kerze erhöht das fraktale Alter. Wenn `SignalAgeLimit` Balken ohne Ausführung vergehen, verfällt das ausstehende Signal.
3. **Kanalauswertung** – der Rolling-Close-Kanal stellt drei Filter zur Verfügung:
   - *Fraktaltunnel*: Wenn `UseFractalChannel` aktiviert ist, muss der Schlusskurs innerhalb eines Prozentsatzes des Abstands zwischen dem letzten Fraktalhoch und -tief (`DepthPercent`) bleiben.
   - *Hohe/Niedrige Ausrichtung*: Bei `UseHighLowChannel` darf der Abschluss nur einen begrenzten Teil des Umschlags durchdringen (`OrientationPercent`).
   - *Flat Blocking*: Wenn `AllowFlatTrading` deaktiviert ist, werden Trades ausgesetzt, solange die Kanalbreite unter `FlatThresholdPips` bleibt.
4. **Auftragsausführung** – Sobald die Filter bestanden wurden, normalisiert die Strategie den gewünschten `OrderVolume` anhand der Instrumentenbeschränkungen und sendet einen Marktauftrag in fraktaler Richtung.
5. **Hedge-Stopps** – wenn `UseCounterOrders` aktiv ist, platziert der Algorithmus die entgegengesetzte Stop-Order zum Fraktalpreis plus/minus `OffsetPips` und spiegelt damit die Gegentrend-Stufe von EA wider.
6. **Williams wird beendet** – wenn `UseWilliamsExit` aktiviert ist, schließt der letzte Williams %R-Wert Long-Positionen, wenn er über `-WilliamsThreshold` steigt, und Short-Positionen, wenn er unter `-100 + WilliamsThreshold` fällt.

Stop-Loss- und Take-Profit-Distanzen sind optional. Immer wenn `StopLossPips` oder `TakeProfitPips` positiv ist, wandelt die Strategie den Pip-Abstand in einen absoluten Preisversatz unter Verwendung der Tick-Größe des Instruments um (mit der 3/5-stelligen Anpassung von EA) und delegiert die Verwaltung der Schutzorder an `StartProtection`.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `OrderVolume` | `0.1` | Basis-Market-Order-Größe vor der Normalisierung gegenüber den Instrumentenlimits. |
| `StopLossPips` | `500` | Schutzstoppabstand in Pips. In Preis umgerechnet und über `StartProtection` angewendet. |
| `TakeProfitPips` | `500` | Nehmen Sie die Gewinnentfernung in Pips. In Preis umgerechnet und über `StartProtection` angewendet. |
| `OffsetPips` | `5` | Zusätzlicher Abstand, der bei der Inszenierung von Stop-Orders gegen den Trend verwendet wird. |
| `ChannelPeriod` | `14` | Anzahl der zuletzt für den Schlusskurskanal gespeicherten Kerzen. |
| `UseFractalChannel` | `false` | Erfordert, dass der Preis vor Eintritten innerhalb des inneren fraktalen Korridors bleibt. |
| `DepthPercent` | `25` | Prozentsatz des fraktalen Bereichs, der den inneren Korridor definiert. |
| `UseHighLowChannel` | `false` | Aktiviert den Close-Channel-Orientierungsfilter im Donchian-Stil. |
| `OrientationPercent` | `20` | Zulässiges Eindringen in den nahen Kanal, wenn `UseHighLowChannel` wahr ist. |
| `AllowFlatTrading` | `true` | Ermöglicht den Handel auch dann, wenn der Markt entsprechend der Breite des engen Kanals flach ist. |
| `FlatThresholdPips` | `20` | Erforderliche Mindestkanalbreite (in Pips), wenn der Flat-Trading deaktiviert ist. |
| `UseWilliamsExit` | `false` | Aktiviert Williams %R-basierte Exit-Regeln. |
| `WilliamsPeriod` | `14` | Rückblickzeitraum für den Indikator Williams %R. |
| `WilliamsThreshold` | `30` | Empfindlichkeitsschwelle (Prozentpunkte) für Williams %R Exits. |
| `UseCounterOrders` | `false` | Platziert nach einem Markteintritt die entgegengesetzte Stop-Order. |
| `SinglePosition` | `false` | Blockiert zusätzliche Eingaben in die gleiche Richtung, während eine Position offen ist. |
| `SignalAgeLimit` | `3` | Maximale Anzahl neuer Balken, während der ein Fraktalsignal gültig bleibt. |
| `CandleType` | `H1` | Für die Analyse verwendete Kerzendatenreihe (standardmäßig ein einstündiger Zeitrahmen). |

## Nutzungshinweise
* Die Strategie erwartet Instrumente mit einem gültigen `PriceStep`, `MinVolume` und `VolumeStep`, damit die Volumennormalisierung und die Pip-Umrechnung korrekt funktionieren.
* Gegentrend-Orders werden automatisch storniert, wenn die Position geschlossen wird, die Strategie stoppt oder die Funktion deaktiviert wird.
* Williams %R-Exits fungieren als Sicherheitsnetz und können Positionen schließen, selbst wenn das ursprüngliche Fraktalsignal noch aktiv ist.
* Der Algorithmus setzt den gesamten zwischengespeicherten Status (Fraktalpuffer, Williams-Verlauf, bereitgestellte Bestellungen) zurück, wenn `OnReseted` ausgelöst wird.

## Unterschiede zur MetaTrader-Version
* Die StockSharp-Implementierung verwendet `SubscribeCandles().Bind(...)`-Abonnements auf hoher Ebene anstelle manueller Indikatorschleifen.
* Schutzstopps basieren auf `StartProtection`, sodass keine direkte Stop-/Limit-Order-Buchhaltung erforderlich ist.
* Das Volumen wird vor dem Senden von Bestellungen anhand der Börsenlimits normalisiert und entspricht den StockSharp-Konventionen.
