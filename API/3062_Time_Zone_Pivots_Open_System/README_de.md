# Strategie Zeitzonenpivots-Offenem-System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp High-Level-API-Port des MetaTrader-Experten `Exp_TimeZonePivotsOpenSystem`. Sie reproduziert die ursprüngliche Logik, die einen symmetrischen Preiskanal am täglichen Eröffnungspreis zu einer konfigurierbaren Stunde verankert und reagiert, wenn abgeschlossene Kerzen über oder unter dieser Band brechen. Alle Orders werden als Marktorders gesendet und optionaler Stop-Loss-/Take-Profit-Schutz wird über `StartProtection` konfiguriert.

## Funktionsweise

1. Abonniert den konfigurierten Kerzen-Zeitrahmen, zeichnet den Instrumentenpreisschritt auf und konfiguriert Schutzstops, wenn die Abstände größer als null sind.
2. Verfolgt die erste Kerze jedes Tages, deren Eröffnungszeit mit `StartHour` übereinstimmt. Der Eröffnungspreis dieser Kerze wird zum Anker der Session und definiert die obere und untere Band bei `OffsetPoints` Preisschritten über und unter dem Anker.
3. Berechnet ein Fünf-Zustands-Signal für jede abgeschlossene Kerze und spiegelt damit den farbcodierten Buffer des ursprünglichen benutzerdefinierten Indikators:
   - `0` / `1`: die Kerze schloss über der oberen Band (bullischer Ausbruch, wobei der Index die Kerzenrichtung widerspiegelt).
   - `2`: die Kerze endete innerhalb der Band (neutral).
   - `3` / `4`: die Kerze schloss unter der unteren Band (bärischer Ausbruch).
4. Pflegt eine gleitende Signalhistorie. Die Kerze, die `SignalBar` Schritte zurückliegt, dient als Bestätigungsbalken, und die Kerze unmittelbar davor muss neutral sein, um einen Einstieg auszulösen, und recreiert damit die MetaTrader-Logik, die nach dem Ausbruch auf eine Kerze wartet.
5. Wenn eine bullische Bestätigung erscheint, schließt die Strategie optional Short-Positionen und eröffnet, wenn flach und erlaubt, eine neue Long-Position. Bärische Bestätigungen verhalten sich symmetrisch für Short-Trades.
6. Nach dem Eröffnen einer neuen Position verschiebt die Strategie weitere Einstiege in dieselbe Richtung, bis die nächste Kerze nach dem Bestätigungsbalken beginnt, um doppelte Orders in derselben Session zu verhindern.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|--------------|---------|
| `CandleType` | Kerzen-Zeitrahmen für die Ausbruchsberechnungen. | `H1` |
| `OrderVolume` | Volumen für neue Positionen. | `0.1` |
| `StartHour` | Stunde (0-23), deren Eröffnungspreis die täglichen Bänder verankert. | `0` |
| `OffsetPoints` | Halbbreite der Band in Preisschritten (Tick-Einheiten). | `100` |
| `SignalBar` | Anzahl geschlossener Kerzen zwischen dem aktuellen Balken und der Ausbruchsbestätigung. Muss in diesem Port ≥ 1 sein. | `1` |
| `StopLossPoints` | Schutzstop-Abstand in Preisschritten. | `1000` |
| `TakeProfitPoints` | Gewinnziel-Abstand in Preisschritten. | `2000` |
| `EnableLongEntry` | Erlaubt das Eröffnen von Long-Positionen nach bullischen Signalen. | `true` |
| `EnableShortEntry` | Erlaubt das Eröffnen von Short-Positionen nach bärischen Signalen. | `true` |
| `CloseLongOnBearishBreak` | Bestehende Long-Positionen bei bärischen Bestätigungen schließen. | `true` |
| `CloseShortOnBullishBreak` | Bestehende Short-Positionen bei bullischen Bestätigungen schließen. | `true` |

## Hinweise

- Der Geldmanagement-Block aus der MetaTrader-Version wird durch den expliziten `OrderVolume`-Parameter ersetzt, der für StockSharp-Strategien typisch ist.
- Die Stop-Loss- und Take-Profit-Parameter werden von Punktabständen in absolute Preisoffsets unter Verwendung des aktuellen Instrumentenpreisschritts umgerechnet.
- Die S#-Implementierung hält genau wie das MQL-Original nur eine Nettoposition (Long, Short oder flat) und überspringt neue Einstiege, während eine Position noch geöffnet ist.
