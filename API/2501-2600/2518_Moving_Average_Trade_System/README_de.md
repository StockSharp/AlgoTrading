# Moving Average Handelssystem-Strategie (2518)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist ein StockSharp-Port des MetaTrader "Moving Average Trade System" Expertenberaters. Sie analysiert den Trend mithilfe von vier einfachen gleitenden Durchschnitten (SMA), die auf dem Mediankerzpreis berechnet werden. Das System wartet auf einen bestätigten Kreuzungspunkt zwischen den mittel- und langfristigen Durchschnitten, während die schnelleren Durchschnitte die Trendalignment bestätigen. Sobald die Bestätigung eintrifft, dreht die Strategie ihre Position in Richtung des neuen Trends und verwaltet das Risiko mit festen Take-Profit-, Stop-Loss- und Trailing-Stop-Offsets, die in Preisschritten definiert werden.

## Handelslogik

1. **Indikatoren**
   - `SMA(5)` (schnell) auf Medianpreis.
   - `SMA(20)` (mittel) auf Medianpreis.
   - `SMA(40)` (Signal) auf Medianpreis.
   - `SMA(60)` (langsam) auf Medianpreis.

2. **Long-Einstieg**
   - `SMA(5) > SMA(20) > SMA(40)`.
   - `SMA(40)` liegt mindestens `SlopeThresholdSteps` Preisschritte über `SMA(60)`.
   - `SMA(40)` kreuzte auf dem aktuellen Bar über `SMA(60)` (vorheriges `SMA(40)` war unter oder gleich dem langsamen SMA).
   - Wenn eine Short-Position offen ist, kauft die Strategie genug Volumen, um sie zu schließen und die gewünschte Long-Größe aufzubauen.

3. **Short-Einstieg**
   - `SMA(5) < SMA(20) < SMA(40)`.
   - `SMA(40)` liegt mindestens `SlopeThresholdSteps` Preisschritte unter `SMA(60)`.
   - `SMA(40)` kreuzte auf dem aktuellen Bar unter `SMA(60)` (vorheriges `SMA(40)` war über oder gleich dem langsamen SMA).
   - Wenn eine Long-Position offen ist, verkauft die Strategie genug Volumen, um sie zu schließen und die gewünschte Short-Größe aufzubauen.

4. **Risikomanagement** (nur ausgewertet, wenn kein neuer Einstieg auf dem Bar ausgelöst wird):
   - **Trend-Ausstieg:** Longs schließen, wenn `SMA(40) <= SMA(60)` und Shorts schließen, wenn `SMA(40) >= SMA(60)`.
   - **Take-Profit:** Aussteigen, sobald der Preis die konfigurierte Take-Profit-Distanz vom Einstiegspreis erreicht.
   - **Stop-Loss:** Aussteigen, wenn sich der Preis um die konfigurierte Stop-Loss-Distanz gegen die Position bewegt.
   - **Trailing Stop:** Sobald der Preis über den Einstieg hinausgeht, wird der Schutz-Stop um `TrailingStopSteps` Preisschritte nachgezogen, wobei das höchste Hoch (für Longs) oder das niedrigste Tief (für Shorts) seit dem Einstieg verwendet wird.

Alle Stop- und Gewinn-Offsets werden in **Preisschritten** (dem `PriceStep` des Instruments) gemessen. Wenn das Wertpapier keinen Preisschritt meldet, wird `1` als Fallback verwendet.

## Parameter

| Name | Beschreibung | Standardwert | Optimierbar |
| --- | --- | --- | --- |
| `Volume` | Ordervolumen beim Eröffnen neuer Positionen. | `1` | Nein |
| `TakeProfitSteps` | Distanz zum Take-Profit-Ziel in Preisschritten. | `50` | Ja |
| `StopLossSteps` | Distanz zum Schutzstop in Preisschritten. | `50` | Ja |
| `TrailingStopSteps` | Trailing-Stop-Offset in Preisschritten (`0` deaktiviert das Trailing). | `11` | Ja |
| `SlopeThresholdSteps` | Mindestabstand zwischen `SMA(40)` und `SMA(60)` zur Validierung eines Ausbruchs (in Preisschritten). | `1` | Ja |
| `FastPeriod` | Länge des schnellen SMA. | `5` | Ja |
| `MediumPeriod` | Länge des mittleren SMA. | `20` | Ja |
| `SignalPeriod` | Länge des Signal-SMA (verglichen mit dem langsamen SMA). | `40` | Ja |
| `SlowPeriod` | Länge des langsamen SMA, der den Hintergrundtrend definiert. | `60` | Ja |
| `CandleType` | Kerzenserie für Indikatorberechnungen. | `1h Zeitrahmen` | Nein |

## Implementierungshinweise

- Indikatoren sind über die High-Level-`Bind`-API an das Kerzen-Abonnement gebunden, was sicherstellt, dass Berechnungen ereignisgesteuert sind und nicht auf manuellem Buffer-Zugriff basieren.
- Der Medianpreis wird für alle SMA-Berechnungen verwendet, um das Verhalten des originalen MetaTrader-EAs zu replizieren.
- Das Positionsmanagement speichert den tatsächlichen Füllpreis mit `OnNewMyTrade`, um Stop-Loss-, Take-Profit- und Trailing-Stop-Niveaus nach jeder Füllung neu zu berechnen.
- Beim Umkehren einer Position sendet die Strategie eine einzelne Marktorder, die sowohl die bestehende Exposure schließt als auch die neue eröffnet, was das Hedging-fähige Verhalten des ursprünglichen Algorithmus widerspiegelt.
- Alle Kommentare in der C#-Quelldatei sind auf Englisch, gemäß den Repository-Richtlinien.

## Verwendungstipps

- Konfigurieren Sie den `Volume`-Parameter entsprechend der Lot-Größe des Instruments oder des Kontraktmultiplikators.
- Passen Sie Stop- und Gewinnabstände an die Volatilität des Instruments an (die Standardwerte entsprechen den MetaTrader-Einstellungen von 50 Pips Stop/Take-Profit und 11 Pips Trailing Stop bei FX-Paaren).
- Der Parameter `SlopeThresholdSteps` kann auf `0` gesetzt werden, um den zusätzlichen Abstandsfilter zu entfernen und auf jeden `SMA(40)`/`SMA(60)`-Kreuzungspunkt zu reagieren.
- Stellen Sie für Backtesting oder Live-Trading sicher, dass das Wertpapier einen gültigen `PriceStep` bereitstellt; andernfalls behandelt die Strategie eine Preiseinheit als einen einzigen Schritt.
