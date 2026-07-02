# Natuseko Protrader 4H-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Natuseko Protrader 4H-Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters *NatusekoProtrader4HStrategy*. Das Original
Der Roboter kombiniert exponentielle gleitende Durchschnitte, einen durch Bollinger-Bänder gefilterten MACD-Oszillator, RSI-Schwellenwerte und den Parabolic SAR
Identifizieren Sie starke Ausbruchskerzen im Vier-Stunden-Zeitrahmen. Wenn eine qualifizierte Kerze erscheint, öffnet sich das System entweder sofort oder
wartet vor dem Eintritt auf einen Rückzug in Richtung des schnellen EMA. Sobald die Strategie positioniert ist, führt sie teilweise Gewinnmitnahmen und vollständige Ausstiege durch
basiert auf den Signalen RSI und Parabolic SAR und repliziert den im MQL-Code vorhandenen Geldverwaltungsblock.

## Handelslogik
1. Abonnieren Sie den durch `CandleType` definierten primären Kerzenstream (standardmäßig 4-Stunden-Kerzen) und verarbeiten Sie nur fertige Kerzen.
2. Berechnen Sie drei exponentielle gleitende Durchschnitte (schnell, langsam und Trend) für die Schlusskurse. Alle drei haben konfigurierbare Längen.
3. Geben Sie den Indikator MACD ein (schnelle, langsame und Signalperioden aus EA) und wenden Sie einen einfachen gleitenden Durchschnitt plus Bollinger-Bänder an
die Hauptzeile MACD. Die Mittellinie Bollinger fungiert als Referenzniveau, das von der Version MQL verwendet wird.
4. Berechnen Sie den RSI für Schlusskurse und den Parabolic SAR unter Verwendung vollständiger Kerzendaten. Diese Indikatoren steuern sowohl Ein- als auch Ausstiege.
5. Erkennen Sie bullische Setup-Kerzen, wenn alle der folgenden Bedingungen erfüllt sind:
   - Der Wert „Schnell“ EMA liegt sowohl über dem Wert „Langsam“ als auch über dem Wert „Trend“ EMA.
   - RSI liegt über `RsiEntryLevel`, aber unter `RsiTakeProfitLong`.
   - Die Hauptlinie MACD liegt sowohl über der kurzen SMA als auch über der Mittellinie Bollinger. der SMA liegt ebenfalls über der Mittellinie.
   - Der Kerzenkörper ist größer als beide Schatten, was bedeutet, dass die Kerze in Bewegungsrichtung stark schließt.
   - Parabolic SAR liegt unterhalb des Kerzenschlusses.
6. Erkennen Sie bärische Setups mithilfe der symmetrischen Prüfungen (schneller EMA unten, RSI zwischen `RsiTakeProfitShort` und `RsiEntryLevel`, MACD-Werte
unterhalb der Bollinger-Mittellinie, bärischer Kerzenkörper und SAR über dem Schlusskurs).
7. Wenn die qualifizierte Kerze zu weit vom Trend EMA entfernt ist (Entfernung über `DistanceThresholdPoints`), setzen Sie eine ausstehende Markierung und warten Sie auf eine
Rückzug. Ein Long-Einstieg wird ausgelöst, sobald der Preis den schnellen EMA berührt, während RSI und SAR weiterhin dem bullischen Szenario entsprechen; die
Der kurze Einstieg funktioniert analog bei Pullbacks zum schnellen EMA von unten.
8. Wenn kein Pullback erforderlich ist, schließt die Strategie alle entgegengesetzten Positionen und eröffnet eine neue Position mit `TradeVolume` Lots. Stop-Loss
Die Platzierung folgt den EA-Regeln: Zuerst wird dem Parabolic SAR der Vorzug gegeben, wenn `UseSarStopLoss` aktiviert ist, andernfalls der Trend
EMA wird verwendet. `StopOffsetPoints` wird mit der Preisstufe des Instruments in einen Preisabstand umgewandelt und auf das Stop-Level angewendet.
9. Während eine Long-Position offen ist, berechnet die Strategie den Stop-Preis kontinuierlich neu und verwaltet Ausstiege:
   - Wenn der Preis unter den Stop fällt, wird die gesamte Position geschlossen.
   - Nach Erreichen von mindestens `MinimumProfitPoints` Gewinn (in Instrumentenpunkten) kann die Strategie die Hälfte der Position schließen, wenn der
RSI überschreitet `RsiTakeProfitLong` oder wenn der Parabolic SAR über dem Preis liegt (gesteuert durch `UseRsiTakeProfit` und
`UseSarTakeProfit`).
   - Sobald der Gewinn ausreichend ist und RSI wieder unter `RsiEntryLevel` fällt, wird das verbleibende Long-Engagement geschlossen.
10. Short-Positionen spiegeln die gleichen Regeln wider, wobei die RSI-Schwellenwerte umgekehrt und SAR-Schecks im Verhältnis zum Preis umgedreht sind.

## Positionsmanagement
- Teilausstiege kommen höchstens einmal pro Handelsseite vor. Nach dem Schließen der Hälfte der Position wartet die Strategie auf die vollständige Ausstiegsbedingung
(RSI überschreitet das neutrale Niveau oder ein Stop-Loss-Treffer).
- Stop-Loss-Preise werden bei jeder Kerze unter Verwendung des neuesten Parabolic SAR- oder Trendwerts EMA neu berechnet, um mit der MQL-Logik in Einklang zu bleiben.
- Wenn die Positionsgröße auf Null zurückkehrt, wird der interne Status (Pending-Entry-Flags, Stop-Referenzen und Teil-Exit-Marker) zurückgesetzt, sodass die Positionsgröße auf Null zurückkehrt
Der nächste Trade beginnt sauber.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Zeitrahmen von 4 Stunden | Primärer Zeitrahmen, der von der Strategie verarbeitet wird. |
| `TradeVolume` | `decimal` | `0.1` | Auftragsvolumen, das für Einträge verwendet wird. |
| `FastEmaPeriod` | `int` | `13` | Länge des schnellen EMA-Filters. |
| `SlowEmaPeriod` | `int` | `21` | Länge des langsameren EMA-Filters. |
| `TrendEmaPeriod` | `int` | `55` | EMA wird für Distanzprüfungen und Stop-Loss-Platzierung verwendet. |
| `MacdFastPeriod` | `int` | `5` | Schnelle Länge von EMA innerhalb des Indikators MACD. |
| `MacdSlowPeriod` | `int` | `200` | Langsame Länge von EMA innerhalb des Indikators MACD. |
| `MacdSignalPeriod` | `int` | `1` | Länge des gleitenden Signaldurchschnitts innerhalb des Indikators MACD. |
| `BollingerPeriod` | `int` | `20` | Anzahl der MACD Stichproben, die zur Berechnung von Bollinger Bändern verwendet werden. |
| `BollingerWidth` | `decimal` | `1` | Standardabweichungsmultiplikator für die MACD Bollinger-Bänder. |
| `MacdSmaPeriod` | `int` | `3` | Länge der MACD Glättung SMA. |
| `RsiPeriod` | `int` | `21` | Länge des RSI-Indikators. |
| `RsiEntryLevel` | `decimal` | `50` | Neutraler RSI-Schwellenwert, der von Ein- und Austrittsregeln gemeinsam genutzt wird. |
| `RsiTakeProfitLong` | `decimal` | `65` | RSI-Level, das teilweise Gewinnmitnahmen für Long-Positionen ermöglicht. |
| `RsiTakeProfitShort` | `decimal` | `35` | RSI-Level, das teilweise Gewinnmitnahmen für Short-Positionen ermöglicht. |
| `DistanceThresholdPoints` | `decimal` | `100` | Maximaler Abstand in Instrumentenpunkten zwischen Preis und Trend EMA, bevor der Einstieg verzögert wird. |
| `SarStep` | `decimal` | `0.02` | Beschleunigungsschritt für den Parabolic SAR. |
| `SarMaximum` | `decimal` | `0.2` | Maximale Beschleunigung für den Parabolic SAR. |
| `UseSarStopLoss` | `bool` | `false` | Verwenden Sie Parabolic SAR, um den Schutzstopp abzuleiten. |
| `UseTrendStopLoss` | `bool` | `true` | Nutzen Sie den Trend EMA, um den Schutzstopp abzuleiten. |
| `StopOffsetPoints` | `int` | `0` | Zusätzlicher Offset (in Punkten) zum Schutz-Stopp-Preis. |
| `UseSarTakeProfit` | `bool` | `true` | Teilausstiege aktivieren, wenn der Preis den Parabolic SAR überschreitet. |
| `UseRsiTakeProfit` | `bool` | `true` | Aktivieren Sie Teilexits, wenn RSI den Take-Profit-Schwellenwert erreicht. |
| `MinimumProfitPoints` | `decimal` | `5` | Mindestgewinn (in Punkten), bevor die Regeln für teilweise oder vollständige Gewinnmitnahmen aktiviert werden. |

## Unterschiede zum Original EA
- StockSharp handelt Nettopositionen. Um das Single-Ticket-Verhalten von MetaTrader zu emulieren, schließt die Strategie automatisch das Gegenteil aus
Exposition bevor Sie einen neuen Trade in die andere Richtung eröffnen.
- Money-Management-Helfer werden mit Marktaufträgen implementiert, anstatt einzelne Aufträge zu ändern, da StockSharp nicht verwaltet wird
Haltestellen pro Ticket. Der Effekt entspricht dem von EA: ein teilweiser Ausstieg, gefolgt von einem endgültigen Ausstieg, wenn die Dynamik von RSI nachlässt.
- Preisentfernungsberechnungen basieren auf dem Instrument `PriceStep`. Wenn das Wertpapier keinen Preisschritt definiert, geht die Strategie von einem aus
Schritt 1. Passen Sie `DistanceThresholdPoints` und `MinimumProfitPoints` entsprechend für Instrumente an, die unterschiedliche Punktgrößen verwenden.

## Anwendungstipps
- Konfigurieren Sie `TradeVolume` entsprechend der Chargenstufe des Instruments; Der Konstruktor weist also auch `Strategy.Volume` denselben Wert zu
Hilfsmethoden verwenden die erwartete Größe.
- Wenn Trades zu oft verzögert werden, weil Kerzen weit vom Trend EMA schließen, senken Sie `DistanceThresholdPoints` oder deaktivieren Sie den Filter um
auf Null setzen.
- Es wird empfohlen, die Strategie zu zeichnen: Der Code zeichnet Kerzen, die drei EMAs, RSI, Parabolic SAR und MACD Bollinger Bänder, damit Sie es können
Bestätigen Sie die konvertierte Logik visuell.
- Die MACD-Parameter spiegeln die ungewöhnliche Kombination von EA wider (schnell=5, langsam=200, Signal=1). Erwägen Sie eine Optimierung vor der Live-Schaltung
weil eine so lange langsame Periode sehr glatte, aber nacheilende Werte erzeugt.
