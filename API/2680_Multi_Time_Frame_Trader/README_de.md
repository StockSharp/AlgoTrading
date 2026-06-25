# Multi-Zeitrahmen-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie recreiert die originale MQL-Logik des "Multi Time Frame Trader" mit StockSharp High-Level-APIs. Sie kombiniert drei
polynomiale Regressionskanäle (M1, M5 und H1) und handelt nur dann, wenn die niedrigeren Zeitrahmen ihre Kanalextreme in der
durch die stündliche Neigung vorgeschlagenen Richtung testen.

Das System berechnet kontinuierlich die oberen, mittleren und unteren Bänder des Regressionskanals bei jeder abgeschlossenen Kerze neu.
Wenn das stündliche obere Band abnimmt, ist die Tendenz bärisch; wenn es steigt, ist die Tendenz bullisch. Einstiege werden ausgelöst,
sobald die M5- und M1-Kerzen das entsprechende Band erreichen und der Richtungsfilter übereinstimmt.

## Kernarbeitsablauf

- **Abonnements**: Die Strategie hört gleichzeitig auf 1-Minuten-, 5-Minuten- und 1-Stunden-Kerzen.
- **Regressionskanal**: Jedes Abonnement erstellt eine polynomiale Regressionslinie (Grad 1-3) über `Bars` Punkte und versetzt sie um
  `StdMultiplier` Standardabweichungen, um Widerstands- und Stützbänder zu erhalten.
- **Neigungsschätzung**: Die Kanalneigung wird aus der Differenz zwischen dem aktuellen oberen Band und dem oberen Band vor `Bars`
  Kerzen abgeleitet, was das Verhalten des `i-Regr`-Indikators widerspiegelt.
- **Richtungsfilter**: Die H1-Neigung definiert, ob nur Shorts (negative Neigung) oder Longs (positive Neigung) erlaubt sind.

## Einstiegslogik

### Short-Trades

1. Stündliche Neigung ist negativ.
2. Das High der letzten 5-Minuten-Kerze berührt oder durchbricht den 5-Minuten-Regressionswiderstand.
3. Das High der letzten 1-Minuten-Kerze berührt oder durchbricht den 1-Minuten-Widerstand.
4. Keine bestehende Short-Position ist offen (`Position >= 0`).
5. Eine Market-Sell-Order wird gesendet, der Stop-Loss wird eine halbe Kanalbreite über dem Einstieg gesetzt und das Ziel entspricht
   der M5-Mittellinie.

### Long-Trades

1. Stündliche Neigung ist positiv.
2. Das Low der letzten 5-Minuten-Kerze berührt oder durchbricht den 5-Minuten-Regressionssupport.
3. Das Low der letzten 1-Minuten-Kerze berührt oder durchbricht den 1-Minuten-Support.
4. Keine bestehende Long-Position ist offen (`Position <= 0`).
5. Eine Market-Buy-Order wird gesendet, der Stop-Loss wird eine halbe Kanalbreite unter dem Einstieg gesetzt und das Ziel entspricht
   der M5-Mittellinie.

## Ausstiegsregeln

- Stops und Ziele werden intern gespeichert und bei jeder abgeschlossenen M1-Kerze ausgewertet. Wenn die Kerzenbreite das gespeicherte
  Stop-Level kreuzt, wird die Position sofort geschlossen.
- Wenn das Gewinnziel vor dem Stop erreicht wird, wird die Position ebenfalls geschlossen.
- Das Schließen setzt die verfolgten Levels zurück, sodass ein frisches Signal ohne Verzögerung ausgewertet werden kann.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `Degree` | 1 | Polynomgrad des Regressionskanals (1=linear, 2=parabolisch, 3=kubisch). |
| `StdMultiplier` | 2.0 | Multiplikator für die Standardabweichung, die die Bandbreite definiert. |
| `Bars` | 250 | Anzahl der Kerzen für die Regressionsmontage und den Neigungsrückblick. |
| `Shift` | 0 | Horizontale Verschiebung des Regressionsauswertungspunkts (begrenzt zwischen 0 und `Bars - 1`). |
| `UseTrading` | true | Deaktiviert die gesamte Order-Generierung bei `false`, während der Kanal weiterhin aktualisiert wird. |

## Zusätzliche Hinweise

- Die Strategie speichert Stop- und Ziel-Levels lokal, da StockSharp Market Orders nicht automatisch SL/TP-Levels anfügen.
- Sie funktioniert auf jedem Instrument, das Minuten- und Stundenkerzen unterstützt; die ursprüngliche Logik wurde jedoch für
  Forex-Paare entwickelt.
- Passe `Bars` an die Volatilität des gehandelten Instruments an. Ein kleinerer Wert reagiert schneller, ein größerer Wert erzeugt
  glattere Kanäle.
- Setze `Degree` auf 1 für einen geraden Regressionskanal (am nächsten an der klassischen linearen Version), oder verwende höhere
  Grade, um die polynomialen Modi des MQL-Indikators zu emulieren.
