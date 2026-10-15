# Gann Line-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die Kernideen des MetaTrader 4-Expertenberaters "Gann Line" (Quell-ID 24877) unter Verwendung der High-Level-API von StockSharp. Sie behält dieselben Trend-, Momentum- und langfristigen MACD-Filter bei, während alle Geldmanagement-Tools in **Preisschritten** ausgedrückt werden, was die Logik broker-unabhängig macht.

## Handelslogik

1. **Trendfilter (primärer Zeitrahmen)**
   - Zwei linear gewichtete gleitende Durchschnitte (LWMA) werden auf den typischen Kerzenpreis (high + low + close) / 3 angewendet.
   - Ein Long-Bias erfordert, dass die schnelle LWMA über der langsamen LWMA schließt; ein Short-Bias erfordert das Gegenteil.
2. **Momentum-Bestätigung (höherer Zeitrahmen)**
   - Ein auf einem konfigurierbaren höheren Zeitrahmen berechneter Momentum-Oszillator prüft, wie weit der Oszillator vom Gleichgewichtsniveau (100) abweicht.
   - Mindestens einer der letzten drei abgeschlossenen Momentum-Werte muss den konfigurierten Abweichungsschwellenwert überschreiten, bevor ein Trade erlaubt ist.
3. **Langsamer MACD-Filter (sehr hoher Zeitrahmen)**
   - Ein auf einem langsamen Zeitrahmen (standardmäßig monatlich) berechneter MACD-Filter muss die Richtung bestätigen: MACD-Hauptlinie über Signal für Longs, unter für Shorts.
4. **Positionsmanagement**
   - Feste Stop-Loss- und Take-Profit-Ziele werden von Preisschritten in absolute Preise umgerechnet, wenn ein Trade eröffnet wird.
   - Optionale Break-even-Logik verschiebt den Stop zum Einstiegspreis plus einem Offset, sobald der Trade eine bestimmte Anzahl von Schritten in Gewinn geht.
   - Optionale Trailing-Logik verschiebt den Stop hinter das höchste Hoch (für Longs) oder niedrigste Tief (für Shorts), sobald der Preis eine konfigurierbare Anzahl von Schritten zurückgelegt hat.

## Risikomanagement

- Alle Abstände (Stop-Loss, Take-Profit, Break-even und Trailing) werden in Preis-**Schritten** eingegeben. Der Helper konvertiert sie in Preise mit dem Instrument-`PriceStep`.
- Die Strategie arbeitet mit der Basis-`Volume`-Eigenschaft. Wenn sie null ist, wird standardmäßig ein Kontrakt/Lot verwendet.
- Nur eine einzige Nettoposition wird verwaltet. Entgegengesetzte Signale schließen den aktuellen Trade, bevor ein neuer geöffnet wird.

## Unterschiede zur MQL4-Version

- Der ursprüngliche Expertenberater verwendete eine manuell gezeichnete Gann-Trendlinie. StockSharp exponiert keine Diagrammobjekte, daher ersetzt der Port diese Prüfung durch die LWMA-Steigungsbestätigung.
- Geldbasiertes Trailing, Teilschließungen und kontoweite Eigenkapitalprüfungen aus dem Skript werden in deterministische schrittbasierte Berechnungen vereinfacht.
- Benachrichtigungen (Alerts, E-Mails, mobile Pushes) werden nicht generiert, da StockSharp-Strategien typischerweise auf der Plattformausgabe protokollieren.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `Fast LWMA` | Länge der schnellen LWMA für den Trendfilter. |
| `Slow LWMA` | Länge der langsamen LWMA für den Trendfilter. |
| `Momentum Period` | Rückblick des Momentum-Oszillators auf dem sekundären Zeitrahmen. |
| `Momentum Threshold` | Minimale Abweichung von 100 für einen der letzten drei Momentum-Werte. |
| `MACD Fast / Slow / Signal` | EMA-Längen des langsamen MACD-Filters. |
| `Take Profit (steps)` | Take-Profit-Abstand in Preisschritten. |
| `Stop Loss (steps)` | Stop-Loss-Abstand in Preisschritten. |
| `Use Trailing`, `Trail Activation`, `Trail Distance` | Trailing aktivieren, Gewinn vor Trailing-Start, Abstand zwischen Preisextrem und Trailing-Stop. |
| `Use BreakEven`, `BreakEven Activation`, `BreakEven Offset` | Break-even aktivieren, Gewinn vor Stop-Verschiebung, danach gesicherter Zusatzgewinn. |
| `Primary Timeframe` | Kerzentyp für den LWMA-Crossover. |
| `Momentum Timeframe` | Kerzentyp für den Momentum-Oszillator. |
| `MACD Timeframe` | Kerzentyp für den langsamen MACD-Filter. |

## Verwendungstipps

1. Wählen Sie ein Instrument und stellen Sie den gewünschten `Primary Timeframe` ein. Die anderen Zeitrahmen haben standardmäßig 1 Stunde (Momentum) und 30 Tage (MACD), können aber angepasst werden.
2. Konfigurieren Sie `Volume` und die schrittbasierten Risikoparameter entsprechend Ihrer Broker-Kontraktspezifikationen.
3. Führen Sie die Strategie in `Designer` oder über Code aus. Überwachen Sie das Protokoll, um zu verifizieren, dass Filter, Break-even-Bewegungen und Trailing-Anpassungen wie erwartet erscheinen.
4. Optimieren Sie Momentum- und MACD-Schwellenwerte, um die portierte Logik an verschiedene Märkte oder Zeitrahmen anzupassen.

## Weitere Verbesserungen

- Einen eigenkapitalbasierten globalen Stop ähnlich dem Originalskript integrieren.
- Den LWMA-Steigungsfilter durch eine benutzerdefinierte chartgezeichnete Trendlinie ersetzen, sobald StockSharp Objektereignisse exponiert.
- Partielle Gewinnmitnahmen hinzufügen, um das Multi-Ziel-Verhalten der MQL4-Version nachzuahmen.
