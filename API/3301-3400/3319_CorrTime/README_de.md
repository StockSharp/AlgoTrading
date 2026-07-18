# CorrTime-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die CorrTime-Strategie ist ein Einzelwertsystem, das den gleichnamigen MetaTrader-Expert-Advisor repliziert. Sie analysiert die Korrelation zwischen Schlusskursen und ihrer chronologischen Reihenfolge, um Momentum-Beschleunigung oder -Umkehr zu erkennen. Der Algorithmus arbeitet auf abgeschlossenen Kerzen und kombiniert drei Bestätigungsebenen:

1. **Volatilitätsfilter:** Die Bollinger-Bandbreite muss innerhalb eines konfigurierbaren Bereichs akzeptabler Aktivität liegen, sodass das System flache und übermäßig volatile Phasen vermeidet.
2. **Trendstärkefilter:** Der Average Directional Index (ADX) muss über einer Schwelle bleiben, bevor Korrelationssignale bewertet werden.
3. **Korrelationsauslöser:** Pearson-, Spearman-, Kendall- oder Fechner-Korrelationsschätzer messen, wie eng sich der Preis mit der Zeit entwickelt. Eine plötzliche Änderung des Koeffizienten erzeugt die Handelsentscheidung.

Obwohl der ursprüngliche Roboter für EURUSD im H1-Zeitrahmen entworfen wurde, hält die StockSharp-Version alle Parameter konfigurierbar. Die Standardeinstellungen bleiben der Quelle treu (1-Stunden-Kerzen, Fechner-Korrelation, Reverse-Handelsmodus).

## Handelsablauf

1. Den gewählten `CandleType` abonnieren und auf eine abgeschlossene Bar warten.
2. Bollinger-Bänder und ADX-Werte auf der neuen Kerze aktualisieren.
3. Die Bar verwerfen, wenn:
   - Der in Pips konvertierte Bollinger-Spread außerhalb `[BollingerSpreadMin, BollingerSpreadMax]` liegt.
   - ADX unter `AdxLevel` liegt.
   - Die Kerze außerhalb des Handelsfensters `[EntryHour, EntryHour + OpenHours]` beginnt (mit Unterstützung für Mitternachtsüberlauf).
4. Eine rollierende Historie der Schlusskurse aufbauen und den Korrelationskoeffizienten über die Lookbacks `CorrelationRangeTrend` und `CorrelationRangeReverse` berechnen. Der Code berechnet die letzten drei Korrelationswerte neu, um ein echtes Kreuzen der Grenzen zu erkennen, genau wie die ursprüngliche Include-Datei dies mit Puffern tat.
5. Trendfolge-Auslöser (wenn `TradeMode` *TrendFollow* oder *Both* ist):
   - **Long:** Korrelation lag unter `CorrLimitTrendBuy`, blieb auf der vorherigen Bar darunter und kreuzt auf der letzten Bar über die Schwelle.
   - **Short:** Korrelation lag über `-CorrLimitTrendSell`, blieb auf der vorherigen Bar darüber und kreuzt auf der letzten Bar unter `-CorrLimitTrendSell`.
6. Umkehr-Auslöser (wenn `TradeMode` *Reverse* oder *Both* ist):
   - **Long:** Korrelation lag unter `-CorrLimitReverseBuy`, blieb auf der vorherigen Bar darunter und steigt auf der letzten Bar über `-CorrLimitReverseBuy`.
   - **Short:** Korrelation lag über `CorrLimitReverseSell`, blieb auf der vorherigen Bar darüber und fällt auf der letzten Bar unter `CorrLimitReverseSell`.
7. Wenn beide Richtungen gleichzeitig feuern, heben sich die Signale gegenseitig auf, entsprechend dem MetaTrader-Verhalten.
8. Wenn `CloseTradeOnOppositeSignal` aktiviert ist, schließt die Strategie sofort jede Gegenposition, bevor eine neue eröffnet wird.
9. Einstiege werden mit der Eigenschaft `Volume` dimensioniert und respektieren `MaxOpenOrders`, sodass die Netto-Exposure in keiner Richtung `Volume * MaxOpenOrders` überschreitet.
10. Risiko wird über `StartProtection` gesteuert: Stop-Loss und Take-Profit nutzen pipbasierte Distanzen, und das Trailing-Flag verwendet bei Aktivierung dieselbe Stopdistanz.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Zeitrahmen zur Kerzenerzeugung und Versorgung aller Indikatoren. |
| `CloseTradeOnOppositeSignal` | Schließt offene Positionen, wenn das nächste Signal in die Gegenrichtung zeigt. |
| `EntryHour`, `OpenHours` | Definiert das tägliche Handelsfenster. `OpenHours = 0` hält das Fenster für eine Stunde offen. |
| `BollingerPeriod`, `BollingerDeviation` | Standard-Bollinger-Band-Einstellungen auf Schlusskursen. |
| `BollingerSpreadMin`, `BollingerSpreadMax` | Minimale und maximale Breite (in Pips), die für den Bollinger-Kanal erforderlich ist. |
| `AdxPeriod`, `AdxLevel` | ADX-Konfiguration und erforderliche minimale Trendstärke. |
| `TradeMode` | Wahl zwischen Trendfolge, Umkehr oder kombinierter Auswertung. |
| `CorrelationRangeTrend`, `CorrelationRangeReverse` | Lookback-Längen für Korrelationsberechnungen. |
| `CorrelationType` | Wählt Pearson-, Spearman-, Kendall- oder Fechner-Korrelationsformeln. |
| `CorrLimitTrendBuy`, `CorrLimitTrendSell` | Schwellen, die einen gültigen Trendfolge-Ausbruch definieren. |
| `CorrLimitReverseBuy`, `CorrLimitReverseSell` | Schwellen, die einen gültigen Umkehr-Ausbruch definieren. |
| `TakeProfitPips`, `StopLossPips`, `TrailingStopPips` | Risikoparameter in Pips, übersetzt in Preiseinheiten mit der Pipgröße des Instruments. |
| `MaxOpenOrders` | Obergrenze für die aggregierte Anzahl von Einstiegen (seitliche Obergrenze gleich `Volume * MaxOpenOrders`). |

## Praktische Hinweise

- Die Pipgröße wird aus den Security-Dezimalstellen abgeleitet (5 oder 3 Dezimalstellen entsprechen einem 10x-Multiplikator), um MetaTraders Punktbehandlung zu imitieren. Passen Sie die Schwellen bei Nicht-Forex-Assets an.
- Korrelationspuffer benötigen mindestens `lookback + 2` abgeschlossene Kerzen, um ein Kreuzen zu bewerten. Während der Aufwärmphase bleibt die Strategie inaktiv.
- Da alle Logik auf abgeschlossenen Kerzen ausgeführt wird, ist die Strategie robust gegenüber Intrabar-Rauschen und spiegelt das ursprüngliche Verhalten auf Basis von `iTime`- und `iClose`-Snapshots.
- Kombinieren Sie diese Strategie beim Einsatz mehrerer Instanzen mit Portfolio-Risikokontrollen, da der ursprüngliche Roboter ebenfalls die Gesamtzahl der Orders über Symbole begrenzte.
