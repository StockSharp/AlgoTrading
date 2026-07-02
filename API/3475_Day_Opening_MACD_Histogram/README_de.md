# Tageseröffnung MACD Histogrammstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den MetaTrader-Experten „2 1000 1 0,7 % 0,5 500lev st“, indem sie zu Beginn jedes neuen Handelstages einen Trade eingibt und die Richtung mit der MACD-Histogrammsteigung filtert. Das System wurde für stündliche Kerzen entwickelt und basiert auf festen Geldverwaltungsparametern, die aus den ursprünglichen MQL-Einstellungen konvertiert wurden.

## Handelslogik
- Die Strategie überwacht stündliche Kerzen und erkennt die erste Kerze jedes neuen Tages.
- Es wertet das MACD-Histogramm der beiden zuletzt abgeschlossenen Kerzen des Vortages aus.
- Wenn das Histogramm zwischen diesen beiden Balken sinkt, eröffnet das System eine Long-Position bei der ersten Kerze des neuen Tages.
- Wenn das Histogramm zunimmt, wird stattdessen eine Short-Position eröffnet.
- Es kann jeweils nur eine Position aktiv sein. Gegensätzliche Signale schließen den aktuellen Handel ab, bevor sie die neue Richtung eröffnen.

## Risikomanagement
- Anfängliche Stop-Loss-Distanz: 875 Punkte (umgerechnet in den Preis durch Multiplikation mit der Preisstufe des Instruments).
- Take-Profit-Distanz: 510 Punkte.
- Trailing-Stop-Distanz: 2172 Punkte. Der Stop folgt dem höchsten (Long) oder niedrigsten (Short) Preis, der seit dem Einstieg erreicht wurde, und überschreibt den anfänglichen Stop, wenn dieser enger wird.
- Die ursprüngliche Break-Even-Option wurde deaktiviert und daher hier weggelassen.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `CandleType` | Von der Strategie verwendete Kerzenserie (standardmäßig stündlich). | 1 Stunde Kerzen |
| `MacdFastPeriod` | Schneller Zeitraum von EMA für den MACD. | 58 |
| `MacdSlowPeriod` | Langsamer Zeitraum von EMA für den MACD. | 195 |
| `MacdSignalPeriod` | Signalleitungsperiode für MACD. | 183 |
| `StopLossPoints` | Stop-Loss-Distanz, ausgedrückt in Instrumentenpunkten. | 875 |
| `TakeProfitPoints` | Take-Profit-Distanz in Punkten. | 510 |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Punkten. | 2172 |

## Notizen
- Die Strategie verwendet nur abgeschlossene Kerzen, um einen Look-Ahead innerhalb des Balkens zu vermeiden, und spiegelt die Option „Vorherigen Balkenwert verwenden“ des Quellexperten wider.
- Trailing- und Fixed-Exits werden intern gehandhabt, daher sollten zusätzliche Portfolioschutzmaßnahmen deaktiviert bleiben, um eine doppelte Handhabung von Stops zu verhindern.
- Die Logik geht davon aus, dass der Broker Standardpunktdefinitionen (Preisschritt) verwendet. Passen Sie die Parameter an, wenn das Instrument eine andere Tick-Größe verwendet.
