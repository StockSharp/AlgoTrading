# Hercules A.T.C. 2006-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Hercules A.T.C. 2006 ist eine Trendfolge-Strategie auf höheren Zeitrahmen, die den 2006 veröffentlichten MetaTrader Expert Advisor
nachbildet. Die StockSharp-Version wartet auf abgeschlossene Kerzen im primären Zeitrahmen, erkennt bullische/bärische Kreuzungen
zwischen einer schnellen EMA(1) und einer langsamen SMA(72) und öffnet Trades nur, wenn zusätzliche Filter den Ausbruch bestätigen.
Die Strategie teilt ihre Position in zwei Tranchen mit unabhängigen Take-Profit-Niveaus auf und zieht den Stop nach, sobald der
Preis voranschreitet.

## Indikatoren und Daten

- **Primäre Kerzen:** konfigurierbar (Standard: 1-Stunden-Kerzen).
- **Schnelle MA:** EMA mit Länge `FastMaPeriod` (Standard: 1).
- **Langsame MA:** SMA mit Länge `SlowMaPeriod` (Standard: 72).
- **RSI-Filter:** RSI der Länge `RsiLength` auf dem `RsiTimeFrame` (Standard: 1 Stunde).
- **Tages-Envelope:** SMA der Länge `DailyEnvelopePeriod` auf `DailyEnvelopeTimeFrame`
  mit ±`DailyEnvelopeDeviation` Prozent Versatz.
- **H4-Envelope:** SMA der Länge `H4EnvelopePeriod` auf `H4EnvelopeTimeFrame`
  mit ±`H4EnvelopeDeviation` Prozent Versatz.
- **Rollendes Hoch/Tief:** höchstes Hoch und tiefstes Tief der letzten `HighLowHours`
  Stunden im primären Zeitrahmen.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `TriggerPips` | 38 | Versatz in Pips, der zum Kreuzungspreis addiert/subtrahiert wird, bevor eine Order ausgelöst wird. |
| `TrailingStopPips` | 90 | Trailing-Stop-Abstand in Pips (0 deaktiviert das Trailing). |
| `TakeProfit1Pips` | 210 | Erster Take-Profit-Abstand in Pips zum Skalieren der Hälfte der Position. |
| `TakeProfit2Pips` | 280 | Letzter Take-Profit-Abstand in Pips zum Schließen der verbleibenden Position. |
| `FastMaPeriod` | 1 | Länge der schnellen EMA im Kreuzungsdetektor. |
| `SlowMaPeriod` | 72 | Länge der langsamen SMA-Basislinie. |
| `StopLossLookback` | 4 | Anzahl abgeschlossener Kerzen für den anfänglichen Stop-Preis. |
| `HighLowHours` | 10 | Größe des rollenden Fensters (in Stunden) für den Ausbruchsfilter. |
| `BlackoutHours` | 144 | Abkühlzeit (in Stunden) nach dem Schließen eines Trades vor einem neuen Einstieg. |
| `RsiLength` | 10 | RSI-Länge im höheren Zeitrahmen-Filter. |
| `RsiUpper` | 55 | Minimaler RSI-Wert für Long-Einstiege. |
| `RsiLower` | 45 | Maximaler RSI-Wert vor dem Blockieren von Short-Einstiegen. |
| `DailyEnvelopePeriod` | 24 | SMA-Länge für den Tages-Envelope-Filter. |
| `DailyEnvelopeDeviation` | 0.99 | Tages-Envelope-Abweichung in Prozent. |
| `H4EnvelopePeriod` | 96 | SMA-Länge für den Vier-Stunden-Envelope-Filter. |
| `H4EnvelopeDeviation` | 0.1 | Vier-Stunden-Envelope-Abweichung in Prozent. |
| `CandleType` | 1 Stunde | Primärer Kerzentyp. |
| `RsiTimeFrame` | 1 Stunde | Kerzentyp für den RSI-Filter. |
| `DailyEnvelopeTimeFrame` | 1 Tag | Kerzentyp für den Tages-Envelope. |
| `H4EnvelopeTimeFrame` | 4 Stunden | Kerzentyp für den Vier-Stunden-Envelope. |

## Handelsregeln

1. **Kreuzungserkennung**
   - EMA(1)- und SMA(72)-Werte der letzten drei abgeschlossenen Balken beobachten.
   - Bullisches Signal erkennen, wenn EMA in einem der zwei vorherigen Balken über SMA kreuzt.
   - Bärisches Signal erkennen, wenn EMA in einem der zwei vorherigen Balken unter SMA kreuzt.
   - Den Kreuzungspreis (Durchschnitt der schnellen und langsamen Werte) speichern und ein Zwei-Balken-Triggerfenster starten.

2. **Triggerbedingung**
   - `TriggerPrice = CrossPrice ± TriggerPips` berechnen (in Preiseinheiten umgerechnet).
   - Der Trigger bleibt zwei primäre Kerzen nach dem Kreuzungszeitpunkt gültig.
   - Longs erfordern, dass das Kerzenhoch den bullischen Triggerpreis erreicht oder überschreitet.
   - Shorts erfordern, dass das Kerzentief den bärischen Triggerpreis erreicht oder durchbricht.

3. **Einstiegsfilter**
   - Keine offene Position und keine aktive Abkühlzeit (`BlackoutHours`).
   - RSI-Filter: `RSI > RsiUpper` für Longs, `RSI < RsiLower` für Shorts.
   - Ausbruchsfilter: aktueller Schlusskurs muss für Longs das rollende Hoch überschreiten oder für Shorts unter das rollende Tief fallen.
   - Envelope-Bestätigung: aktueller Schlusskurs muss für Longs über beiden oberen Envelope-Bändern liegen oder für Shorts unter beiden unteren Bändern.

4. **Orderausführung**
   - Marktorder mit dem Strategie-Volumen senden (Standard: 2 Einheiten, d.h. zwei gleiche Teilpositionen).
   - Stop-Loss: Tief (Long) oder Hoch (Short) der `StopLossLookback`-ten Kerze.
   - Take-Profit-Niveaus: `TakeProfit1Pips` für die erste Hälfte, `TakeProfit2Pips` für den Rest.
   - Blackout-Timer starten, um neue Einstiege für `BlackoutHours` Stunden zu blockieren.

5. **Positionsmanagement**
   - Trailing-Stop aktiviert sich sofort, wenn `TrailingStopPips` > 0, und bewegt sich nur zugunsten des Trades.
   - Hälfte der Position beim ersten Take-Profit-Niveau skalieren.
   - Verbleibende Position schließen, wenn der letzte Take-Profit ausgelöst wird, der Stop-Loss getroffen wird oder der Preis den Trailing-Stop kreuzt.

## Risikomanagement

- Stops werden immer aus abgeschlossenen Kerzen abgeleitet, um Intrabar-Rauschen zu reduzieren.
- Zwei Take-Profit-Ziele sichern Teilgewinne, bevor der Trade weiterläuft.
- Trailing-Stops schützen Gewinne, nachdem sich der Markt in die gewünschte Richtung bewegt hat.
- Eine lange Sperrzeit (Standard: 144 Stunden) verhindert schnellen Wiedereinstieg nach einem Ausbruch und spiegelt das ursprüngliche EA-Verhalten wider.

## Hinweise

- Der StockSharp-Port bewahrt die ursprüngliche Money-Management-Idee, indem er das Strategie-Volumen standardmäßig auf zwei Einheiten setzt, sodass der Teilausstieg die Hälfte der Position laufen lässt.
- Envelope-Versatzwerte aus MetaTrader werden durch die Verwendung der aktuellsten Envelope-Werte angenähert, da Vorwärtsverschiebung von der High-Level-API nicht unterstützt wird.
- Die Strategie benötigt Preisschrittinformationen für die korrekte Übersetzung von Pip-Abständen; sicherstellen, dass die Instrument-Metadaten vollständig sind.
