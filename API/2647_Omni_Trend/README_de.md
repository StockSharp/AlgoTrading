# Omni Trend Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Omni Trend Strategie ist ein direkter Port des MetaTrader-Experten "Exp_Omni_Trend". Sie kombiniert einen gleitenden Durchschnitt mit einem ATR-basierten Kanal, um den dominanten Trend zu erkennen und zwischen Long- und Short-Exposition zu wechseln. Die StockSharp-Version behält das ursprüngliche Verhalten bei, einschließlich der Verzögerung zwischen Signalerkennung und Orderausführung sowie der Möglichkeit, individuelle Einstiegs- oder Ausstiegs-Beine zu deaktivieren.

Die Strategie abonniert die konfigurierte Kerzenserie und speist jeden abgeschlossenen Balken in die Omni-Trend-Logik ein. Der gleitende Durchschnitt dient als Schätzung der zentralen Tendenz, während ATR-Multiplikatoren Volatilitätshüllen aufbauen. Die Hüllen verhalten sich wie Trailing-Stops: Ein Preis, der über die vorherige Hüllengrenze hinaus schließt, dreht den Trend um, generiert ein neues Einstiegssignal in die neue Richtung und schließt sofort jede entgegengesetzte Exposition.

Wenn die optionalen Stop-Loss- und Take-Profit-Schwellenwerte aktiviert sind, wirken sie auf der Broker-Seite in Preisschritten und ergänzen die indikatorbasierten Ausstiege. Die Positionsgröße wird über die integrierte `Volume`-Eigenschaft der Strategie gesteuert (Standard `1`).

## Handelslogik

1. Den gewählten gleitenden Durchschnitt (`MaType`, `MaLength`, `AppliedPrice`) auf dem Kerzenstrom berechnen.
2. ATR (`AtrLength`) berechnen und zwei adaptive Bänder mit `VolatilityFactor` und `MoneyRisk` ableiten. Das obere Band schützt Short-Positionen, das untere Band schützt Long-Positionen.
3. Wenn der Preis das Schutzband des vorherigen Balkens überschreitet, ändert sich der Trend:
   - Ein bullischer Ausbruch (`HighPrice` über dem vorherigen oberen Band) dreht den Trend auf "hoch", schließt jede Short-Position wenn erlaubt, und öffnet nach `SignalBar` abgeschlossenen Kerzen eine Long-Position.
   - Ein bärischer Ausbruch (`LowPrice` unter dem vorherigen unteren Band) dreht den Trend auf "runter", schließt jede Long-Position wenn erlaubt, und öffnet nach der konfigurierten Verzögerung eine Short-Position.
4. Solange der Trend bullisch bleibt, fordert die Strategie weiterhin Short-Ausstiege; die symmetrische Regel gilt für einen bärischen Trend und Long-Ausstiege. Dies spiegelt das Verhalten des MetaTrader-Experten wider, wo das entgegengesetzte Band konstant flache Exposition gegen die vorherrschende Richtung erzwingt.
5. Das optionale Risikomanagement überwacht jede abgeschlossene Kerze. Wenn der aktuelle Balken den Stop- oder Zielpreis (ausgedrückt in Preisschritten) erreicht, wird die Position sofort geschlossen und der gespeicherte Einstiegspreis wird zurückgesetzt.

Signale werden über eine FIFO-Warteschlange geplant. Wenn `SignalBar` null ist, werden sie beim Schluss derselben Kerze ausgeführt. Andernfalls werden sie auf der Eröffnung der Kerze ausgelöst, die die Verzögerung abschließt, was den "vorherigen Balken"-Ausführungsstil des Quellexperten repliziert.

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|---------|
| `CandleType` | Kerzentyp (Zeitrahmen) für Berechnungen. | 4-Stunden-Zeitrahmen |
| `MaLength` | Periode des gleitenden Durchschnitts. | 13 |
| `MaType` | Methode des gleitenden Durchschnitts: einfach, exponentiell, geglättet oder linear gewichtet. | Exponentiell |
| `AppliedPrice` | Preisfeld für den gleitenden Durchschnitt (Schluss, Eröffnung, Hoch, Tief, Median, Typisch, Gewichtet). | Schluss |
| `AtrLength` | ATR-Periode für den Volatilitätskanal. | 11 |
| `VolatilityFactor` | Multiplikator für ATR beim Aufbau des Rohkanals. | 1.3 |
| `MoneyRisk` | Versatzfaktor, der den Kanal vom gleitenden Durchschnitt wegschiebt, identisch mit dem MQL-Input. | 0.15 |
| `SignalBar` | Anzahl abgeschlossener Kerzen vor der Signalausführung. | 1 |
| `EnableBuyOpen` | Long-Positionen öffnen erlauben. | true |
| `EnableSellOpen` | Short-Positionen öffnen erlauben. | true |
| `EnableBuyClose` | Long-Positionen bei bärischem Trend schließen erlauben. | true |
| `EnableSellClose` | Short-Positionen bei bullischem Trend schließen erlauben. | true |
| `StopLossPoints` | Optionaler Schutz-Stop-Abstand in Preisschritten. `0` deaktiviert. | 1000 |
| `TakeProfitPoints` | Optionaler Gewinnziel-Abstand in Preisschritten. `0` deaktiviert. | 2000 |
| `Volume` | Strategie-Eigenschaft zur Steuerung der Handelsgröße. | 1 |

## Hinweise und Empfehlungen

- Die StockSharp-Implementierung speist dieselben Indikatorwerte wie das Original ein und reproduziert seine Trendwechsel. Präzise Ausführungen hängen jedoch von der Datenquelle und der Ausführungslatenz ab.
- Setzen Sie `SignalBar = 1`, um den Standardwert des Expertenberaters zu imitieren, bei dem Orders auf der Eröffnung der nächsten Kerze nach Verfügbarkeit eines Signals ausgeführt werden. Größere Werte verzögern die Ausführung weiter; `0` führt beim aktuellen Schluss aus.
- Stop-Loss- und Take-Profit-Schwellenwerte werden in Punkten (Preisschritte) ausgedrückt. Stellen Sie sicher, dass das verbundene Wertpapier einen gültigen `PriceStep` bereitstellt.
- Der integrierte Chart zeichnet die Kerzenserie, den gewählten gleitenden Durchschnitt und die eigenen Trades der Strategie für schnelle visuelle Validierung.
- Deaktivieren Sie spezifische Einstiegs- oder Ausstiegs-Schalter, um die Strategie auf einseitigen Betrieb zu beschränken oder Ausstiege manuell zu handhaben.
- Die Strategie erstellt keine Pending-Orders; sie gibt Market-Orders über `BuyMarket` und `SellMarket` aus, genau wie die direkte Order-Platzierung des Quellexperten.

## Dateien

- `CS/OmniTrendStrategy.cs` — C#-Implementierung der Strategie.
- `README.md`, `README_ru.md`, `README_zh.md` — Dokumentation in Englisch, Russisch und Chinesisch.

Python-Unterstützung wurde auf Anfrage bewusst weggelassen.
