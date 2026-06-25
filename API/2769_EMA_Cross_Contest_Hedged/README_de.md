# EMA Kreuzung Wettbewerb Abgesichert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Recreiert die MetaTrader-Strategie "EMA Cross Contest Hedged" mit der StockSharp High-Level-API.
- Handelt ein Paar exponentieller gleitender Durchschnitte (EMA) und bestätigt optional mit der MACD-Hauptlinie.
- Baut nach jedem Einstieg eine Leiter ausstehender Stop-Orders ("Hedge"-Levels) auf, um in starke Trends zu skalieren.
- Wendet statische Stop-Loss/Take-Profit-Niveaus in Pips und einen Trailing Stop an, der nach einem Mindestgewinn aktiviert wird.
- Erlaubt die Wahl, ob Signale die aktuelle abgeschlossene Kerze oder die vorherige geschlossene Kerze verwenden sollen.

## Indikatoren und Daten
- Kurze EMA mit konfigurierbarer Länge (Standard 4).
- Lange EMA mit konfigurierbarer Länge (Standard 24); die kurze Periode muss unterhalb der langen Periode bleiben.
- MACD (4, 24, 12) Hauptlinie als optionaler Bestätigungsfilter.
- Funktioniert auf jedem Zeitrahmen, der vom `CandleType`-Parameter geliefert wird (Standard 15-Minuten-Kerzen).

## Einstiegslogik
1. Warten auf eine fertige Kerze des konfigurierten Zeitrahmens.
2. Die schnellen und langsamen EMA-Werte berechnen. Abhängig von `TradeBar` die Kreuzung bestimmen mit:
   - Der neuesten und der vorherigen fertigen Kerze (`Current`).
   - Der vorherigen und der noch früheren Kerze (`Previous`, Standard).
3. Ein Long-Signal generieren, wenn die schnelle EMA über die langsame EMA kreuzt. Wenn `UseMacdFilter` aktiviert ist, muss der MACD-Wert für dieselbe Bar nicht-negativ sein.
4. Ein Short-Signal generieren, wenn die schnelle EMA unter die langsame EMA kreuzt. Mit aktiviertem MACD-Filter muss der MACD-Wert nicht-positiv sein.
5. Nur eine neue Position eröffnen, wenn keine Exposition vorhanden ist (alle vorherigen Trades sind flat).
6. Marktorders mit Größe `OrderVolume` ausführen. Nach einem Einstieg:
   - Speichert die Strategie Stop-Loss- und Take-Profit-Niveaus, die um `StopLossPips` und `TakeProfitPips` vom Ausführungspreis versetzt sind.
   - Setzt den Trailing-Stop-Zustand zurück.
   - Erstellt vier Hedging-Stop-Orders im Abstand von `HedgeLevelPips` in der Handelsrichtung. Jede ausstehende Order erbt dieselbe Stop-Loss/Take-Profit-Distanz und läuft nach `PendingExpirationSeconds` Sekunden ab, es sei denn, der Preis erreicht sie früher.

## Ausstiegsverwaltung
- **Stop-Loss / Take-Profit:** Die Strategie überwacht innerbar Hochs und Tiefs. Wenn der Preis den gespeicherten Stop oder das Ziel berührt, wird die gesamte Position geschlossen.
- **Trailing Stop:** Wenn der Gewinn `TrailingStopPips + TrailingStepPips` überschreitet, wird der Stop auf `TrailingStopPips` hinter dem letzten Schlusskurs nachgezogen. Long-Positionen ziehen nach oben, Short-Positionen nach unten.
- **Gegenteilige Kreuzung:** Wenn `CloseOppositePositions` aktiviert ist, wird die Position sofort geschlossen, sobald die entgegengesetzte EMA-Kreuzung erkannt wird.
- **Ausstehende Leiter:** Jede Hedging-Order wird zu einer zusätzlichen Marktorder, sobald der Preis das Stop-Niveau überschreitet. Neue Ausführungen passen den durchschnittlichen Einstiegspreis und die Schutz-Niveaus entsprechend an.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `OrderVolume` | 0.1 | Ordergröße für jede Markt- oder Stop-Order. |
| `StopLossPips` | 140 | Stop-Abstand in Pips. Auf 0 setzen zum Deaktivieren. |
| `TakeProfitPips` | 120 | Take-Profit-Abstand in Pips. Auf 0 setzen zum Deaktivieren. |
| `TrailingStopPips` | 30 | Trailing-Stop-Abstand in Pips. Auf 0 setzen zum Deaktivieren. |
| `TrailingStepPips` | 1 | Minimaler zusätzlicher Gewinn (in Pips) bevor der Trailing Stop erneut anzieht. |
| `HedgeLevelPips` | 6 | Abstand zwischen den gestaffelten Hedging-Stop-Orders. |
| `CloseOppositePositions` | false | Aktive Position schließen, wenn eine gegenteilige Kreuzung erscheint. |
| `UseMacdFilter` | false | MACD-Bestätigung erfordern (>= 0 für Longs, <= 0 für Shorts). |
| `PendingExpirationSeconds` | 65535 | Lebensdauer jeder Hedging-Stop-Order in Sekunden. |
| `ShortMaPeriod` | 4 | Kurze EMA-Länge. Muss kleiner als `LongMaPeriod` sein. |
| `LongMaPeriod` | 24 | Lange EMA-Länge. |
| `TradeBar` | Previous | Bestimmt, welches Balkenpaar zur Erkennung der Kreuzung verwendet wird. |
| `CandleType` | 15 Minuten | Beim Datenanbieter angeforderter Zeitrahmen. |

## Zusätzliche Hinweise
- Pips werden durch Multiplikation von `Security.PriceStep` umgerechnet und für 3- und 5-Dezimal-Instrumente automatisch mit dem Faktor 10 multipliziert, um den MetaTrader-Pip-Konventionen zu entsprechen.
- Ausstehende Hedging-Orders werden innerhalb der Strategie simuliert und ausgeführt, sobald der Kerzenbereich ihr Niveau berührt.
- `StartProtection()` wird aufgerufen, um die integrierten StockSharp-Positionsschutz-Dienste zu aktivieren.
- Die Strategie pflegt separate Trailing-Stop-Logik für Long- und Short-Positionen, um die ursprüngliche abgesicherte Implementierung zu spiegeln.
