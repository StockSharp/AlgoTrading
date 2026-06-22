# Color PEMA Envelopes Digit System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das **Color PEMA Envelopes Digit System** reproduziert die Logik des MetaTrader-Experten
`Exp_Color_PEMA_Envelopes_Digit_System.mq5`. Die Strategie wertet die Farbcodes aus,
die vom Color PEMA Envelopes-Indikator erzeugt werden: Wenn eine Kerze außerhalb der
oberen oder unteren Bande schließt, malt der Indikator eine spezielle Farbe, und sobald
der Preis wieder in den Kanal eintritt, wird ein Trade in Richtung des Ausbruchs ausgelöst.

## Funktionsweise
1. Die Strategie baut eine achtstufige Polynomielle EMA (PEMA) mit fraktionalen Längen auf,
   genau wie im ursprünglichen Indikator. Das Ergebnis wird auf die konfigurierte
   Genauigkeit gerundet und um den optionalen Preisversatz verschoben.
2. Obere und untere Hüllkurven werden durch Anwendung einer prozentualen Abweichung um den PEMA-Wert erstellt.
3. Jede abgeschlossene Kerze erhält einen Farbcode je nach ihrer Beziehung zu den verschobenen Hüllkurven:
   - `4`/`3`: Schlusskurs über der oberen Bande (bullischer/bärischer Körper).
   - `1`/`0`: Schlusskurs unter der unteren Bande (bullischer/bärischer Körper).
   - `2`: Preis bleibt innerhalb der Hüllkurve.
4. Die Strategie liest die Farbe, die auf der `SignalBar + 1`-Kerze aufgetreten ist, und vergleicht sie mit
der Farbe der `SignalBar`-Kerze. Dies ahmt die `CopyBuffer`-Aufrufe des Expertenberaters nach.
5. Wenn die ältere Farbe einen Ausbruch über die obere Bande anzeigt und die neuere Farbe
in den Kanal zurückkehrt, ist ein Long-Einstieg erlaubt (wenn aktiviert) und jede Short-Position wird geschlossen.
   Die gespiegelte Logik wird für Short-Einstiege und zum Schließen von Long-Positionen verwendet.
6. Schutz-Stop-Loss- und Take-Profit-Orders werden über das Risikmodul von StockSharp verwaltet.

## Parameter
- `CandleType` – Zeitrahmen für Analyse und Handel.
- `TradeVolume` – Menge, die mit Market-Orders gesendet wird.
- `EmaLength` – Fraktionale Länge, die von jeder EMA-Schicht in der PEMA-Kette verwendet wird.
- `AppliedPrices` – Quellpreis (Schluss, Eröffnung, Median, gewichtet, Trendfolge, DeMark, etc.).
- `DeviationPercent` – Prozentualer Abstand für beide Hüllkurven um PEMA.
- `Shift` – Anzahl abgeschlossener Kerzen zur Verschiebung des Hüllkurvenvergleichs.
- `PriceShift` – Zusätzliche absolute Verschiebung für beide Hüllkurven.
- `Digit` – Zusätzliche Präzisionsstellen beim Runden der PEMA-Ausgabe.
- `SignalBar` – Wie viele geschlossene Kerzen zurück die aktuelle Farbe abgelesen wird (die ältere Farbe wird eine Barre weiter zurück genommen).
- `AllowBuyOpen` / `AllowSellOpen` – Neue Long/Short-Einstiege aktivieren oder deaktivieren.
- `AllowBuyClose` / `AllowSellClose` – Schließen von Long/Short-Positionen bei entgegengesetzten Signalen erlauben.
- `StopLossPoints` – Schutz-Stop-Abstand in Preispunkten (multipliziert mit `PriceStep`).
- `TakeProfitPoints` – Gewinnziel-Abstand in Preispunkten.

## Standardwerte
- `CandleType = TimeSpan.FromHours(4).TimeFrame()`
- `TradeVolume = 1m`
- `EmaLength = 50.01m`
- `AppliedPrices = AppliedPrices.Close`
- `DeviationPercent = 0.1m`
- `Shift = 1`
- `PriceShift = 0m`
- `Digit = 2`
- `SignalBar = 1`
- `AllowBuyOpen = true`
- `AllowSellOpen = true`
- `AllowBuyClose = true`
- `AllowSellClose = true`
- `StopLossPoints = 1000m`
- `TakeProfitPoints = 2000m`

## Filter
- **Kategorie**: Ausbruch / Kanal-Wiedereintritt
- **Richtung**: Long/Short
- **Indikatoren**: Polynomielle EMA-Hüllkurven
- **Stops**: Ja (punktbasierter Stop-Loss und Take-Profit)
- **Zeitrahmen**: Swing (Standard 4H)
- **Risikolevel**: Moderat – handelt nur, wenn der Preis von einem Extrem zurückkehrt
- **Saisonalität**: Keine
- **Neuronale Netze**: Nein
- **Divergenz**: Nein
