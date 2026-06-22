# Evening Star Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein direkter Port des Expert Advisors **EveningStar.mq5** (MQL5-ID 18507). Sie überwacht die klassische Evening-Star-Kerzenformation und eröffnet eine Position, sobald der nächste Bar zu handeln beginnt. Die Logik wurde auf der High-Level-API von StockSharp neu geschrieben, während die ursprünglichen Musterfilter und das Risikomanagement beibehalten wurden.

## Handelslogik
1. Die Strategie abonniert den durch den Parameter `CandleType` gewählten Zeitrahmen. Die gesamte Verarbeitung erfolgt nur bei abgeschlossenen Kerzen.
2. Jedes Mal, wenn eine neue Kerze schließt, werden die letzten Snapshots gecacht, damit das durch `Shift` definierte Drei-Kerzen-Fenster ausgewertet werden kann.
3. Das Evening-Star-Muster gilt als gültig, wenn:
   - Kerze *N-2* (älteste) bullisch ist (`open < close`).
   - Kerze *N-1* (mittlere) die Präferenz `Candle2Bullish` erfüllt (standardmäßig bullisch).
   - Kerze *N* (aktuellste) bärisch ist (`open > close`).
   - Wenn `CheckCandleSizes` aktiviert ist, muss die mittlere Kerze den kleinsten Körper der drei haben.
   - Wenn `ConsiderGap` aktiviert ist, muss es einen Gap zwischen den Kerzenkörpern geben, wie im originalen Roboter (Gapgröße entspricht einem Pip, berechnet aus dem Preisschritt des Instruments).
4. Sobald das Muster bestätigt ist, prüft die Strategie die durch `Direction` gewählte Richtung:
   - `Short` (Standard) eröffnet eine Verkaufsorder, was dem ursprünglichen Evening-Star-Verhalten entspricht.
   - `Long` ermöglicht das Fahren der genau entgegengesetzten Exposure (für Feature-Parität mit der MQL-Version beibehalten).
5. Vor dem Eröffnen einer Position schließt der Algorithmus optional die entgegengesetzte Exposure, wenn `CloseOppositePositions` auf `true` gesetzt ist.
6. Stop-Loss- und Take-Profit-Preise werden aus den Pip-Abständen (`StopLossPips`, `TakeProfitPips`) mit derselben 3/5-Ziffern-Anpassung berechnet, die in MetaTrader vorhanden war.
7. Die Positionsgröße wird aus dem aktuellen Portfoliowert und `RiskPercent` abgeleitet. Wenn das berechnete Volumen kleiner als die minimale handelbare Größe ist, wird das Signal ignoriert.

## Positionsmanagement
- Wenn eine Long-Position aktiv ist, überwacht die Strategie jede neue Kerze. Wenn der Tiefstkurs das Stop-Niveau unterschreitet oder der Höchstkurs das Take-Profit-Niveau erreicht, wird die gesamte Position zum Marktpreis geschlossen.
- Wenn eine Short-Position aktiv ist, wird dieselbe Logik mit umgekehrten Vergleichen angewendet.
- Wenn der Portfoliowert oder der Stop-Abstand null ist, kann die Ordergröße nicht berechnet werden, daher wird der Einstieg übersprungen.

## Parameter
| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `Direction` | `Short` | Wählt, ob das Muster eine Long- oder Short-Position eröffnen soll. |
| `TakeProfitPips` | `150` | Abstand zum Gewinnziel in Pips. Auf null setzen, um zu deaktivieren. |
| `StopLossPips` | `50` | Abstand zum Schutz-Stop in Pips. Ein nicht-positiver Wert deaktiviert den Trade. |
| `RiskPercent` | `5` | Prozentsatz des Portfolio-Eigenkapitals, das pro Trade riskiert wird. Wird zur Berechnung des Ordervolumens verwendet. |
| `Shift` | `1` | Anzahl der Bars, die von der aktuellsten Kerze übersprungen werden, bevor das Muster ausgewertet wird. |
| `ConsiderGap` | `true` | Erfordert einen Gap zwischen Kerzenkörpern, genau wie der originale Expert Advisor. |
| `Candle2Bullish` | `true` | Zwingt die mittlere Kerze, bullisch zu sein. Deaktivieren, um eine bärische mittlere Kerze zu fordern. |
| `CheckCandleSizes` | `true` | Stellt sicher, dass die mittlere Kerze den kleinsten absoluten Körper hat. |
| `CloseOppositePositions` | `true` | Schließt die entgegengesetzte Exposure, bevor die neue Order gesendet wird. |
| `CandleType` | `1H`-Zeitrahmen | Kerzenserie für die Analyse. |

## Hinweise
- Die Pip-Größe wird aus dem Preisschritt des Instruments abgeleitet. Für 3- und 5-stellige Forex-Symbole entspricht ein Pip zehn Preisschritten, was das Verhalten des originalen EA reproduziert.
- Wenn `StopLossPips` null ist, kann die Positionsgröße nicht berechnet werden, und das Signal wird ignoriert, um unbegrenzte Risiken zu verhindern.
- Die Strategie kürzt den gecachten Verlauf automatisch, sodass der Speicherverbrauch auch bei langen Sitzungen konstant bleibt.
