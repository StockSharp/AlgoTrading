# Separate-Trade-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Separate-Trade-Strategie ist eine Konvertierung des MetaTrader 5-Expertenberaters "Separate trade". Sie bewahrt die ursprüngliche Multi-Filter-Logik und adoptiert gleichzeitig die StockSharp-High-Level-API für robustes Orderverwaltung und Indikatorhandling. Die Strategie versucht, stille Marktwendepunkte zu erfassen, wenn Volatilität und Dispersion unterdrückt sind. Es wird jeweils nur eine Nettoposition gehalten, was die Absicht des ursprünglichen Codes widerspiegelt, der die Anzahl der gleichzeitigen Positionen begrenzte.

## Indikatoren und Daten
- Zwei gleitende Durchschnitte mit konfigurierbarer Methode (SMA, EMA, SMMA oder LWMA) und gemeinsamer Preisquelle.
- Average True Range (ATR) mit separaten Zeiträumen und Schwellenwerten für Long- und Short-Filter.
- Standardabweichung unter Verwendung desselben angewendeten Preises wie die gleitenden Durchschnitte, wiederum mit richtungsspezifischen Zeiträumen und Obergrenzen.
- Kerzen werden über einen konfigurierbaren `DataType`-Parameter geliefert, sodass die Strategie an jeden Zeitrahmen oder benutzerdefinierten Kerzenbauer angehängt werden kann.

## Parameter
| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Ordergröße in Lots. | `1` |
| `SlowMaPeriod` | Zeitraum des langsameren gleitenden Durchschnitts. | `65` |
| `FastMaPeriod` | Zeitraum des schnelleren gleitenden Durchschnitts. | `14` |
| `MaMethod` | Glättungsmethode für beide gleitenden Durchschnitte (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Exponential` |
| `PriceType` | Preiseingang für die gleitenden Durchschnitte und Standardabweichung (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Close` |
| `StopLossBuyPips` / `StopLossSellPips` | Stop-Loss-Abstand für Long- und Short-Trades in Pips (0 deaktiviert den Stop). | `50` |
| `TakeProfitBuyPips` / `TakeProfitSellPips` | Take-Profit-Abstand für Long- und Short-Trades in Pips (0 deaktiviert den Take-Profit). | `50` |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. | `5` |
| `TrailingStepPips` | Minimaler Gewinnvorschub in Pips, bevor der Trailing-Stop bewegt wird. Muss positiv sein, wenn Trailing aktiviert ist. | `5` |
| `MaxPositions` | Maximal erlaubte gleichzeitige Nettopositionen. Die StockSharp-Version arbeitet mit einer einzigen aggregierten Position, auch wenn der Wert größer als eins ist. | `1` |
| `DeltaBuyPips` / `DeltaSellPips` | Maximal erlaubter Abstand zwischen dem schnellen und langsamen gleitenden Durchschnitt (pro Richtung). Ein Wert von null deaktiviert den Abstandsfilter. | `2` |
| `AtrPeriodBuy` / `AtrPeriodSell` | ATR-Rückblickzeitraum für die Long- und Short-Filter. | `26` |
| `AtrLevelBuy` / `AtrLevelSell` | Oberer ATR-Schwellenwert, der vor dem Einstieg nicht überschritten werden darf. | `0.0016` |
| `StdDevPeriodBuy` / `StdDevPeriodSell` | Standardabweichungs-Rückblickzeitraum für die Long- und Short-Filter. | `54` |
| `StdDevLevelBuy` / `StdDevLevelSell` | Standardabweichungsobergrenze, die vor dem Einstieg nicht überschritten werden darf. | `0.0051` |
| `CandleType` | Vom Abonnement verwendeter Kerzendatentyp. | `TimeSpan.FromMinutes(15)` |

## Handelslogik
1. **Balkensynchronisation** – die Strategie agiert nur auf fertigen Kerzen, die vom konfigurierten Abonnement empfangen werden. Dies repliziert den `OnTick`-Neuer-Balken-Guard aus dem MetaTrader-Skript.
2. **Indikatorfilter** – für Long-Einstiege muss der langsame MA unter dem schnellen MA liegen, ATR muss unter `AtrLevelBuy` liegen, Standardabweichung muss unter `StdDevLevelBuy` liegen, und der MA-Abstand muss kleiner als `DeltaBuyPips` sein (wenn das Delta positiv ist). Short-Einstiege kehren die Bedingungen um und verwenden ihre eigenen ATR- und Abweichungsparameter.
3. **Positionssteuerung** – Trades werden nur eingegangen, wenn keine offene Position besteht und die letzte Einstiegszeit für die jeweilige Seite älter als die aktuelle Kerze ist. Dies verhindert Re-Einstiege innerhalb desselben Balkens, was der `m_last_deal_IN_*`-Prüfung im Quell-EA entspricht.
4. **Orderausführung** – Marktorders werden mit dem konfigurierten Volumen platziert. Umkehrtrades flachen die aktuelle Position automatisch ab, bevor eine neue geöffnet wird, dank der `Volume + Math.Abs(Position)`-Menge, die dem MQL-Verhalten des Schließens entgegengesetzter Exposure entspricht.

## Risikomanagement
- **Pip-Konvertierung** – Pip-Abstände werden unter Verwendung des `PriceStep` des Instruments konvertiert. Für Instrumente mit 3 oder 5 Dezimalstellen entspricht die Pip-Größe `PriceStep * 10`, was die ursprüngliche `digits_adjust`-Logik widerspiegelt.
- **Stop-Loss / Take-Profit** – die Strategie verfolgt Preislevels intern und steigt aus, wenn der Kerzenbereich den angegebenen Stop oder das Ziel berührt. Beide können deaktiviert werden, indem der Pip-Abstand auf null gesetzt wird.
- **Trailing-Stop** – sobald der Preis über `TrailingStopPips + TrailingStepPips` hinausgeht, wird der Stop bewegt, um die Trailing-Distanz aufrechtzuerhalten. Die Trailing-Schrittanforderung stimmt mit der MetaTrader-Implementierung überein und vermeidet das Bewegen des Stops um einen unbedeutenden Betrag.

## Implementierungshinweise
- Die Strategie verwendet eine einzelne aggregierte Position, da StockSharp standardmäßig mit Nettopositionen arbeitet. Obwohl der `MaxPositions`-Parameter für die Kompatibilität beibehalten wird, verhindert eine Überschreitung von eins einfach neue Einstiege, bis die aktuelle Position geschlossen ist.
- Indikatorwerte werden unter Verwendung der StockSharp-Indikatorklassen und der `Bind`-Infrastruktur berechnet, um manuellen Pufferzugriff gemäß den Projektrichtlinien zu vermeiden.
- Die Konvertierung hält alle Kommentare auf Englisch und ordnet jeden ursprünglichen Eingang einem dedizierten `StrategyParam` zu, sodass Optimierung und Designer-Integration verfügbar bleiben.
- Wenn `TrailingStopPips` positiv ist, muss `TrailingStepPips` auch positiv sein. Der Code stoppt die Strategie frühzeitig und schreibt eine Fehlermeldung, wenn diese Anforderung verletzt wird, was die Sicherheitsprüfung des MQL-Experten reproduziert.
