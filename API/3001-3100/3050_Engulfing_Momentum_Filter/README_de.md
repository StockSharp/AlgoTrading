# Engulfing Momentum-Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den **ENGULFING** MetaTrader Expert Advisor zur StockSharp High-Level-API. Sie kombiniert ein bullisches/bärisches Engulfing-Muster auf dem Arbeitszeitrahmen mit einer Momentum-Bestätigung auf einem höheren Zeitrahmen und einem monatlichen MACD-Trendfilter. Das Risikomanagement reproduziert das ursprüngliche Break-Even- und Trailing-Verhalten unter Verwendung von Stop-Abständen, die in Instrumentschritten gemessen werden.

## Funktionsweise

1. **Kerzenmuster** – die zuletzt abgeschlossene Kerze muss die vorherige Kerze in der Handelsrichtung verschlucken. Die Strategie überprüft auch, dass der Balken zwei Perioden zuvor mit dem vorherigen Balken überlappt, was die fraktalbasierte Bestätigung des Originals widerspiegelt.
2. **Trendfilter** – schnelle und langsame *gewichtete* gleitende Durchschnitte (LWMA-Analogon) steuern Einstiege. Long-Trades erfordern, dass der schnelle Durchschnitt über dem langsamen liegt und umgekehrt für Shorts.
3. **Momentum-Filter** – ein 14-Perioden-Momentum-Indikator, der auf einem höheren Zeitrahmen berechnet wird, muss vom neutralen Level (100) um mindestens den konfigurierten Schwellenwert bei einem der letzten drei Werte abweichen. Dies reproduziert die `MomLevelB/MomLevelS`-Prüfungen aus dem MQL-Code.
4. **MACD-Filter** – eine monatliche (30-Tage) MACD-Serie muss die Hauptlinie über der Signallinie für Longs und darunter für Shorts zeigen, genau wie der `MacdMAIN0` vs. `MacdSIGNAL0` Vergleich im EA.
5. **Order-Handling** – die Strategie dreht die Position immer, wenn ein entgegengesetztes Signal erscheint. Die Schutzlogik schließt Trades, wenn Stop-, Ziel-, Break-Even- oder Trailing-Regeln auslösen.

## Risikomanagement

- **Stop-Loss / Take-Profit** – Abstände werden in Instrumentschritten (Ticks) konfiguriert. Sie spiegeln die `Stop_Loss`- und `Take_Profit`-Eingaben des ursprünglichen EA wider.
- **Trailing-Stop** – optionales Trailing in Schritten gemessen. Der Stop folgt dem besten nach dem Einstieg erzielten Preis.
- **Break-Even-Bewegung** – sobald der Preis um `BreakEvenTriggerSteps` voranschreitet, wird der Stop auf den Einstieg plus `BreakEvenOffsetSteps` verschoben und reproduziert das "kein Verlust"-Feature (`USEMOVETOBREAKEVEN`).

Geldbasierte Ziele aus dem MQL-Skript (`Use_TP_In_Money`, `Take_Profit_In_percent`) werden absichtlich weggelassen, um die Logik konsistent mit StockSharp's Einheitensystem zu halten. Prozent- oder währungsbasierte Ausstiege können durch Anpassen der Schritt-Parameter recreiert werden.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `FastMaPeriod` / `SlowMaPeriod` | Längen der gewichteten gleitenden Durchschnitte für die Trendbestätigung. |
| `MomentumPeriod` | Momentum-Länge auf dem höheren Zeitrahmen. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Mindest-Absolutabweichung von 100 für den Momentum-Filter. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD-Konfiguration für `MacdCandleType`. |
| `StopLossSteps`, `TakeProfitSteps` | Schutz-Stop- und Zielabstände in Kursschritten. Auf null setzen zum Deaktivieren. |
| `TrailingStopSteps` | Optionaler Trailing-Stop-Abstand (0 deaktiviert Trailing). |
| `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | Abstand vor dem Break-Even-Stopp und der angewendete Offset. |
| `CandleType` | Primärer Zeitrahmen, wo Engulfing-Muster ausgewertet werden. |
| `HigherCandleType` | Höherer Zeitrahmen für den Momentum-Filter (Standard: 1 Stunde). |
| `MacdCandleType` | Zeitrahmen für den MACD-Trendfilter (Standard: 30 Tage ≈ monatlich). |

## Verwendung

1. Die Strategie einem Wertpapier zuweisen und `CandleType`, `HigherCandleType` und `MacdCandleType` auf bevorzugte Zeitrahmen einstellen.
2. MA- und Momentum-Parameter anpassen, wenn eine andere Marktstruktur ausgerichtet werden soll.
3. Stop-, Take-Profit-, Trailing- und Break-Even-Abstände in Kursschritten konfigurieren, die dem Tick-Preis des Instruments entsprechen.
4. Strategie starten; sie abonniert automatisch alle notwendigen Kerzen-Feeds und beginnt mit der Signalauswertung, sobald Indikatoren gebildet sind.

## Hinweise und Unterschiede zum Original-EA

- Gewichtete gleitende Durchschnitte replizieren die in MQL verwendeten LWMA-Berechnungen ohne manuelles Iterieren über Preise.
- Break-Even- und Trailing-Logik wird auf abgeschlossenen Kerzen angewendet und entspricht dem Balken-für-Balken-Ansatz des EA unter Nutzung von StockSharp-Schutz-Hilfsmitteln.
- Geldbasiertes Trailing und prozentbasierte Ausstiege werden nicht portiert, da StockSharp auf Instrumenteinheiten operiert; äquivalentes Verhalten kann durch Kalibrieren der schrittbasierten Parameter erreicht werden.
- Die Strategie nimmt eine Position gleichzeitig an, was dem üblichen Verwendungsszenario des Quell-EA entspricht, obwohl er eine `Max_Trades`-Eingabe enthielt.

Schwellenwerte und Zeitrahmen an das gehandelte Asset anpassen. Instrumente mit höherer Volatilität erfordern oft größere Schrittabstände oder breitere Momentum-Schwellenwerte, um vorzeitige Ausstiege zu vermeiden.
