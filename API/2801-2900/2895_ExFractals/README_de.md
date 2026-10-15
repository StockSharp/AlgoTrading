# ExFractals-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die ExFractals-Strategie ist ein Ausbruchssystem, das Williams-ähnliche Fraktalniveaus mit dem ExVol-Durchschnittskörper-Momentumfilter kombiniert. Der Algorithmus überwacht kontinuierlich die jüngsten bestätigten fraktalen Hochs und Tiefs, mittelt sie paarweise und eröffnet Trades, wenn der Kurs über diese gemittelten Niveaus hinaus schließt, während die ExVol-Messung die Richtung der Bewegung bestätigt.

## Handelslogik

1. **Fraktalerkennung**
   - Kerzen werden verarbeitet, sobald sie schließen.
   - Aufwärts- (bärische) und Abwärts- (bullische) Fraktale werden erkannt, sobald die mittlere Kerze in einem Fünf-Kerzen-Fenster ein striktes Extrem im Vergleich zu ihren Nachbarn ist.
   - Die Strategie speichert die zwei neuesten bestätigten Fraktale pro Seite zusammen mit ihren Zeitstempeln.
   - Jede Seite erzeugt ein handelbares Niveau gleich dem Durchschnitt der letzten zwei Fraktalpreise. Doppelte Zeitstempel werden ignoriert, um zu verhindern, dasselbe Fraktal zweimal zu verwenden.
2. **ExVol-Filter**
   - Der ExVol-Wert entspricht dem einfachen Durchschnitt des Kerzenkörpers (Schluss minus Eröffnung) ausgedrückt in Kursschritten während des ausgewählten Lookback-Zeitraums.
   - Ein negativer ExVol zeigt anhaltende bullische Kerzen (positiver Schluss-zu-Eröffnung), und ein positiver ExVol zeigt anhaltende bärische Kerzen an.
3. **Einstiegsbedingungen**
   - **Long:** Der letzte Schlusskurs liegt über dem gemittelten oberen Fraktalniveau und ExVol ist negativ. Jede aktive Short-Position wird geschlossen und eine neue Long-Position wird eröffnet.
   - **Short:** Der letzte Schlusskurs liegt unter dem gemittelten unteren Fraktalniveau und ExVol ist positiv. Jede aktive Long-Position wird geschlossen und eine neue Short-Position wird eröffnet.
4. **Risiko- und Ausstiegsregeln**
   - Feste Stop-Loss- und Take-Profit-Ziele werden bei konfigurierbaren Pip-Abständen vom Einstiegspreis platziert.
   - Optionale Trailing-Stops bewegen sich erst, nachdem der Trade mindestens `Trailing Stop + Trailing Step` Pips gewonnen hat. Der Stop wird hoch/runter gezogen, um einen konstanten Trailing-Abstand beizubehalten, während der minimale Trailing-Schritt eingehalten wird.
   - Wenn der Kurs den Stop-Loss oder Take-Profit trifft, wird die gesamte Position geschlossen.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ------------ | -------- |
| `Candle Type` | Kerzendatentyp/-zeitrahmen der Strategie. | 1-Stunden-Zeitrahmen |
| `ExVol Period` | Anzahl der geschlossenen Kerzen für den Kerzenkörperdurchschnitt (ExVol). | 15 |
| `Stop Loss` | Stop-Loss-Abstand in Pips vom Einstiegspreis. Auf `0` setzen, um zu deaktivieren. | 40 |
| `Take Profit` | Take-Profit-Abstand in Pips vom Einstiegspreis. Auf `0` setzen, um zu deaktivieren. | 100 |
| `Trailing Stop` | Trailing-Stop-Abstand in Pips. Auf `0` setzen, um Trailing zu deaktivieren. | 30 |
| `Trailing Step` | Zusätzliche Preisbewegung (in Pips) erforderlich, bevor der Trailing-Stop bewegt wird. Muss positiv sein, wenn Trailing aktiviert ist. | 5 |
| `Volume` | Standard-Ordervolumen aus der Basisklasse `Strategy`. | 1 |

## Zusätzliche Hinweise

- Die Trailing-Logik spiegelt die MetaTrader-Implementierung wider: Der Stop wird erst angepasst, wenn die Position mindestens `TrailingStop + TrailingStep` Pips im Gewinn ist.
- ExVol-Berechnungen basieren auf dem `PriceStep` des Instruments; wenn der Schritt nicht verfügbar ist, wird ein Standardwert von 0.0001 verwendet.
- Die Strategie gibt Marktaufträge über `BuyMarket` und `SellMarket` aus und kehrt automatisch jede bestehende Position um, bevor eine neue eröffnet wird.
- Sicherstellen, dass der Daten-Feed genügend historische Kerzen bereitstellt, um die anfänglichen Fraktalpaare zu bilden (mindestens fünf geschlossene Kerzen).
