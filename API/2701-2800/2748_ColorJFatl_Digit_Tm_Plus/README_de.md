# ColorJFatl Digit TM Plus Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die ColorJFatl Digit TM Plus Strategie ist ein direkter Port des MetaTrader-5-Expert-Advisors *Exp_ColorJFatl_Digit_Tm_Plus*. Sie handelt Steigungsumkehrungen einer Fast Adaptive Trend Line (FATL), die mit einem Jurik Moving Average (JMA) geglättet wird. Der ursprüngliche Indikator veröffentlicht drei Farben (oben, flach, unten). Die Strategie reagiert, wenn sich die Farbe auf dem neuesten fertigen Balken ändert und richtet die Position an der neuen Steigung aus.

Die StockSharp-Implementierung behält das High-Level-Verhalten der MQL-Version bei: Orders werden auf geschlossenen Kerzen generiert, zeitbasierte Exits sind optional verfügbar, und die Lot-Sizing-Eingabe wird durch den `TradeVolume`-Parameter repräsentiert.

## Signallogik

1. **Indikatorberechnung**
   - Preise werden durch den 39-Tap-FATL-Digitalfilter geleitet, der mit dem ursprünglichen Indikator geliefert wird.
   - Die gefilterte Serie wird mit einem Jurik Moving Average geglättet. Länge, angewendeter Preis und Rundungsgenauigkeit können durch Parameter angepasst werden.
   - Der Farbzustand wird durch das Vorzeichen der Differenz zwischen dem aktuellen und dem vorherigen geglätteten Wert bestimmt: `2` für bullische Steigung, `0` für bearishe Steigung und `1` für neutral/unverändert.

2. **Einstiegsbedingungen**
   - **Long-Einstieg** – aktiviert durch `EnableBuyEntries`. Ausgelöst, wenn die aktuelle Balkenfarbe `2` wird, während die vorherige Balkenfarbe kleiner als `2` war. Eine vorhandene Short-Position wird zuerst geschlossen, wenn `EnableSellExits` true ist.
   - **Short-Einstieg** – aktiviert durch `EnableSellEntries`. Ausgelöst, wenn die aktuelle Balkenfarbe `0` wird, während die vorherige Farbe größer als `0` war. Eine vorhandene Long-Position wird zuerst geschlossen, wenn `EnableBuyExits` true ist.
   - Es kann nur eine Position gleichzeitig offen sein. Orders werden beim Schluss der bestätigenden Kerze gesendet.

3. **Ausstiegsbedingungen**
   - **Steigungsumkehr-Exits** – wenn die Steigung in die entgegengesetzte Richtung kippt, schließt das entsprechende `EnableBuyExits`- oder `EnableSellExits`-Flag die offene Position.
   - **Zeitbasierter Exit** – wenn `UseTimeExit` aktiviert ist, wird eine Position nach `HoldingMinutes` Minuten Haltedauer geschlossen.
   - **Schutzlevel** – `StopLossPoints` und `TakeProfitPoints` werden in Preisschritten ausgedrückt. Sie werden auf jeder fertigen Kerze ausgewertet, indem das Sitzungshoch/-tief mit dem Einstiegspreis verglichen wird.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `TradeVolume` | Menge für Markteinsteige. |
| `StopLossPoints` | Schutz-Stop-Abstand in Preisschritten. Auf `0` setzen zum Deaktivieren. |
| `TakeProfitPoints` | Gewinnziel-Abstand in Preisschritten. Auf `0` setzen zum Deaktivieren. |
| `EnableBuyEntries` / `EnableSellEntries` | Long-/Short-Einstiege aktivieren oder deaktivieren. |
| `EnableBuyExits` / `EnableSellExits` | Steigungsbasierte Ausstiege aktivieren oder deaktivieren. |
| `UseTimeExit` | Aktiviert die zeitgesteuerte Exit-Logik. |
| `HoldingMinutes` | Haltedauer in Minuten, wenn der zeitgesteuerte Exit aktiv ist. |
| `CandleType` | Zeitrahmen für Berechnungen (Standard 4 Stunden). |
| `JmaLength` | Jurik-Moving-Average-Glättungslänge, die auf die FATL-Ausgabe angewendet wird. |
| `AppliedPrices` | Preisquelle für den Digitalfilter (Schluss, Eröffnung, Median, Demark, usw.). |
| `RoundingDigits` | Anzahl der Stellen beim Runden der geglätteten Linie. |
| `SignalBar` | Offset des fertigen Balkens, der zur Auswertung des Indikator-Zustands verwendet wird. |

## Hinweise

- Die Strategie verarbeitet nur vollständig abgeschlossene Kerzen und eignet sich daher gut für historische Backtests.
- `AppliedPrices.Demark` reproduziert dieselbe Berechnung wie der ursprüngliche MQL-Indikator.
- Da StockSharp die Orderausführung asynchron handhabt, wird das interne Tracking des Einstiegspreises aktualisiert, wenn eine neue Position eröffnet wird, und gelöscht, wenn eine Exit-Order gesendet wird.
