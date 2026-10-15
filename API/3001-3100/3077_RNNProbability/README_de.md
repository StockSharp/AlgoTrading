# RNN Probability Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die RNN Probability Strategie ist eine Konvertierung des MetaTrader-Experten *RNN (barabashkakvn's edition)*. Der ursprüngliche Roboter sammelt drei RSI-Schnappschüsse, die durch den RSI-Zeitraum getrennt sind, und speist sie in ein handgefertigtes Wahrscheinlichkeitsgitter, das ein rekurrentes neuronales Netzwerk emuliert. Der StockSharp-Port repliziert dieses Verhalten mit der High-Level-Kerzenabonnement-API und konvertiert automatisch die MetaTrader-Lots, Preisschritte und Stop/Ziel-Abstände in StockSharp-Konzepte.

Sobald der RSI-Wert der letzten abgeschlossenen Kerze verfügbar ist, schaut die Strategie um ein und zwei RSI-Perioden zurück, um eine Drei-Punkte-Geschichte zu erstellen. Diese normalisierten Ablesungen werden mit den acht MetaTrader-Gewichten (`Weight0` … `Weight7`) kombiniert, um eine Wahrscheinlichkeit zu erzeugen, dass der Markt fallen sollte. Die Wahrscheinlichkeit wird in den Bereich `[-1; 1]` umgerechnet, und das Vorzeichen bestimmt, ob eine Long- oder Short-Position eröffnet wird. Es wird jeweils nur eine Position gehalten, was dem ursprünglichen Expert Advisor entspricht.

## Handelslogik
1. Die konfigurierte Kerzenserie abonnieren und den Indikator `RelativeStrengthIndex` manuell mit dem ausgewählten `AppliedPrice`-Eingang verarbeiten (standardmäßig Open).
2. Die abgeschlossenen RSI-Werte in einem Rollpuffer speichern, der groß genug ist, um auf die RSI-Ablesung von ein und zwei vollständigen Perioden zurück zuzugreifen.
3. Die drei RSI-Werte auf den Bereich `[0; 1]` normalisieren und das neuronale Netzgitter auswerten:
   - Der erste Zweig (`Weight0`, `Weight1`, `Weight2`, `Weight3`) behandelt den Fall, wenn der aktuelle RSI in der unteren Hälfte liegt (unter 50).
   - Der zweite Zweig (`Weight4`, `Weight5`, `Weight6`, `Weight7`) behandelt den Fall, wenn der aktuelle RSI in der oberen Hälfte liegt.
4. Die resultierende Wahrscheinlichkeit in ein Handelssignal zwischen `-1` und `+1` transformieren.
5. Wenn keine Position offen ist und das Signal negativ ist, `TradeVolume` Lots kaufen. Wenn das Signal nicht-negativ ist, stattdessen `TradeVolume` Lots verkaufen.
6. Optional symmetrische Stop-Loss- und Take-Profit-Niveaus in Pips aktivieren. Die Strategie konvertiert automatisch den Pip-Abstand in einen absoluten Preisversatz, einschließlich der Extraziffer-Anpassung von MetaTrader für 3- und 5-stellige Forex-Symbole.
7. Jede Entscheidung mit den RSI-Eingaben, der Wahrscheinlichkeit und dem resultierenden Signal protokollieren, das geschwätzige Verhalten des Quell-Experten widerspiegelnd.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-Stunden-Zeitrahmen | Primäre Kerzenserie für Indikatoraktualisierungen und Signalgenerierung. |
| `TradeVolume` | `decimal` | `1` | Lotgröße mit jeder Marktorder. |
| `RsiPeriod` | `int` | `9` | Länge des RSI-Indikators. Definiert auch den Abstand zwischen den historischen RSI-Stichproben. |
| `AppliedPrice` | `AppliedPriceType` | `Open` | Preiskomponente für den RSI (Open, Close, High, Low, Median, Typical, Weighted). |
| `StopLossTakeProfitPips` | `decimal` | `100` | Pip-Abstand für Stop-Loss und Take-Profit. Auf null setzen, um Schutzorders zu deaktivieren. |
| `Weight0` … `Weight7` | `decimal` | `6, 96, 90, 35, 64, 83, 66, 50` | Wahrscheinlichkeitsgewichte für die acht Gitterzweige. Jeder Wert stellt einen Prozentsatz zwischen 0 und 100 dar. |

## Unterschiede gegenüber dem ursprünglichen MetaTrader-Experten
- E-Mail-Benachrichtigungen wurden entfernt. StockSharp-Protokolle bieten dieselbe Einsicht ohne Abhängigkeit von einem SMTP-Server.
- Die Positionsgröße ist auf ein einziges `TradeVolume` festgelegt. Teilschließungen oder inkrementelles Skalieren sind absichtlich weggelassen, um dem Ein-Positions-Design des Quellcodes zu entsprechen.
- Indikatordaten werden über das High-Level-Kerzenabonnement von StockSharp geliefert, was manuelle `CopyBuffer`-Aufrufe und Zeigerarithmetik eliminiert.
- Die Pip-Konvertierung verwendet den `PriceStep` des Instruments und kompensiert automatisch 3/5-stellige Forex-Symbole, anstatt auf fest codierte Tick-Größen zurückzugreifen.

## Nutzungstipps
- `TradeVolume` vor dem Start der Strategie mit dem Mindest-Lot-Schritt des Instruments abstimmen; der Konstruktor überträgt den Wert auch in `Strategy.Volume`.
- Die acht Gewichte während der Optimierung einstellen, um das Wahrscheinlichkeitsgitter an verschiedene Märkte anzupassen. Alle Gewichte sind als Optimierungsparameter zugänglich.
- `StopLossTakeProfitPips` verringern oder auf null setzen, wenn auf Symbolen mit breiten Spreads oder bei diskretionären Ausstiegen gehandelt wird.
- Die Strategie einem Diagramm hinzufügen, um Kerzen, RSI und ausgeführte Trades zur einfacheren Validierung der neuronalen Netzausgabe zu visualisieren.

## Indikatoren
- Ein `RelativeStrengthIndex`, berechnet aus dem gewählten angewendeten Preis.
