# Envelopes-EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie repliziert den MetaTrader-4 Expert Advisor "EnvelopesEA". Sie wendet eine exponentielle gleitende Durchschnittshülle auf den primären Kerzenstrom an und handelt Mean Reversion. Wenn der Markt weit außerhalb der Hülle liegt, wird eine konträre Marktorder gesendet. Positionen werden geschlossen, sobald der Preis in Gegenrichtung wieder in die Hülle eintritt. Der ursprüngliche Expert Advisor wurde 2019 auf EUR/USD getestet; die StockSharp-Portierung behält dieselbe Logik bei und stellt alle wichtigen Eingaben als optimierbare Parameter bereit.

## Handelslogik
1. Berechnen Sie einen exponentiellen gleitenden Durchschnitt (EMA) der Länge `EnvelopePeriod` auf den ausgewählten Kerzen.
2. Erstellen Sie eine obere und eine untere Hülle, indem die EMA mit `UpperDeviationPercent` beziehungsweise `LowerDeviationPercent` erweitert wird.
3. Wenden Sie einen zusätzlichen Einstiegspuffer an, definiert durch `EntryOffsetPoints` (multipliziert mit dem Preisschritt des Instruments), um verfrühte Trades zu vermeiden.
4. Wenn keine Position offen ist:
   - Long einsteigen, wenn der Schlusskurs unter die untere Hülle abzüglich Einstiegspuffer fällt.
   - Short einsteigen, wenn der Schlusskurs über die obere Hülle zuzüglich Einstiegspuffer steigt.
5. Wenn eine Position besteht:
   - Long-Positionen schließen, sobald der Schlusskurs wieder über die obere Hülle kreuzt.
   - Short-Positionen schließen, sobald der Schlusskurs wieder unter die untere Hülle kreuzt.

Die Strategie hält immer höchstens eine offene Position und verwendet Marktorders sowohl für Einstiege als auch für Ausstiege.

## Geldmanagement
Das Ordervolumen wird direkt über den Parameter `Volume` (Lots) angegeben. Es gibt keine automatischen Martingale- oder Pyramiding-Regeln, wodurch das Verhalten identisch zur neuesten MQ4-Implementierung bleibt, in der Skalierungsfunktionen standardmäßig deaktiviert waren.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `Volume` | Ordervolumen in Lots. | 0.2 |
| `EnvelopePeriod` | Länge der EMA, die die Basis der Hülle bildet. | 50 |
| `UpperDeviationPercent` | Prozentuale Abweichung für das obere Band. | 0.5 |
| `LowerDeviationPercent` | Prozentuale Abweichung für das untere Band. | 0.5 |
| `EntryOffsetPoints` | Zusätzliche Distanz in Preisschritten, die der Preis vor dem Einstieg über das Band hinaus laufen muss. | 100 |
| `CandleType` | Zeitrahmen für Kerzen und Indikatorberechnungen. | 30-Minuten-Kerzen |

Alle numerischen Parameter (außer `CandleType`) sind als optimierbar markiert, um die ursprünglichen Optimierungsabläufe nachbilden zu können.

## Hinweise
- Die Hülle verwendet eine EMA statt der SMA aus früheren Versionen, weil sich das MQ4-Skript in der neuesten Iteration zu einer exponentiellen Basis entwickelt hat. Dies reagiert schneller auf Preisschwünge und verbessert das Timing der Mean Reversion.
- Der Einstiegspuffer wird mit dem Instrumenten-`PriceStep` multipliziert. Stellen Sie sicher, dass die Wertpapiermetadaten eine gültige Schrittgröße enthalten; andernfalls fällt die Strategie auf den konservativen Standard `0.0001` zurück.
- Die Chart-Visualisierung enthält Preiskerzen, die EMA-Hülle und die Trades der Strategie, sodass das Signalverhalten leicht gegen den ursprünglichen Expert Advisor validiert werden kann.
