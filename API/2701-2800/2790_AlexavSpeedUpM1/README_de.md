# Alexav SpeedUp M1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des Expert Advisors "Alexav SpeedUp M1" aus MetaTrader 5 in die StockSharp High-Level-API.
- Entwickelt für schnelle Märkte im 1-Minuten-Zeitrahmen (Standard) und reagiert auf ungewöhnlich große Kerzenkörper.
- Eröffnet eine einzige Nettoposition in Richtung des starken Kerzenkörpers und verwaltet sie mit festem Stop-Loss, Take-Profit und einem gestuften Trailing-Stop.
- Verwendet pip-basierte Eingaben, die automatisch nach der Tick-Größe des Instruments und der Dezimalgenauigkeit in Preisabstände umgerechnet werden.

## Ursprüngliche Idee vs. StockSharp-Implementierung
- Der ursprüngliche EA öffnete auf Hedging-Konten gleichzeitig Long- und Short-Trades. StockSharp-Strategien arbeiten in einer Netting-Umgebung, daher hält dieser Port nur eine Position gleichzeitig und tritt in Richtung der großen Kerze ein.
- Die Trailing-Stop-Logik folgt der MT5-Version: Sie wartet, bis der Preis sich um `TrailingStop + TrailingStep` bewegt hat, bevor der Stop um die Trailing-Distanz näher gerückt wird, und aktualisiert nur, wenn der Preis mindestens einen Trailing-Step über den vorherigen Stop hinaus vorrückt.
- Pip-Abstände werden in Preiseinheiten umgerechnet, indem mit der minimalen Tick-Größe multipliziert wird. Für Forex-Symbole mit 3 oder 5 Dezimalstellen multipliziert der Code den Tick mit 10, um die MT5-Pip-Behandlung zu emulieren.

## Einstiegsregeln
1. Mit abgeschlossenen Kerzen des ausgewählten Zeitrahmens arbeiten (Standard: 1 Minute).
2. Den Kerzenkörper messen: `abs(Close - Open)`.
3. Wenn der Körper `MinimumBodySizePips * pipSize` überschreitet und keine aktive Position vorhanden ist, in Richtung des Kerzenkörpers einsteigen:
   - Bullische Kerze → Long-Position eröffnen.
   - Bärische Kerze → Short-Position eröffnen.

## Ausstiegsregeln
- **Stop-Loss** – platziert `StopLossPips * pipSize` vom Einstiegspreis entfernt. Deaktiviert, wenn der Parameter null ist.
- **Take-Profit** – platziert `TakeProfitPips * pipSize` vom Einstieg entfernt. Deaktiviert, wenn der Parameter null ist.
- **Trailing-Stop** – aktiviert, wenn `TrailingStopPips > 0` und `TrailingStepPips > 0`.
  - Wird aktiviert, nachdem der Trade mindestens `TrailingStopPips + TrailingStepPips` Pips gewonnen hat.
  - Bei Long-Trades wird der Stop auf `Close - TrailingStopPips * pipSize` verschoben, wenn die Bedingung erfüllt ist und der Preis mindestens einen Trailing-Step über den vorherigen Stop hinaus vorrückte.
  - Bei Short-Trades wird der Stop mit derselben Schritt-Bedingung auf `Close + TrailingStopPips * pipSize` verschoben.

## Parameter
- `OrderVolume` – Handelsgröße in Lots (Standard `0.1`).
- `StopLossPips` – Stop-Loss-Abstand in Pips (Standard `30`).
- `TakeProfitPips` – Take-Profit-Abstand in Pips (Standard `90`).
- `TrailingStopPips` – Trailing-Stop-Abstand in Pips (Standard `10`).
- `TrailingStepPips` – Mindest-Vorwärtsbewegung, bevor der Trailing-Stop aktualisiert wird (Standard `5`). Muss größer als null sein, wenn der Trailing-Stop aktiviert ist.
- `MinimumBodySizePips` – Mindest-Körpergröße (in Pips), die zum Auslösen eines Trades erforderlich ist (Standard `100`).
- `CandleType` – Zeitrahmen für Kerzen (Standard `1 Minute`).

## Visualisierung
- Die Strategie zeichnet automatisch die ausgewählte Kerzenserie und eigene Trades im Diagrammbereich, wenn einer verfügbar ist, was die Signalinspektion beim Testen vereinfacht.

## Verwendungshinweise
- Die Standardkonfiguration spiegelt die MT5-Einstellungen wider. Pip-Abstände anpassen, um die Volatilität des gehandelten Instruments zu berücksichtigen.
- Da nur eine Nettoposition unterstützt wird, die Strategie nicht auf Hedging-Konten ausführen, die gleichzeitige Long- und Short-Positionen erwarten.
- Für Märkte mit größeren Tick-Größen die pip-basierten Eingaben entsprechend reduzieren, um vergleichbare Preisabstände beizubehalten.
