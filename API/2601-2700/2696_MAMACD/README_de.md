# MAMACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist eine direkte Konvertierung des MetaTrader 5 Expert Advisors **MAMACD (Edition von barabashkakvn)** aus dem Ordner `MQL/19334` in die High-Level-API von StockSharp. Der Ansatz kombiniert Trenderkennung auf Tiefpreisen durch zwei linear gewichtete gleitende Durchschnitte (LWMA) mit einem schnellen exponentiellen gleitenden Durchschnitt (EMA) als Auslöser und Bestätigung durch die MACD-Hauptlinie. Trades werden einmal pro abgeschlossener Kerze ausgeführt und behalten die Logik des Original-EAs bei, einschließlich der Reset-Flags, die erfordern, dass die schnelle EMA den LWMA-Kanal verlässt, bevor ein neuer Einstieg erlaubt wird.

## Indikatoren
- **LWMA #1 (Tiefstkurs, Standard 85)** – langsamer Basislinienfilter, angewendet auf Kerzentiefs.
- **LWMA #2 (Tiefstkurs, Standard 75)** – etwas schnellerer Filter auf Kerzentiefs zur Kanalbestätigung.
- **EMA-Auslöser (Schlusskurs, Standard 5)** – Momentum-Auslöser, der über/unter beide LWMAs kreuzen muss, um einen Trade zu aktivieren.
- **MACD-Hauptlinie (schnell 15, langsam 26)** – Bestätigungsfilter; Long-Positionen erfordern positiven oder steigenden MACD, Short-Positionen erfordern negativen oder fallenden MACD.

## Einstiegslogik
1. Die Strategie wartet nur auf abgeschlossene Kerzen (`CandleStates.Finished`).
2. Wenn der Auslöser-EMA unter beide LWMAs fällt, wird ein **Long-Bereitschafts-Flag** gesetzt. Eine Long-Position kann eröffnet werden, sobald der EMA über beide LWMAs zurückkehrt **und** der MACD entweder über null liegt oder größer als sein vorheriger Wert ist. Es kann jeweils nur eine Long-Position eröffnet werden.
3. Wenn der Auslöser-EMA über beide LWMAs steigt, wird ein **Short-Bereitschafts-Flag** gesetzt. Eine Short-Position kann eröffnet werden, nachdem der EMA unter beide LWMAs zurückkehrt und der MACD entweder unter null liegt oder kleiner als sein vorheriger Wert ist. Es ist jeweils nur eine Short-Position aktiv.
4. Die Positionsgröße verwendet die `Volume`-Eigenschaft der Strategie. Beim Richtungswechsel schließt der Algorithmus zuerst die entgegengesetzte Exposition.

## Ausstiegslogik
- Im Original-EA ist keine diskretionäre Ausstiegslogik kodiert. Schutzaufträge werden über StockSharp's `StartProtection` mit optionalen Stop-Loss- und Take-Profit-Abständen in Pips verwaltet. Das Erreichen eines der Schutzniveaus schließt die Position automatisch.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `FirstLowMaLength` | Periode des ersten LWMA, angewendet auf Tiefpreise (Standard 85). |
| `SecondLowMaLength` | Periode des zweiten LWMA, angewendet auf Tiefpreise (Standard 75). |
| `TriggerEmaLength` | Periode des schnellen EMA-Auslösers auf Schlusspreise (Standard 5). |
| `MacdFastLength` | Länge des schnellen EMA der MACD-Hauptlinie (Standard 15). |
| `MacdSlowLength` | Länge des langsamen EMA der MACD-Hauptlinie (Standard 26). |
| `StopLossPips` | Stop-Loss-Abstand in Pips; auf null setzen zum Deaktivieren (Standard 15). |
| `TakeProfitPips` | Take-Profit-Abstand in Pips; auf null setzen zum Deaktivieren (Standard 15). |
| `CandleType` | Zeitrahmen der von der Strategie verarbeiteten Kerzen (Standard 1 Stunde). |

## Implementierungshinweise
- Die Pip-Größe wird aus `Security.PriceStep` abgeleitet. Bei 3- und 5-stelligen Symbolen multipliziert der Code den Schritt automatisch mit 10, um die MT5-Definition eines Pips nachzuahmen.
- Der MACD-Verlaufspuffer entspricht dem EA: Der erste gültige MACD-Wert wird gespeichert und als Referenz für den folgenden Balken verwendet, bevor Signale ausgewertet werden.
- Die Flags `_readyForLong` und `_readyForShort` replizieren die ursprüngliche `startb`/`starts`-Zustandsmaschine und stellen sicher, dass der Kurs den LWMA-Kanal verlassen muss, bevor ein neuer Trade eingegangen wird.
- Chartbereiche visualisieren die Preisserie mit gleitenden Durchschnitten und ein separates MACD-Panel zur einfacheren Überprüfung der Konvertierung.

## Konvertierungszuordnung
| MT5-Element | StockSharp-Äquivalent |
| --- | --- |
| `iMA` auf Tief/Schluss | `WeightedMovingAverage` (Tief-Feed) und `ExponentialMovingAverage` (Schluss-Feed) |
| `iMACD`-Hauptlinie | `MovingAverageConvergenceDivergence`-Hauptausgabe |
| Positionsprüfungen (`buy`, `sell`) | `Position`-Vorzeichen und Volumenbehandlung über `BuyMarket` / `SellMarket` |
| Magic Number & Slippage | Nicht erforderlich in der StockSharp High-Level-API |
| Stop-Loss / Take-Profit (Pips) | `StartProtection` mit absoluten Preisoffsets, berechnet aus der Pip-Größe |

Das resultierende Verhalten spiegelt die MT5-Version wider und nutzt gleichzeitig den Strategie-Lebenszyklus, die Indikatorbindung und die Risikomanagement-Hilfsmittel von StockSharp.
