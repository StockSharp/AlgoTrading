# Master MM Droid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Master MM Droid-Strategie ist ein Multi-Modul-Port des ursprünglichen MetaTrader 5 Expert Advisors. Die StockSharp-Implementierung behält die Kernideen des Legacy-Robots bei, nutzt jedoch die High-Level-API für Kerzenabonnements, Indikatorbindung und Orderverwaltung. Vier unabhängige Money-Management-Blöcke können ein- oder ausgeschaltet werden, sodass die Strategie Momentum-Einstiege mit geplanten Ausbruchsorders und wöchentlichen Gap-Spielen kombinieren kann.

## Module
1. **RSI-Block**
   - Verwendet einen 14-Perioden-Relative-Strength-Index auf dem konfigurierten Kerzentyp.
   - Geht Long, wenn RSI aus dem überverkauften Bereich nach oben kreuzt, und Short, wenn er aus dem überkauften Bereich nach unten kreuzt.
   - Ermöglicht Pyramidisierung mit einer konfigurierbaren Anzahl zusätzlicher Einstiege in festen Preisschritten.
   - Wendet einen festen anfänglichen Stop basierend auf Punktabstand an und aktiviert einen Trailing-Stop, sobald die Position offen ist.
2. **Box-Ausbruch-Block**
   - Baut dreimal täglich Ausbruchsboxen neu auf (sitzungsversetzte Stunden 6, 12 und 20 standardmäßig).
   - Platziert geklammerte Stop-Orders oberhalb des Sitzungshochs und unterhalb des Sitzungstiefs mit einem konfigurierbaren Puffer.
   - Löscht alle ausstehenden Orders und Positionen bei Sitzungszurücksetzungen (Stunden 0, 10 und 16), imitiert das ursprüngliche Expertenverhalten.
3. **Wöchentlicher Ausbruch-Block**
   - Verfolgt die Montags-Preisbewegung und speichert das laufende Hoch/Tief des ersten Sitzungsteils.
   - Platziert symmetrische Stop-Orders innerhalb eines begrenzten Aktivierungsfensters (`StartHour` – `WeeklySetupEndHour`), damit die Woche mit einem OCO-Ausbruch beginnt.
   - Erzwingt freitagabends eine flache Position, um Wochenend-Exposure zu vermeiden.
4. **Gap-Block**
   - Vergleicht die neue Tageseröffnung mit dem Tageshoch/-tief des Vortages (unter Verwendung des verschobenen Kalenders).
   - Kauft starke Gap-Down-Eröffnungen und verkauft starke Gap-Up-Eröffnungen.
   - Setzt einen Schutz-Stop in konfigurierbarem Abstand und übergibt die weitere Verwaltung an das Trailing-Engine.

## Parameter
| Name | Beschreibung |
| ---- | ------------ |
| `CandleType` | Zeitrahmen für Indikatorberechnungen und Zeitfensterchecks. |
| `TimeShiftHours` | Sitzungsversatz auf Kerzen-Zeitstempel, damit der Stundenplan dem ursprünglichen EA entspricht. |
| `StartHour` | Basis-Montagsstundenbeginn für das Wochenmodul (vor Anwendung des Versatzes). |
| `EnableRsiModule`, `EnableBoxModule`, `EnableWeeklyModule`, `EnableGapModule` | Schalter für die vier unabhängigen Blöcke. |
| `RsiPeriod`, `RsiLowerLevel`, `RsiUpperLevel` | RSI-Berechnung und Triggerlevel. |
| `RsiMaxEntries`, `RsiPyramidPoints` | Pyramidisierungssteuerung für den RSI-Block. |
| `RsiStopLossPoints`, `RsiTrailingPoints` | Anfangs- und Trailing-Stop-Größen (in Punkten) für RSI-gesteuerte Trades. |
| `BoxEntryPoints`, `BoxTrailingPoints` | Ausbruchspuffer und Trailing-Abstand für Box-Orders. |
| `WeeklyEntryPoints`, `WeeklySetupEndHour`, `WeeklyTrailingPoints` | Wöchentliche Ausbruchskonfiguration. |
| `GapStopLossPoints`, `GapTrailingPoints` | Schutz-Stop und Trailing-Abstand für das Gap-Modul. |

Alle punktbasierten Parameter werden mit dem `TickSize` des Instruments multipliziert, um Preisoffsets zu erhalten, sodass sich die Strategie verschiedenen Symbolen anpasst.

## Handelslogik
- **Indikatorbindung**: Ein einzelner RSI-Indikator ist an das Kerzenabonnement gebunden. Jede fertige Kerze löst `ProcessCandle` aus, das die Werte an die vier Modul-Handler weiterleitet.
- **Tägliches Status-Tracking**: Die Strategie aggregiert Eröffnung/Hoch/Tief für jeden verschobenen Tag, um die Gap-Logik zu unterstützen und eine historische Referenz für das Wochenmodul zu halten.
- **Orderplatzierung**: Orders werden über `BuyMarket`, `SellMarket`, `BuyStop`, `SellStop` entsprechend den Best Practices der High-Level-API eingereicht. Geplante Module stornieren stets aktive Orders, bevor sie sich neu rüsten, um Duplikate zu vermeiden.
- **Trailing-Verwaltung**: Sobald eine Position aktiv ist, speichert `_activeTrailingPoints` den modulspezifischen Abstand. Die `UpdateTrailing`-Methode verschiebt Stop-Orders nur in der günstigen Richtung.

## Risikomanagement
- Nur durch die RSI- und Gap-Module erstellte Marktorders sind durch einen sofortigen in Punkten berechneten Stop geschützt.
- Ausbruchsmodule verlassen sich nach der Aktivierung auf das Trailing-Engine; sie können bei Bedarf mit externem Portfolio-Schutz kombiniert werden.
- Das Aufrufen von `ClosePosition()` ist der kanonische Weg zum Glätten, was die Kompatibilität mit StockSharp-Risikowerkzeugen bewahrt.

## Verwendungshinweise
- Die Strategie operiert auf einem einzelnen Wertpapier und verwendet den globalen `Volume`-Wert für die Größenbestimmung. Portfolio-Schutz separat anpassen, wenn per-Position-Risikolimits benötigt werden.
- Sitzungszeiten werden nach Anwendung von `TimeShiftHours` ausgewertet. Beispielsweise entspricht bei Standardwert `2` die Box-Zurücksetzung bei Stunde `0` der Serverzeit 02:00.
- Da StockSharp-Strategien Netto-Positionen verwalten, werden simultane Long/Short-Körbe (in MetaTrader-Hedging-Konten möglich) konsolidiert. Dies ist der hauptsächliche Verhaltensunterschied zum ursprünglichen EA und sollte bei der Validierung berücksichtigt werden.

## Protokollierung und Überwachung
- Jedes Modul setzt seine internen Flags zurück, sobald die Position auf null zurückkehrt, was Betreibern hilft zu diagnostizieren, welcher Block einen Trade erzeugt hat.
- Optionale Charts oder Protokollierung über StockSharp-Einrichtungen hinzufügen, wenn detaillierte Analysen erforderlich sind.
