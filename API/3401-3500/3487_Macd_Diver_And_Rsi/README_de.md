# MACD Taucher und RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine C#-Konvertierung des **"Macd Diver and RSI"** MetaTrader 5 Expert Advisors. Es bleibt bei der ursprünglichen zweistufigen Signalidee: Der Relative Strength Index (RSI) erkennt überverkaufte oder überkaufte Extreme, während das MACD-Histogramm bestätigt, dass sich die Dynamik wieder in Richtung des Handels dreht. Long- und Short-Seiten werden unabhängig voneinander konfiguriert, sodass das Verhalten separat auf bullische und bärische Setups abgestimmt werden kann.

Die Strategie basiert auf einem Einzelkerzenabonnement (konfigurierbarer Zeitrahmen) und handelt das gecharterte Wertpapier direkt über Marktaufträge. Die gesamte Indikatorverarbeitung verwendet die übergeordnete StockSharp API über `BindEx`, entsprechend den Projektregeln.

## Handelslogik

1. **Indikatorvorbereitung**
   - Es werden zwei RSI-Indikatoren erstellt, einer für den langen Zweig und einer für den kurzen Zweig, mit individuellen Längen und Schwellenwerten.
   - Zwei `MovingAverageConvergenceDivergenceSignal`-Indikatoren spiegeln die MACD-Einstellungen für Long- und Short-Trades wider. Ihre Histogrammkomponente wird zur Bestätigung von Impulsumkehrungen verwendet.

2. **Eintrittsregeln**
   - **Long-Setup**: Wenn der Long-RSI-Wert bei oder unter dem überverkauften Schwellenwert liegt *und* das Long-MACD-Histogramm über Null geht (das Vorzeichen von negativ zu positiv ändert), wird eine bullische Position eröffnet. Wenn eine Short-Position aktiv ist, wird sie in derselben Marktorder geschlossen und rückgängig gemacht.
   - **Short-Setup**: Wenn der Short-RSI-Wert bei oder über der überkauften Schwelle liegt *und* das Short-MACD-Histogramm unter Null fällt, wird eine rückläufige Position eröffnet. Das bestehende Long-Engagement wird abgeflacht, bevor das neue Short-Engagement etabliert wird.

3. **Risikomanagement**
   - Nach jedem Einstieg zeichnet die Strategie den Schlusskurs des Signalbalkens als Referenzpreis auf.
   - Stop-Loss- und Take-Profit-Niveaus werden anhand dieses Preises anhand von Pip-Abständen prognostiziert, die für Long- und Short-Trades separat definiert werden.
   - Pips werden mit dem Instrument `PriceStep` in Preiseinheiten umgewandelt und für Symbole mit 3 oder 5 Dezimalstellen automatisch um 10 skaliert, um das MT5-Verhalten widerzuspiegeln.
   - Bei jeder abgeschlossenen Kerze wird der Hoch-/Tief-Bereich mit diesen Niveaus verglichen. Bei Erreichen eines dieser Level wird die Position sofort mit einer Marktorder geschlossen.

4. **Handelsmanagement**
   - Der Positionsstatus wird gelöscht, wenn die Positionsgröße auf Null zurückkehrt (entweder weil ein Stop/Take-Profit erreicht wurde oder die Strategie durch ein entgegengesetztes Signal umgekehrt wurde).
   - Es werden keine teilweisen Exits oder nachgestellten Anpassungen durchgeführt; Die Positionssteuerung erfolgt ausschließlich über die statischen Stop-Loss- und Take-Profit-Level.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Zeitrahmen des für Signale verwendeten Kerzenabonnements. |
| `LongRsiPeriod`, `ShortRsiPeriod` | RSI Längen für lange und kurze Erkennung. |
| `LongRsiThreshold`, `ShortRsiThreshold` | RSI Schwellenwerte, die Einstiege ermöglichen (überverkauft für Long-Positionen, überkauft für Short-Positionen). |
| `LongMacdFastLength`, `LongMacdSlowLength`, `LongMacdSignalLength` | MACD EMA Längen für das bullische Bein. |
| `ShortMacdFastLength`, `ShortMacdSlowLength`, `ShortMacdSignalLength` | MACD EMA Längen für das bärische Bein. |
| `LongVolume`, `ShortVolume` | Handelsvolumen pro Signal. Beim Umkehren addiert die Strategie das absolute Eröffnungsvolumen, sodass die einzelne Order den Abschluss und die neue Eröffnung durchführt. |
| `LongStopLossPips`, `LongTakeProfitPips`, `ShortStopLossPips`, `ShortTakeProfitPips` | Abstand von Stop-Loss- und Take-Profit-Orders in Pips. Null deaktiviert die jeweilige Stufe. |

## Notizen

- Die Strategie erfordert Instrumente mit einem `PriceStep` ungleich Null. Wenn der Schritt fehlt, fällt die Pip-Berechnung auf 0,0001 zurück, um eine Division durch Null zu verhindern.
- Da beide Seiten unabhängige Indikatorinstanzen verwenden, können Sie bullisches und bärisches Verhalten separat anpassen, indem Sie beispielsweise die überkaufte Schwelle verschärfen und gleichzeitig die überverkaufte Seite freizügiger halten.
- Der Code fügt englische Kommentare und Dokumentation hinzu, um den Handelsprozess zu verdeutlichen und die Projektrichtlinien zu erfüllen.
