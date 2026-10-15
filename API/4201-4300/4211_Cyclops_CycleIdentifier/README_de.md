# Strategie zur Identifizierung des Zyklopenzyklus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie portiert den Expert Advisor **Cyclops v1.2** von MetaTrader zusammen mit seinem proprietären Indikator *CycleIdentifier* auf das hohe Niveau von StockSharp API. Der Algorithmus glättet die Schlusskurse mit einem geglätteten gleitenden Durchschnitt (SMMA), misst die jüngste Volatilität anhand eines langen Lookback-Durchschnitts-True-Ranges und markiert Zykluswendepunkte, wenn der Preis weit genug vom letzten Schwung entfernt ist. Große Zyklusumkehrungen generieren neue Einstiege, während kleinere Umkehrungen optionale Ausstiegssignale bieten.

Ein konfigurierbarer Zero-Lag-Filter validiert die Steigung der geglätteten Reihe. Der Filter kann direkt auf geglättete Preisdaten oder auf ein aus derselben Serie abgeleitetes RSI im Wilder-Stil wirken. Eine zusätzliche Bestätigung ist über einen klassischen Momentum-Indikator verfügbar, und der Handel kann auf ein bestimmtes Wochentags-/Stundenfenster beschränkt werden.

## Signallogik

- **Zykluserkennung** – Die interne Zustandsmaschine verfolgt die letzten Hochs und Tiefs des geglätteten Preises. Wenn der Preis den adaptiven Schwellenwert (durchschnittliche Spanne × *Länge*) überschreitet, markiert die Strategie einen kleinen Zyklus. Zur Kennzeichnung eines Hauptzyklus ist ein größeres Vielfaches (*MajorCycleStrength*) erforderlich.
- **Einträge** – Große Aufwärtszyklen (`MajorBuy`) eröffnen Long-Positionen; Große rückläufige Zyklen (`MajorSell`) eröffnen Shorts. Aktive Positionen werden vor dem Rückwärtsfahren auf die Gegenseite automatisch geschlossen.
- **Optionale Exits** – Wenn *UseExitSignal* aktiviert ist, können profitable Trades beim entsprechenden Nebenzyklussignal (`MinorSellExit` für Long-Positionen, `MinorBuyExit` für Shorts) geschlossen werden, wenn kein entgegengesetzter Hauptzyklus vorhanden ist.
- **Zero-Lag-Filter** – Wenn *UseCycleFilter* aktiviert ist, muss ein Zero-Lag-Glättungsfilter die Steigung bestätigen (steigend bei Long-Positionen, fallend bei Short-Positionen). Die Filterquelle wird durch *CycleFilterMode* ausgewählt (geglätteter Preis oder RSI).
- **Momentum-Filter** – Wenn *UseMomentumFilter* aktiviert ist, erfordern Einträge `Momentum ≥ MomentumTriggerLong` für Long-Positionen und `Momentum ≤ MomentumTriggerShort` für Short-Positionen.

## Handelsmanagement

- **Feste Ziele** – *TakeProfitPips* und *StopLossPips* definieren optionale feste Exits in Instrumenten-Pips.
- **Break-Even** – Wenn *BreakEvenTrigger* Gewinnpips erreicht werden, wird der Stop auf den Einstiegspunkt ± einen Pip gezogen.
- **Trailing** – *TrailingStopTrigger* aktiviert einen Trailing Stop, der dem Preis bei *TrailingStopPips* folgt, sobald die Triggerdistanz erreicht ist.
- **Sitzungssteuerung** – Wenn *UseTimeRestriction* wahr ist, sind neue Positionen nur vor `DayEnd` (0=Sonntag) und bis zu `HourEnd` (einschließlich) an diesem Tag zulässig. Bestehende Trades werden auch im Anschluss weiterhin verwaltet.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `Volume` | Auftragsvolumen, das für Einträge verwendet wird. |
| `PriceActionFilter` | Länge des geglätteten gleitenden Durchschnitts, der auf den Schlusskurs angewendet wird. |
| `Length` | Auf den Durchschnittsbereich angewendeter Multiplikator zur Erkennung kleinerer Zyklen. |
| `MajorCycleStrength` | Multiplikator, der große von kleinen Schwankungen trennt. |
| `UseCycleFilter` | Aktiviert die Bestätigung der verzögerungsfreien Steigung. |
| `CycleFilterMode` | Wählt die Eingabe ohne Verzögerung aus: geglätteter Preis (`Sma`) oder RSI (`Rsi`). |
| `FilterStrengthSma` | Länge des Zero-Lag-Filters bei Verwendung des geglätteten Preises. |
| `FilterStrengthRsi` | Länge und RSI Zeitraum, wenn der Filter auf RSI Werten basiert. |
| `UseMomentumFilter` | Schaltet die Impulsbestätigung ein oder aus. |
| `MomentumPeriod` | Länge des Momentum-Indikators. |
| `MomentumTriggerLong` | Minimaler Schwung für lange Einstiege erforderlich. |
| `MomentumTriggerShort` | Maximal zulässiger Schwung für kurze Einstiege. |
| `UseExitSignal` | Ermöglicht Exits auf Basis kleinerer Zyklen, wenn sie profitabel sind. |
| `UseTimeRestriction` | Beschränkt den Handel auf das konfigurierte Wochentags-/Stundenfenster. |
| `DayEnd` | Letzter Tag der Woche, an dem neue Einträge zulässig sind. |
| `HourEnd` | Letzte Stunde am letzten Handelstag für Neuzugänge. |
| `BreakEvenTrigger` | Gewinn in Pips, der zur Aktivierung des Break-Even-Stopps erforderlich ist. |
| `TrailingStopTrigger` | Der Gewinn in Pips ist erforderlich, um mit dem Trailing zu beginnen. |
| `TrailingStopPips` | Vom Trailing Stop aufrechterhaltener Abstand in Pips. |
| `TakeProfitPips` | Feste Take-Profit-Distanz in Pips. |
| `StopLossPips` | Stop-Loss-Distanz in Pips korrigiert. |
| `CandleType` | Primärer Zeitrahmen, der die Strategie speist. |

## Unterschiede zum Original EA

- Der durchschnittliche Bereich wird mit einem 250-Perioden-Durchschnitt der wahren Reichweite multipliziert mit *Länge* geschätzt und liefert ein Verhalten, das dem in MQL verwendeten gleitenden Hoch-/Tief-Bereich entspricht.
- Die Momentum-Bestätigung verwendet den tatsächlichen Indikatorwert (das Skript MQL wird mit dem Pip-Multiplikator `bm` verglichen, wodurch der Filter effektiv deaktiviert wird).
- Die verzögerungsfreie Glättung wird mit denselben rekursiven Koeffizienten implementiert, jedoch in Dezimalarithmetik ausgedrückt. Der RSI-Modus verwendet einen Wilder RSI, dessen Periode *FilterStrengthRsi* entspricht.

## Nutzungshinweise

1. Wählen Sie das Instrument aus und binden Sie den Parameter `CandleType` an den gewünschten Zeitrahmen.
2. Konfigurieren Sie die Risiko- und Sitzungseinstellungen passend zu Ihrer Broker-Umgebung.
3. Aktivieren Sie *UseCycleFilter* oder *UseMomentumFilter*, wenn eine strengere Bestätigung erforderlich ist; Deaktivieren Sie sie für schnellere, aber lautere Eingaben.
4. Die Strategie behält höchstens eine offene Position bei. Gegenläufige Taktsignale schließen die aktuelle Position, bevor eine neue ausgewertet wird.
