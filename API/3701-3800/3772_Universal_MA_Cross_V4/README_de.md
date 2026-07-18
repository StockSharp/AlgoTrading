# Universelle MA Cross V4-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Universal MA Cross V4-Strategie** ist eine High-Level-StockSharp-Portierung des MetaTrader 4-Expertenberaters „Universal MACross EA v4“. Der Algorithmus folgt der Interaktion zwischen einem konfigurierbaren schnellen gleitenden Durchschnitt und einem langsam gleitenden Durchschnitt. Es unterstützt mehrere gleitende Durchschnittstypen, auswählbare Preisquellen, ein stündliches Handelsfenster und flexibles Positionsmanagement, einschließlich Stop-and-Reverse-Verhalten, Schutzziele und Trailing Stops. Die Strategie ist für die balkenbasierte Ausführung unter Verwendung des StockSharp-High-Level-API mit Kerzenabonnements konzipiert.

## Handelslogik
### Indikatorverarbeitung
* Für jede fertige Kerze werden zwei gleitende Durchschnitte ausgewertet. Jeder gleitende Durchschnitt kann seine eigene Länge, Glättungsmethode (einfach, exponentiell, geglättet oder linear gewichtet) und Preisquelle (Schluss, Eröffnung, Hoch, Tief, Median, typisch oder gewichtet) verwenden.
* Der Filter **MinCrossDistancePoints** erfordert, dass die schnellen und langsamen Durchschnittswerte am Crossover-Balken um mindestens die angegebene Anzahl von Preisschritten voneinander abweichen. Wenn **ConfirmedOnEntry** aktiviert ist, wird die Divergenz bei der zuvor abgeschlossenen Kerze validiert, wodurch der „bestätigte“ Modus des ursprünglichen EA reproduziert wird.
* Durch die Einstellung **ReverseCondition** werden bullische und bärische Signale ausgetauscht, ohne die Indikatorkonfiguration zu ändern.

### Einreisebestimmungen
1. Ein Long-Einstieg liegt vor, wenn der schnelle Durchschnitt den langsamen Durchschnitt um mindestens **MinCrossDistancePoints** überschreitet. Ein kurzer Einstieg erfordert das entgegengesetzte Kreuz.
2. Wenn **StopAndReverse** wahr ist, schließt ein entgegengesetztes Signal die aktive Position, bevor neue Einträge berücksichtigt werden.
3. **OneEntryPerBar** verhindert mehrere Einträge innerhalb derselben Kerze, indem es den Zeitstempel der letzten Bestellung verfolgt.
4. Die Ordergröße wird durch **TradeVolume** gesteuert. StockSharp wendet dieses Volumen automatisch auf die generierten Marktaufträge an.

### Positionsmanagement
* Stop-Loss- und Take-Profit-Abstände werden in Punkten durch **StopLossPoints** und **TakeProfitPoints** definiert. Sie werden über die Instrumentenpreisstufe in absolute Preise umgerechnet. Wenn **PureSar** aktiv ist, ist die gesamte Schutzlogik deaktiviert, genau wie die Option „Pure SAR“ in der Version MQL.
* Die Trailing-Stop-Verwaltung spiegelt die MQL-Implementierung wider: Sobald sich der Preis weiter als **TrailingStopPoints** vom Einstiegsniveau entfernt, wird der Stop um die gleiche Distanz hinter den Markt gezogen. Trailing Stops werden ignoriert, wenn **PureSar** aktiviert ist.
* Die Schutzniveaus werden bei jeder geschlossenen Kerze überwacht. Wenn die Kerzenspanne gegen den aktiven Stop oder das aktive Ziel verstößt, schließt die Strategie die Position per Marktauftrag, um ein deterministisches Verhalten anhand historischer Daten aufrechtzuerhalten.

### Sitzungsfilter
* Das Flag **UseHourTrade** beschränkt den Handel auf das inklusive Fenster zwischen **StartHour** und **EndHour** (0–23). Die Sitzungsgrenzen beginnen um Mitternacht, wenn die Endstunde kleiner als die Startstunde ist. Die Positionsverwaltung, einschließlich Trailing Stops, bleibt außerhalb der Sitzung aktiv, es sind jedoch keine neuen Eingaben zulässig.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `FastMaPeriod`, `SlowMaPeriod` | Längen der schnellen und langsamen gleitenden Durchschnitte. |
| `FastMaType`, `SlowMaType` | Methoden des gleitenden Durchschnitts: Einfach, exponentiell, geglättet oder linear gewichtet. |
| `FastPriceType`, `SlowPriceType` | Preisquellen flossen in jeden gleitenden Durchschnitt ein. |
| `StopLossPoints`, `TakeProfitPoints` | Schutzabstände in Preisstufen. Zum Deaktivieren auf `0` setzen. |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Preisschritten. Auf `0` setzen, um das Nachstellen zu deaktivieren. |
| `MinCrossDistancePoints` | Mindestabstand zwischen den Durchschnittswerten, der zur Validierung eines Kreuzes erforderlich ist. |
| `ReverseCondition` | Tauschen Sie bullische und bärische Regeln aus, ohne die Indikatoren zu ändern. |
| `ConfirmedOnEntry` | Validieren Sie die Signale des zuvor geschlossenen Balkens. Zur sofortigen Bestätigung deaktivieren. |
| `OneEntryPerBar` | Erlauben Sie höchstens eine neue Position pro Kerze. |
| `StopAndReverse` | Schließen und kehren Sie die aktuelle Position um, wenn das entgegengesetzte Signal erscheint. |
| `PureSar` | Deaktivieren Sie die Stop-Loss-, Take-Profit- und Trailing-Stop-Logik. |
| `UseHourTrade`, `StartHour`, `EndHour` | Sitzungsfilter, der Einträge auf einen bestimmten Stundenbereich beschränkt. |
| `TradeVolume` | Von `BuyMarket` und `SellMarket` verwendetes Bestellvolumen. |
| `CandleType` | Für Indikatorberechnungen abonnierte Kerzenserie. |

## Konvertierungshinweise
* Preisbasierte Entfernungen werden in MetaTrader Punkten ausgedrückt. Der Helfer `GetPriceOffset` wandelt diese Werte mithilfe der Sicherheitspreisschrittweite oder der Dezimalgenauigkeit in StockSharp-Preise um. Dadurch bleibt das Strategieverhalten unabhängig vom Instrument am ursprünglichen EA ausgerichtet.
* Trailing Stops werden intern verwaltet, da StockSharp High-Level-Strategien auf fertige Kerzen angewendet werden. Dieser deterministische Ansatz stellt sicher, dass Backtests mit Kerzen die beabsichtigte MT4-Trailing-Logik reproduzieren.
* Es ist kein Python-Port enthalten, der der Konvertierungsanforderung entspricht. In diesem Paket werden nur die C#-Implementierung und die mehrsprachige Dokumentation bereitgestellt.
