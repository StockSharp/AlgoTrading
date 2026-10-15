# Strategie Zwei Pending Orders Öffnen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie repliziert den MetaTrader Expert Advisor, der gleichzeitig eine Buy-Stop- und eine Sell-Stop-Order rund um den aktuellen Spread platziert. Sie arbeitet auf einem einzelnen Instrument und verwendet High-Level StockSharp API-Aufrufe, um das Order Book zu abonnieren, Pending Orders zu verwalten und Portfolio-Risikokontrollen zu handhaben. Sobald eine Pending Order gefüllt wird, wird die entgegengesetzte Order storniert und die aktive Position wird mit Stop-Loss-, Take-Profit- und Trailing-Stop-Regeln verwaltet.

## Handelslogik
1. Das Order Book abonnieren und die besten Bid- und Ask-Preise lesen.
2. Wenn keine offene Position oder aktive Einstiegsorder vorhanden ist, das Einstiegsvolumen berechnen und zwei Stop-Orders platzieren:
   - Buy Stop bei *Ask + EntryOffsetPoints × PriceStep*.
   - Sell Stop bei *Bid − EntryOffsetPoints × PriceStep*.
3. Wenn eine Stop-Order ausgeführt wird:
   - Die entgegengesetzte Pending Order stornieren.
   - Den Ausführungspreis als neuen Eintrittspreis speichern.
   - Die initialen Stop-Loss- und Take-Profit-Niveaus in Kursschritten relativ zur Füllung berechnen.
4. Während die Position aktiv ist, das Order Book überwachen:
   - Longs schließen, wenn der Bid das Stop-Loss- oder Take-Profit-Niveau erreicht.
   - Shorts schließen, wenn der Ask das Stop-Loss- oder Take-Profit-Niveau erreicht.
   - Den Trailing Stop aktivieren, nachdem sich der Preis um die Trailing-Distanz zugunsten des Trades bewegt hat, und das Stop-Niveau entsprechend verschieben.
5. Wenn die Position zu flach zurückkehrt, den internen Status zurücksetzen und ein frisches Paar Stop-Orders platzieren.

Ausstiege werden mit Marktorders ausgeführt, sobald ein Schutzniveau berührt wird. Dies hält die Logik nahe an der MQL-Implementierung, ohne auf Lower-Level Order-Modifikations-APIs angewiesen zu sein.

## Geldverwaltung
Die Strategie kann entweder ein festes Volumen oder dynamisches risikobasiertes Sizing verwenden:
- **Festes Volumen** – die konstante Lotgröße verwenden, die durch den Parameter `FixedVolume` definiert wird.
- **Geldverwaltung** – wenn aktiviert, das Volumen aus dem Portfolio-Eigenkapital, dem Risikoprozentsatz und dem Stop-Loss-Abstand in Kursschritten berechnen. Volumina werden auf den Volumen-Schritt des Instruments gerundet und zwischen Minimum- und Maximumwerten des Instruments begrenzt.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `UseMoneyManagement` | Aktiviert risikobasiertes Positionssizing. Standard: `true`. |
| `RiskPercent` | Prozentsatz des Portfolio-Eigenkapitals, der pro Trade riskiert wird, wenn die Geldverwaltung aktiv ist. Standard: `2`. |
| `FixedVolume` | Lotgröße, die verwendet wird, wenn die Geldverwaltung deaktiviert ist. Standard: `1`. |
| `StopLossPoints` | Stop-Loss-Abstand in Kursschritten vom Eintrittspreis. Standard: `100`. |
| `TakeProfitPoints` | Take-Profit-Abstand in Kursschritten vom Eintrittspreis. Standard: `300`. |
| `TrailingStopPoints` | Trailing-Stop-Abstand in Kursschritten. Ein Wert von `0` deaktiviert den Trailing. Standard: `50`. |
| `EntryOffsetPoints` | Abstand in Kursschritten, der verwendet wird, um die Pending Orders vom Spread entfernt zu platzieren. Standard: `50`. |
| `SlippagePoints` | Zusätzlicher Puffer in Kursschritten für Slippage reserviert. Derzeit informativ und nicht direkt verwendet. Standard: `5`. |

## Hinweise
- Die Strategie ist auf den Order Book-Feed angewiesen. Stellen Sie sicher, dass Markttiefe-Daten für das ausgewählte Instrument verfügbar sind.
- Die Stop-Loss- und Take-Profit-Ausführung verwendet Marktorders, sobald der Bid/Ask das Niveau kreuzt, und entspricht dem Verhalten der ursprünglichen MQL-Trailing-Stop-Logik.
- Trailing Stops beginnen erst, nachdem sich der Preis um die konfigurierte Trailing-Distanz vom Einstieg bewegt hat.
- Der Code verwendet Tabulatoreinrückung, englische Kommentare und High-Level StockSharp-Methoden gemäß den Projektrichtlinien.
