# MA zur Momentum-Min-Profit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den MetaTrader 5 Expert Advisor **MA on Momentum Min Profit.mq5**, indem sie den Crossover zwischen einem Momentum-Indikator und einem gleitenden Durchschnitt handelt, der auf der Grundlage der Momentum-Reihe berechnet wird. Ein zinsbullisches Signal erscheint, wenn das Momentum seinen Durchschnitt überschreitet, während der vorherige Balken das Momentum unter der neutralen 100-Marke hielt. Ein rückläufiges Signal wird erzeugt, wenn das Momentum unter den Durchschnitt fällt und der vorherige Balken über 100 liegt. Die Implementierung behält den ursprünglichen geldbasierten Aktienstopp und die feste Take-Profit-Distanz, gemessen in Punkten, bei.

## Handelslogik
1. Fordern Sie durch `CandleType` definierte Kerzen an und geben Sie sie in den Momentum-Indikator ein.
2. Glätten Sie den Impulsstrom mit einem gleitenden Durchschnitt, der durch `MomentumMovingAverageType` und `MomentumMovingAveragePeriod` definiert ist.
3. Erkennen Sie Überkreuzungen anhand der vorherigen Balkenwerte, um Doppelsignale zu vermeiden.
4. Optionale Funktionen aus der MQL-Version:
   - Kehren Sie die Richtung der erzeugten Signale um.
   - Schließen Sie das Gegenrisiko, bevor Sie einen neuen Trade eingehen, oder überspringen Sie die Eingabe ganz.
   - Erzwingen Sie jederzeit eine einzelne Nettoposition.
   - Ermöglichen Sie das Auslösen auf die aktuelle (sich bildende) Kerze statt auf den vollständig geschlossenen Balken.
5. Risikomanagement anwenden:
   - Eigenkapital-Stopp in Geld: `PnL + Position * (close - PositionPrice)` muss über `StopLossMoney` bleiben.
   - Take-Profit-Distanz in Punkten umgerechnet durch `Security.PriceStep`.

## Parameter
| Parameter | Typ | Standard | Beschreibung |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Kerzen zur Berechnung des Impulses. |
| `MomentumPeriod` | `int` | `14` | Rückblickzeitraum des Momentum-Indikators. |
| `MomentumMovingAveragePeriod` | `int` | `6` | Länge des auf den Impuls angewendeten gleitenden Durchschnitts. |
| `MomentumMovingAverageType` | `MomentumMovingAverageType` | `Smoothed` | Algorithmus für gleitenden Durchschnitt (einfach, exponentiell, geglättet, gewichtet). |
| `ReverseSignals` | `bool` | `false` | Spiegeln Sie MetaTrader Kauf-/Verkaufssignale. |
| `CloseOpposite` | `bool` | `true` | Schließen Sie die gegenüberliegende Position, bevor Sie eine neue Position eröffnen. |
| `OnlyOnePosition` | `bool` | `true` | Behalten Sie eine einzige Nettoposition bei. |
| `UseCurrentCandle` | `bool` | `false` | Bewerten Sie Signale anhand der sich aktuell bildenden Kerze anstelle des geschlossenen Balkens. |
| `StopLossMoney` | `decimal` | `15` | Kapitalabnahme vor Abschluss aller Geschäfte möglich. |
| `TakeProfitPoints` | `decimal` | `460` | Gewinnziel in Instrumentenpunkten (multipliziert mit `PriceStep`). |
| `MomentumReference` | `decimal` | `100` | Neutrales Momentumniveau, kopiert von der MQL-Strategie. |

## Hinweise zur Implementierung
- Der gleitende Durchschnitt wird mit `LengthIndicator<decimal>` Instanzen implementiert, um die integrierten SMA/EMA/SMMA/WMA-Klassen von StockSharp wiederzuverwenden.
- Die ursprüngliche Orderwarteschlange und die Magic-Number-Filter werden auf Nettopositionen von StockSharp abgebildet. Daher sendet die Strategie eine einzelne Marktorder in der Größe, um sowohl die Gegenseite abzuflachen als auch das neue Risiko zu eröffnen, wenn `CloseOpposite` aktiviert ist.
- Der Aktienschutz schließt alle Positionen über `CloseAll()`, sobald der variable Verlust den Schwellenwert überschreitet, was genau dem MetaTrader-Verhalten der Überwachung der kombinierten Provision, des Swaps und des Gewinns entspricht.
