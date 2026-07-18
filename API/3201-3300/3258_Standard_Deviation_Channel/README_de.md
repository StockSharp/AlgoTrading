# Standard Deviation Channel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp-Port des MetaTrader-Experten **Standard Deviation Channel**. Sie zeichnet einen Volatilitätskanal auf Basis eines linearen gewichteten gleitenden Durchschnitts (LWMA) und handelt Ausbrüche, die mit dem vorherrschenden Trend übereinstimmen. Einstiege werden durch Momentum-Stärke und eine MACD-Bestätigung gefiltert, während Ausstiege feste Ziele, Break-Even-Sprünge und Trailing-Schutz kombinieren.

## Indikatoren und Signale
- **Standard Deviation-Kanal** aus einer LWMA-Basislinie und einem konfigurierbaren Abweichungsmultiplikator. Long-Setups erfordern eine steigende obere Bande; Short-Setups erfordern eine fallende untere Bande.
- **Trendfilter:** Schnelle und langsame LWMA auf denselben Kerzen. Longs erfordern `LWMA_fast > LWMA_slow`; Shorts erfordern das Gegenteil.
- **Momentum-Filter:** Ein 14-Perioden-Momentum-Indikator. Mindestens eine der letzten drei Messwerte muss vom neutralen Niveau 100 um den konfigurierten Schwellenwert abweichen.
- **MACD-Filter:** Klassische 12/26/9-Konfiguration. Long-Einstiege benötigen `MACD ≥ signal`, während Short-Einstiege `MACD ≤ signal` erfordern.

## Trade-Management
- **Positionsgrößenbestimmung:** Verwendet den Parameter `TradeVolume`. Umkehrungen schließen automatisch das entgegengesetzte Exposure, bevor die neue Seite eröffnet wird.
- **Take-Profit & Stop-Loss:** In Pips ausgedrückt und gegen den `PriceStep` des Instruments bewertet. Die Strategie gibt Marktausstiege aus, sobald die Kerzenspanne das Ziel- oder Stop-Preisniveau berührt.
- **Break-Even-Sprung:** Sobald der unrealisierte Gewinn `BreakEvenTriggerPips` erreicht, wird der Stop auf Einstieg plus `BreakEvenOffsetPips` verschoben (oder minus bei Shorts).
- **Trailing Stop:** Nach Erreichen von `TrailingStartPips` folgt der Stop dem Preis um `TrailingStepPips` und sichert Gewinne auf beiden Seiten.
- **Kanalablehnungsausstieg:** Wenn der Preis zurück in den Kanal schließt und die Neigung gegen die Position abflacht, wird der Trade frühzeitig geschlossen.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Primärer Zeitrahmen für alle Berechnungen. |
| `TradeVolume` | Basis-Auftragsgröße. |
| `TrendLength` | LWMA-Rückblickperiode, die die Kanal-Basislinie definiert. |
| `DeviationMultiplier` | Standardabweichungsmultiplikator für die Kanalbreite. |
| `FastMaLength` / `SlowMaLength` | LWMA-Längen für den Trendfilter. |
| `MomentumPeriod` | Rückblickperiode für den Momentum-Filter. |
| `MomentumThreshold` | Mindestabweichung von 100 in einem der letzten drei Momentum-Werte. |
| `TakeProfitPips` / `StopLossPips` | Abstände der festen Ausstiegsniveaus (umgerechnet mit `PriceStep`). |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Steuert wann und wie der Break-Even-Stop aktiviert wird. |
| `TrailingStartPips` / `TrailingStepPips` | Aktiviert und dimensioniert den Trailing Stop. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD-Konfiguration. |
| `MaxPositionUnits` | Maximale absolute Nettoposition; verhindert Überheblung. |

## Verwendungshinweise
1. Fügen Sie die Strategie einem Wertpapier hinzu, das einen gültigen `PriceStep` aufweist. Pips werden durch Multiplikation dieses Schrittwerts umgerechnet.
2. Verwenden Sie `TrendLength` und `DeviationMultiplier`, um den Kanal an verschiedene Märkte anzupassen.
3. Momentum- und MACD-Filter können gelockert werden (niedrigerer Schwellenwert, kürzere Perioden), um die Handelsfrequenz zu erhöhen.
4. Die Trailing-Logik funktioniert bei Kerzenschlusskursen; Intrabar-Spitzen, die nicht über die Schwellenwerte hinausgehen, werden ignoriert.

## Unterschiede zum originalen Expert Advisor
- Die MetaTrader-Version verwendet grafische Objekte zur Ablesung der Kanalneigung und nutzt mehrere Money-Management-Zweige (Martingale-Sizing, Eigenkapitalschutz). Dieser Port behält die Neigungsprüfung bei, vereinfacht aber die Risikokontrolle auf Trades fester Größe, begrenzt durch `MaxPositionUnits`.
- Alle Ausstiege werden mit Marktorders beim Kerzenabschluss abgewickelt, da StockSharp-Strategien die MT4-Ordermodifikations-APIs nicht direkt spiegeln.
- E-Mail- und Push-Benachrichtigungen werden durch `AddInfoLog`-Nachrichten ersetzt, um die Konvertierung eigenständig zu halten.
- Kapitalbasierte Konto-Stopp-Outs wurden weggelassen; stattdessen liegt der Fokus auf positionsbezogenen Schutzfunktionen.

## Haftungsausschluss
Dieses Beispiel ist für Bildungszwecke bestimmt. Führen Sie immer Vorwärtstests durch und validieren Sie die Konfiguration, bevor Sie sie auf einem Live-Konto einsetzen.
