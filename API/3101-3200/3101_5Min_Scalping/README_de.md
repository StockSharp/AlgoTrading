# 5-Minuten-Scalping-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Portierung des MT4-Expert-Advisors **"5MIN SCALPING" (MQL ID 22828)** auf die StockSharp High-Level-API. Die Strategie sucht nach schnellen Ausbruchs-Setups im primären Zeitrahmen, bestätigt diese mit dem Momentum eines übergeordneten Zeitrahmens und der monatlichen MACD-Richtung, bevor sie in den Markt eintritt.

- **Kategorie:** Ausbruchs-Scalping / Momentum
- **Originalplattform:** MetaTrader 4
- **Datenanforderungen:** Tick- oder Kerzen-Feed für alle konfigurierten Zeitrahmen (Standard 5 Minuten, 30 Minuten, 1 Monat)

## Handelslogik

1. **Trendfilter.** Zwei Linear-Gewichtete Gleitende Durchschnitte (LWMA) mit konfigurierbaren Längen (Standard 6 und 85) definieren den vorherrschenden Trend. Longs erfordern, dass der schnelle LWMA über dem langsamen bleibt, Shorts erfordern das umgekehrte Verhältnis.
2. **Multi-Balken-Strukturfilter.** Das interne LWMA-Triplett (Längen 8, 13, 21) wird über die letzten 20 abgeschlossenen Kerzen bewertet. Der Algorithmus imitiert die `scalper()`-Funktion der MQL-Version:
   - Bullisches Setup: Jeder Balken innerhalb der Schleife muss `LWMA8 > LWMA13 > LWMA21` erfüllen, das Kerzentief zieht sich zum gleitenden Durchschnitts-Stapel zurück, und der aktuelle Schlusskurs bricht über das höchste Hoch der vorherigen 5 Kerzen aus.
   - Bärisches Setup: Spiegellogik mit Hochs, die in den LWMA-Stapel eindringen, und dem aktuellen Schlusskurs, der unter das niedrigste Tief der vorherigen 5 Kerzen ausbricht.
3. **Überlappungsschutz.** Eine kleine Überlappungsbedingung (`Low[2] < High[1]` für Longs, `Low[1] < High[2]` für Shorts) verhindert Einstiege in isolierten Spitzen.
4. **Momentum-Bestätigung.** Ein `Momentum`-Indikator des übergeordneten Zeitrahmens (Standard 30-Minuten-Kerzen, Länge 14) muss zeigen, dass mindestens einer der letzten drei Werte von der Basislinie 100 stärker abweicht als die konfigurierten Schwellenwerte (Standard 0.3).
5. **Makro-MACD-Ausrichtung.** Ein monatliches `MACD(12, 26, 9)`-Histogramm wird via `MovingAverageConvergenceDivergenceSignal` berechnet. Long-Trades erfordern, dass die MACD-Linie über der Signallinie liegt, Short-Trades erfordern das Gegenteil.
6. **Positionsaggregation.** Der Eintritt in die Gegenrichtung schließt zunächst das bestehende Engagement und eröffnet unmittelbar danach den neuen Trade mit dem konfigurierten Volumen.

## Risikomanagement

- **Statische Ziele.** Optionale Take-Profit- und Stop-Loss-Levels in Pips (intern über den `PriceStep` des Instruments konvertiert).
- **Break-Even-Modul.** Wenn aktiviert, wird der Stop auf Einstieg ± Offset verschoben, sobald der Preis eine konfigurierbare Anzahl von Pips zurücklegt.
- **Trailing Stop.** Optionaler Trailing Stop, der die Position in einem festen Pip-Abstand verfolgt, sobald sich der Markt bewegt.
- **Manuelle Ausstiege.** Alle Ausstiege werden innerhalb der Strategie verwaltet, ohne schützende Orders zu platzieren, was das Verhalten des ursprünglichen EA widerspiegelt.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `CandleType` | 5-Minuten-Zeitrahmen | Primärer Zeitrahmen, auf dem Ausbrüche erkannt werden. |
| `MomentumCandleType` | 30-Minuten-Zeitrahmen | Kerzentyp für den übergeordneten Momentum-Filter. |
| `MacroMacdCandleType` | 1-Monats-Zeitrahmen | Kerzentyp für die langfristige MACD-Bestätigung. |
| `FastMaLength` | 6 | Länge des schnellen LWMA-Trendfilters. |
| `SlowMaLength` | 85 | Länge des langsamen LWMA-Trendfilters. |
| `MomentumLength` | 14 | Lookback-Periode für den Momentum-Indikator. |
| `MomentumBuyThreshold` | 0.3 | Mindestabweichung |Momentum-100| zur Bestätigung von Long-Trades. |
| `MomentumSellThreshold` | 0.3 | Mindestabweichung |Momentum-100| zur Bestätigung von Short-Trades. |
| `TakeProfitPips` | 50 | Take-Profit-Distanz in Pips. Auf 0 setzen zum Deaktivieren. |
| `StopLossPips` | 20 | Stop-Loss-Distanz in Pips. Auf 0 setzen zum Deaktivieren. |
| `TrailingStopPips` | 40 | Trailing-Stop-Distanz in Pips. Nur wirksam wenn `EnableTrailing` wahr ist. |
| `EnableTrailing` | true | Aktiviert oder deaktiviert die Trailing-Stop-Logik. |
| `EnableBreakEven` | true | Aktiviert automatisches Break-Even-Management. |
| `BreakEvenTriggerPips` | 30 | Gewinn in Pips, bevor der Stop zum Break-Even verschoben wird. |
| `BreakEvenOffsetPips` | 30 | Zusätzlicher Puffer (in Pips) beim Verschieben des Stops zum Break-Even. |
| `TradeVolume` | 1 | Ordervolumen für Einstiege. |

## Verwendung

1. Die Strategie zum StockSharp-Projekt hinzufügen und mit dem gewünschten Instrument verbinden.
2. Sicherstellen, dass historische Daten für alle konfigurierten Kerzentypen vor dem Start der Strategie verfügbar sind.
3. Volumen, Zeitrahmen und Schwellenwerte entsprechend der Volatilität des gehandelten Instruments konfigurieren.
4. Die Strategie starten. Sie abonniert alle erforderlichen Kerzenserien, zeichnet Indikatoren auf dem Chart (wenn verfügbar) und verwaltet Einstiege/Ausstiege automatisch.

## Unterschiede zum ursprünglichen EA

- Die geldbasierten Trailing-Module (`Take_Profit_In_Money`, `TRAIL_PROFIT_IN_MONEY2`) und der Equity-Stop der MQL-Version sind nicht portiert. Das Risiko wird über Pip-Distanzen verwaltet.
- Die Martingal-ähnliche Lot-Skalierung (`Lots * MathPow(LotExponent, CountTrades())`) ist nicht implementiert. `TradeVolume` manuell anpassen, wenn dynamisches Positionsmanagement benötigt wird.
- E-Mail-/Benachrichtigungs-Alerts aus dem Originalcode werden weggelassen. StockSharp-Benachrichtigungsinfrastruktur bei Bedarf verwenden.
- Die Strategie ist auf den `PriceStep` des Instruments zur Pip-Konvertierung angewiesen. Sicherstellen, dass die Instrument-Metadaten in der Verbindungsumgebung korrekt gefüllt sind.
