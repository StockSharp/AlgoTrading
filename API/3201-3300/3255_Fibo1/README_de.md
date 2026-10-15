# FIBO1-Strategie (MQL 24845-Konvertierung)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **FIBO1-Strategie** reproduziert die Handelsregeln des ursprünglichen Expertenberaters `FIBO1.mq4` von Aharon Tzadik (MQL-Skript 24845) unter Verwendung der High-Level-API von StockSharp. Die Strategie handelt ein einzelnes Symbol auf einem ausgewählten Zeitrahmen und kombiniert drei Filtergruppen:

1. **Trendfilter** – eine schnelle und eine langsame Linear Weighted Moving Average (LWMA) des typischen Preises. Long-Signale erfordern, dass die schnelle LWMA über der langsamen LWMA bleibt, während Shorts die umgekehrte Beziehung erfordern.
2. **Momentum-Bestätigung** – drei aufeinanderfolgende Momentum-Werte werden gegen benutzerdefinierte Kauf-/Verkaufsschwellenwerte verglichen. Der Algorithmus ahmt die absolute Abweichung von 100 nach, die der MQL-Code auf höheren Zeitrahmen verwendete.
3. **MACD-Filter** – ein übergeordneter Zeitrahmen-MACD muss die Handelsrichtung bestätigen. Der StockSharp-Port behält die Standardwerte 12/26/9 bei und prüft die Beziehung zwischen MACD-Haupt- und Signallinie genau wie im Expertenberater.

Sobald eine Position aktiv ist, recreiert die Strategie die anspruchsvolle Exit-Logik von `FIBO1.mq4`:

- Traditionelle pip-basierte Stop-Loss- und Take-Profit-Abstände.
- Optionale geld-/prozentbasierte Take-Profit- und Trailing-Ziele.
- Kerzenbasierte Trailing-Stops, die jüngsten Hochs/Tiefs folgen, inklusive eines zusätzlichen Preis-Puffers identisch zur "PAD AMOUNT"-Einstellung.
- Klassische Trailing-Abstände, die nach einem Mindestgewinn-Schwellenwert aktivieren.
- Automatischer Break-even-Schutz mit einem in Pips ausgedrückten Offset.
- Ein Equity-Stop, der schwebenden Drawdown gegen den historischen Equity-Peak überwacht.

> **Hinweis:** Der ursprüngliche MQL-Experte verwendete eine manuell gezeichnete "FIBO"-Linie auf dem Chart für den Live-Handel. StockSharp-Strategien können nicht auf Terminal-Zeichenobjekte zugreifen, daher verhält sich der Port immer wie der Testbranch des MQL-Codes (der Teil, der den Fibonacci-Retracement-Filter ignoriert). Alle anderen Funktionen bleiben erhalten.

## Handelslogik

1. **Signalerkennung**
   - Auf eine abgeschlossene Kerze auf dem primären Zeitrahmen warten.
   - Sicherstellen, dass die schnelle LWMA über (Long) oder unter (Short) der langsamen LWMA liegt.
   - Das Preismuster prüfen, das das vorherige Kerzen-Hoch/Tief-Paar vergleicht, `Low[2] < High[1]` für Longs und `Low[1] < High[2]` für Shorts spiegelnd.
   - Die maximale absolute Abweichung der letzten drei Momentum-Werte vom neutralen Niveau 100 auswerten. Wenn sie den konfigurierten Schwellenwert überschreitet, besteht der Momentum-Filter.
   - Bestätigen, dass die übergeordnete MACD-Hauptlinie über (Long) oder unter (Short) ihrer Signallinie bleibt.
   - Wenn alle Filter übereinstimmen, jede entgegengesetzte Exposition umkehren und eine Market-Order mit dem konfigurierten Handelsvolumen öffnen.

2. **Risikomanagement**
   - Jede neue Position erhält sofort pip-basierte Stop-Loss- und Take-Profit-Orders über die StockSharp Protective API.
   - Break-even-Logik zieht den Stop an, sobald der schwebende Gewinn den Aktivierungsschwellenwert erreicht.
   - Preis-basiertes Trailing kann in zwei Modi arbeiten: (a) Kerzenextremen mit einem Pad-Offset folgen, oder (b) einen festen Pip-Abstand beibehalten, nachdem der Trade in den Gewinn übergeht.
   - Ein Geldmanagement-Modul handhabt kassenbasierte Ziele, Prozent-von-Equity-Ziele und einen schwebenden Gewinn-Trailing-Stop identisch zum ursprünglichen EA.
   - Der globale Equity-Stop verfolgt kontinuierlich das höchste Equity-Niveau seit dem Start und schließt alle Positionen, wenn der maximal erlaubte Drawdown überschritten wird.

## Parameter

| Name | Standard | Beschreibung |
|------|---------|-------------|
| `UseMoneyTakeProfit` | `false` | Alle Positionen schließen, wenn der unrealisierte Gewinn `MoneyTakeProfit` erreicht (Kontowährung). |
| `MoneyTakeProfit` | `10` | Gewinnziel in Kontowährung. Wirksam nur wenn `UseMoneyTakeProfit = true`. |
| `UsePercentTakeProfit` | `false` | Ein Gewinnziel als Prozentsatz des anfänglichen Equity-Snapshots aktivieren. |
| `PercentTakeProfit` | `10` | Prozentsatz für das equity-basierte Gewinnziel. |
| `EnableMoneyTrailing` | `true` | Aktiviert geldbasiertes Trailing, sobald unrealisierter Gewinn `MoneyTrailTarget` erreicht. |
| `MoneyTrailTarget` | `40` | Minimaler schwebender Gewinn für die Geld-Trailing-Logik. |
| `MoneyTrailStop` | `10` | Maximaler zulässiger Drawdown (in Währungseinheiten) nach Geld-Trailing-Aktivierung. |
| `UseEquityStop` | `true` | Globalen Equity-Drawdown-Schutz aktivieren. |
| `EquityRiskPercent` | `1` | Maximaler Drawdown (Prozent des Peak-Eigenkapitals) vor dem Schließen aller Positionen. |
| `TradeVolume` | `1` | Basisvolumen (Lots/Kontrakte) für Market-Einstiege. |
| `FastMaPeriod` | `20` | Periode der schnellen LWMA auf dem typischen Preis. |
| `SlowMaPeriod` | `100` | Periode der langsamen LWMA auf dem typischen Preis. |
| `MomentumPeriod` | `14` | Länge des Momentum-Indikators für den Bestätigungsfilter. |
| `MomentumBuyThreshold` | `0.3` | Minimale absolute Abweichung von 100 für Long-Trades. |
| `MomentumSellThreshold` | `0.3` | Minimale absolute Abweichung von 100 für Short-Trades. |
| `MacdFastPeriod` | `12` | Schnelle EMA-Länge im übergeordneten MACD. |
| `MacdSlowPeriod` | `26` | Langsame EMA-Länge im übergeordneten MACD. |
| `MacdSignalPeriod` | `9` | Signal-EMA-Länge im übergeordneten MACD. |
| `TakeProfitPips` | `50` | Schutz-Take-Profit-Abstand in Pips. |
| `StopLossPips` | `20` | Schutz-Stop-Loss-Abstand in Pips. |
| `TrailingActivationPips` | `40` | Mindestgewinn (Pips) vor Aktivierung des pip-basierten Trailings. |
| `TrailingDistancePips` | `40` | Vom preis-basierten Trailing-Stop gehaltener Abstand. |
| `UseCandleTrailing` | `true` | Wenn aktiviert, folgt der Trailing-Stop jüngsten Kerzenextremen statt einem festen Abstand. |
| `CandleTrailingLength` | `3` | Anzahl abgeschlossener Kerzen zur Berechnung des Trailing-Extrems. |
| `CandleTrailingOffsetPips` | `3` | Zusätzlicher Pip-Puffer für den Kerzen-Trailing-Preis. |
| `MoveToBreakEven` | `true` | Break-even-Schutz aktivieren. |
| `BreakEvenActivationPips` | `30` | Gewinn (Pips) vor Break-even-Stop-Verschiebung. |
| `BreakEvenOffsetPips` | `30` | Offset (Pips) über dem Einstiegspreis bei Break-even-Anwendung. |
| `CandleType` | `15m` | Primäre Kerzenserie für Handelssignale. |
| `MomentumCandleType` | `15m` | Kerzenserie für den Momentum-Indikator. |
| `MacdCandleType` | `1d` | Übergeordnete Zeitrahmenserie für den MACD-Filter. |

## Verwendungshinweise

- Die Standard-Kerzentypen spiegeln die Multi-Timeframe-Logik des Expertenberaters wider: die Haupt- und Momentum-Serien verwenden den Chart-Zeitrahmen, während MACD auf einem höheren Zeitrahmen (täglich standardmäßig) arbeitet. Alle drei Serien können rekonfiguriert werden.
- Die Pip-Konvertierungsroutine berücksichtigt automatisch 3/5-Dezimal-Forex-Symbole durch Multiplikation des Preisschritts mit 10. Instrumente mit anderen Tick-Größen behalten den rohen `PriceStep`-Multiplikator.
- Die Strategie basiert ausschließlich auf abgeschlossenen Kerzen. Stellen Sie sicher, dass der verbundene Datenanbieter Kerzelzustände veröffentlicht, sonst triggern Eintrittsbedingungen nie.
- Wenn das Symbol in einer Netting-Umgebung handelt, werden Positions-Umkehrungen durch Schließen der entgegengesetzten Exposition vor dem Öffnen eines neuen Trades ausgeführt, genau wie der ursprüngliche EA mit Market-Orders.

## Unterschiede zum ursprünglichen EA

- Fibonacci-Retracement-Objektprüfungen sind nicht vorhanden, da StockSharp nicht auf MT4-Chart-Zeichnungen zugreifen kann. Die Strategie verhält sich immer wie der Testbranch des MQL-Codes.
- Geldmanagement-Parameter (`Lots`, `LotExponent` und `Max_Trades`) wurden durch eine einzige `TradeVolume`-Eigenschaft ersetzt, da StockSharp-Strategien auf Nettopositionen operieren. Volumen-Skalierung kann extern über Optimizer geskriptet werden.
- Alle Protokollierungs- und Alarmierungsroutinen (`Alert`, `SendMail`, `SendNotification`) wurden absichtlich entfernt.

Mit diesen Anpassungen bleibt der StockSharp-Port der Handelslogik von `FIBO1.mq4` treu und bietet eine saubere, parametrisierte Implementierung.
