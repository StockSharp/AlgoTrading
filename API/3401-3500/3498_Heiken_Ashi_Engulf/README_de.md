# Heiken Ashi Engulf-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie repliziert das Verhalten der MetaTrader 5 Experten **heiken ashi engulf ea buy mt5.mq5** und **heiken ashi engulf sale ea mt5.mq5**, indem beide Richtungen in einer einzigen StockSharp-Strategie auf hoher Ebene kombiniert werden. Es rekonstruiert klassische Heiken-Ashi-Kerzen aus dem abonnierten Zeitrahmen, wartet auf ein verschlingendes Muster, bestätigt es mit der Ausrichtung des gleitenden Durchschnitts und zwei RSI-basierten Filtern und eröffnet schließlich eine Marktposition mit optionalen festen Stop-Loss- und Take-Profit-Abständen, ausgedrückt in MetaTrader Pips.

Durch die Konvertierung bleiben die ursprünglichen „Kauf“- und „Verkauf“-Konfigurationen getrennt, sodass jede Seite unabhängig optimiert werden kann. Ein Richtungswähler ermöglicht es Händlern, nur die bullische, nur die bärische oder beide Strategien gleichzeitig zu verfolgen.

## Handelslogik
### Heiken Ashi-Rekonstruktion
1. Für jede abgeschlossene Kerze erstellt die Strategie die Eröffnungs-, Höchst-, Tiefst- und Schlusswerte von Heiken Ashi unter Verwendung der vorherigen synthetischen Eröffnungs- und Schlusswerte (Standard-MT-Algorithmus).
2. Zwei historische Heiken Ashi-Kerzen (`shift = 1` und `shift = 2`) werden gespeichert, um die `Shift`-Parameter aus dem MetaTrader-Code zu emulieren.

### Lange Einrichtung
1. Es ist keine offene Position zulässig (entspricht dem Block `NoOpenedOrders`).
2. Die neueste Heiken Ashi-Kerze muss bullisch und die vorherige bärisch sein (`ChosenCandleType = 1`, `PreviousCandleType = 2`).
3. Die letzte echte Kerze muss über dem Hoch der Kerze davor schließen (`Close[1] > High[2]`), während die vorherige Kerze bärisch sein muss (`Close[2] < Open[2]`).
4. Der Heiken Ashi-Schlusskurs der neuesten Kerze muss über dem gleitenden Basisdurchschnitt (`iMA` mit Parametern `BuyBaselineMethod/Period`) bleiben.
5. Der MA des schnellen Trends muss über dem MA des langsamen Trends liegen (`BuyFast` vs. `BuySlow`).
6. Zwei RSI-Filter müssen ihre Werte innerhalb der konfigurierten Grenzwerte für die angegebene Anzahl von Kerzen halten (dieselbe Logik wie der `IndicatorWithinLimits`-Block, einschließlich des Ausnahmezählers).
7. Wenn alle Bedingungen erfüllt sind, kauft die Strategie das angeforderte Volumen, wandelt die konfigurierten Stop-Loss- und Take-Profit-Abstände von Pips in Preiseinheiten um und setzt Schutzaufträge über `SetStopLoss` / `SetTakeProfit`. Eine optionale Protokollmeldung repliziert die Warnung MetaTrader.

### Kurze Einrichtung
Die kurze Logik spiegelt die langen Regeln mit entgegengesetzten Vergleichen wider:
1. Flache Position.
2. Die neueste Heiken Ashi-Kerze ist bärisch und die vorherige bullisch.
3. Die letzte echte Kerze schließt unter dem Tief der Kerze davor (`Close[1] < Low[2]`), und die vorherige Kerze ist bullisch.
4. Der Schlusskurs von Heiken Ashi bleibt unter dem bärischen Basis-MA, während der schnelle MA unter dem langsamen MA bleibt.
5. Beide RSI-Filter bleiben innerhalb ihrer Grenzen und verwenden ihre eigene Schicht-/Perioden-/Ausnahmekonfiguration.
6. Es wird ein Marktverkaufsauftrag erteilt und die Stop-Loss-/Take-Profit-Abstände für Shorts werden angewendet.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | H1 | Für alle Indikatoren und Signale verwendeter Zeitrahmen. |
| `Direction` | Beides | Welche Seite des Engulfing-Playbooks sollte aktiv sein (`BuyOnly`, `SellOnly`, `Both`). |
| `BuyVolume` | 0,01 | Lotgröße für Long-Trades. |
| `BuyStopLossPips` | 50 | MetaTrader Pips zwischen Einstieg und Stop-Loss für Long-Positionen. `0` deaktiviert den Festanschlag. |
| `BuyTakeProfitPips` | 50 | MetaTrader Pips zwischen Einstieg und Take-Profit für Long-Positionen. `0` deaktiviert das feste Ziel. |
| `BuyBaselinePeriod` / `BuyBaselineMethod` | 20 / Exponentiell | MA im Vergleich zur bullischen Heiken Ashi-Kerze (Spiegel `inp1_Ro_*`). |
| `BuyFastPeriod` / `BuyFastMethod` | 20 / Exponentiell | Schneller Trend-MA (`inp12_Lo_*`). |
| `BuySlowPeriod` / `BuySlowMethod` | 30 / Exponentiell | Langsamer Trend-MA (`inp12_Ro_*`). |
| `BuyPrimaryRsi*` | 14, Schicht 1, Fenster 2, Ausnahmen 0, Grenzen [0;100] | Erster RSI-Filter (entspricht `inp13_*`). |
| `BuySecondaryRsi*` | 5, Schicht 2, Fenster 3, Ausnahmen 0, Grenzen [0;100] | Zweiter RSI-Filter (`inp14_*`). |
| `SellVolume` | 0,01 | Lotgröße für Short-Trades. |
| `SellStopLossPips` | 50 | MetaTrader Pips zwischen Einstieg und Stop-Loss für Shorts. |
| `SellTakeProfitPips` | 50 | MetaTrader Pips zwischen Einstieg und Take-Profit für Shorts. |
| `SellBaselinePeriod` / `SellBaselineMethod` | 20 / Exponentiell | Basislinien-MA für rückläufige Setups (`inp15_*`). |
| `SellFastPeriod` / `SellFastMethod` | 20 / Exponentiell | Schneller Trend-MA (`inp26_Lo_*`). |
| `SellSlowPeriod` / `SellSlowMethod` | 30 / Exponentiell | Langsamer Trend-MA (`inp26_Ro_*`). |
| `SellPrimaryRsi*` | 14, Schicht 1, Fenster 2, Ausnahmen 0, Grenzen [0;100] | Erster RSI-Filter für Kurzfilme (`inp27_*`). |
| `SellSecondaryRsi*` | 5, Schicht 2, Fenster 3, Ausnahmen 0, Grenzen [0;100] | Zweiter RSI-Filter für Kurzfilme (`inp28_*`). |
| `AlertTitle` | „Warnmeldung“ | Text, der bei Eröffnung eines Handels in das Protokoll geschrieben wird. |
| `SendNotification` | wahr | Aktiviert die Info-Log-Nachricht, die MetaTrader Pop-ups/Benachrichtigungen ersetzt. |

## Risikomanagement
- Stop-Loss- und Take-Profit-Abstände werden von MetaTrader Pips in Preiseinheiten umgerechnet. Bei der Konvertierung wird der Wert automatisch entsprechend der Tick-Größe des Wertpapiers skaliert (Unterstützung für 3/5-stellige Notierungen inbegriffen).
- Wenn ein neuer Trade ausgeführt wird, wird die erwartete resultierende Position an `SetStopLoss` / `SetTakeProfit` übergeben, wodurch die ursprüngliche virtuelle/reale Stop-Platzierung nachgeahmt wird.
- In der Quelle EA war keine zusätzliche nachgestellte Logik vorhanden und wird daher nicht eingeführt.

## Notizen
- Die RSI-Filter verwenden die gleiche „Fenster mit Ausnahmen“-Logik wie der MetaTrader-Builder. Wenn die Anzahl der verfügbaren Kerzen nicht ausreicht, wird das Handelssignal ignoriert, bis genügend Historie gesammelt wurde.
- Die Heiken Ashi-Werte werden pro Kerze zwischengespeichert, sodass die Indikatorverschiebungen (`Shift + CandlesShift`) dem Verhalten der ursprünglichen `.mq5`-Dateien entsprechen.
- Wenn Sie `Direction` auf `BuyOnly` oder `SellOnly` setzen, wird die Gegenseite vollständig deaktiviert, ohne ihre Parameter zu ändern, was bei der Optimierung hilfreich ist.
