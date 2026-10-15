# Martin 1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertierung des MetaTrader 5-Experten «Martin 1» in die StockSharp-High-Level-Strategie-API. Der Algorithmus hält kontinuierlich eine Exposition aufrecht und verwendet Hedging-artige Martingale-Schritte, um Drawdowns zu erholen, während er in profitable Trends pyramidisiert.

## Handelslogik

1. **Initiale Exposition** – wenn die Strategie flat ist, öffnet sie sofort eine Position in der durch `StartDirection` definierten Richtung, unabhängig vom Zeitfilter. Die Basisordergröße wird aus `InitialVolume` entnommen, nachdem auf den Volumenschritt des Instruments gerundet wurde.
2. **Zeitfensterfilter** – wenn `UseTradingHours` aktiviert ist, sind nur Skalierungsaktionen (Pyramidisierung oder Hedging) zwischen `StartHour` und `EndHour` einschließlich erlaubt, wobei die Exchange-Zeit aus Kerzen-Zeitstempeln verwendet wird.
3. **Pyramidisierung von Gewinnern** – jede offene Position wird bei jeder fertigen Kerze bewertet. Wenn der schwebende Gewinn einer Long-Position die Take-Profit-Distanz überschreitet und positiv bleibt, wird eine zusätzliche Long-Order mit dem aktuellen Volumen gesendet. Short-Positionen verhalten sich symmetrisch. Der Preis der neuen Order wird als Schlusskurs der aktuellen Kerze angenommen.
4. **Hedging-Martingale** – wenn die Startrichtung Long ist und eine Long-Position mehr als `(StopLossPips × Pip-Größe × (Multiplikationsindex + 1))` verliert, öffnet die Strategie eine entgegengesetzte Short-Order. Vor der Platzierung des Hedges wird das Volumen mit `LotMultiplier` multipliziert, auf den erlaubten Schritt gerundet, und der Multiplikationszähler erhöht. Die gleiche Logik wird umgekehrt für die Short-Startrichtung angewendet. Hedging stoppt, sobald `MaxMultiplications` Schritte erreicht wurden.
5. **Globales Gewinnziel** – der unrealisierte Gewinn aller verbleibenden Positionen (in Geld umgerechnet mit `PriceStep`/`StepPrice`) wird summiert. Wenn er `MinProfit` überschreitet, wird jede offene Position durch eine Marktorder in der entgegengesetzten Richtung geschlossen, und der Martingale-Zustand wird zurückgesetzt.

## Risiko- und Geldmanagement

- Die Pip-Größe wird aus dem Kurspreis des Instruments berechnet. Drei- und fünfstellige Kurse multiplizieren den Schritt mit zehn, um die ursprüngliche MetaTrader-Pip-Anpassung zu emulieren.
- Volumen werden auf den nächsten `VolumeStep` abgerundet. Wenn der gerundete Wert unter den Schritt fällt, wird die Order übersprungen.
- Der Martingale-Zähler und das aktuelle Volumen werden zurückgesetzt, wann immer das Buch flat wird, entweder natürlich oder nach Erreichen des globalen Gewinnziels.
- Die Gewinnschätzung ignoriert Provisionen und Swaps und spiegelt das Verhalten des ursprünglichen Skripts wider, das sich ausschließlich auf den schwebenden PnL stützte.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Kerzentyp, der alle Berechnungen antreibt. | 1 Minuten-Zeitrahmen |
| `UseTradingHours` | Aktiviert oder deaktiviert den Zeitfensterfilter. | `true` |
| `StartHour` | Inklusive Stunde, in der der Zeitfilter neue Skalierungsaktionen erlaubt. | 2 |
| `EndHour` | Inklusive Stunde, in der Skalierungsaktionen stoppen. | 21 |
| `LotMultiplier` | Faktor, der auf das aktuelle Volumen vor dem Öffnen eines Hedges angewendet wird. | 1.6 |
| `MaxMultiplications` | Maximale Anzahl von Hedging-Schritten, die ausgelöst werden dürfen. | 5 |
| `StartDirection` | Richtung der ersten Order, nachdem die Strategie flat wird. | Buy |
| `MinProfit` | Schwebender Gewinn (in Geld), der alle Positionen zum Schließen zwingt. | 1.5 |
| `InitialVolume` | Basisvolumen für die erste Order und Reset-Zustand. | 0.1 |
| `StopLossPips` | Pip-Distanz, die den nächsten Martingale-Hedge auslöst. | 40 |
| `TakeProfitPips` | Pip-Distanz, die einen Pyramidisierungs-Einstieg auslöst. | 100 |

## Implementierungshinweise

- `ProcessCandle` verwendet die High-Level-Kerzen-Abonnement-Pipeline (`SubscribeCandles().Bind(...)`) und arbeitet ausschließlich auf fertigen Kerzen, entsprechend den Plattformrichtlinien.
- Die gesicherte Exposition wird intern mit zwei FIFO-Listen verfolgt, sodass die Strategie das MetaTrader-Hedging-Verhalten auch auf Netting-Konten emulieren kann.
- Die Gewinnkonvertierung stützt sich auf `Security.PriceStep` und `Security.StepPrice`. Wenn diese Werte nicht verfügbar sind, wird die Preisdifferenz direkt mit dem gehandelten Volumen als Fallback multipliziert.
- Die Strategie handelt kontinuierlich; das Deaktivieren des Zeitfilters oder das Einstellen breiter Stunden lässt den Algorithmus wie den ursprünglichen Dauerbetrieb-Experten verhalten.
