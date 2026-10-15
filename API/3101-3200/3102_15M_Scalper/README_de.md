# 15-Minuten-Scalper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den **15M Scalper** MetaTrader Expert Advisor auf die StockSharp High-Level-API. Sie recreiert die
Mehrfachfilter-Einstiegslogik (gewichtete gleitende Durchschnitte, Stochastik-Oszillator, Parabolic SAR, Multi-Timeframe-Momentum
und monatlicher MACD) sowie den umfangreichen Ausstiegs-Stack, der geldbasierte Ziele, Trailing Stops, Break-Even-Bewegungen und
einen Eigenkapital-Drawdown-Wächter kombiniert. Die StockSharp-Version arbeitet auf abgeschlossenen Kerzen genau wie der EA und
hält den Code ereignisgesteuert, während die ursprünglichen Parameter beibehalten werden.

## Funktionsweise

1. **Trendfilter** – schnelle und langsame *gewichtete* gleitende Durchschnitte, berechnet auf dem aktuellen Zeitrahmen (Standard
   15 Minuten), müssen mit der Handelsrichtung ausgerichtet sein. Die Durchschnitte verwenden den typischen Preis
   (`(High + Low + Close) / 3`), um dem MQL `PRICE_TYPICAL`-Input zu entsprechen.
2. **Stochastik-Umkehr** – ein 5/3/3-Stochastik-Oszillator wird an den beiden vorherigen geschlossenen Kerzen abgetastet. Long-
   Signale erfordern einen %K-Rückkreuzung über 20, während Short-Signale einen Kreuzung unter 80 erfordern, was die
   `Stoc1`/`Stoc2`-Prüfungen des Skripts widerspiegelt.
3. **Parabolic SAR-Bestätigung** – der SAR-Wert der abgeschlossenen Bar muss für Longs unter der vorherigen Eröffnung und für
   Shorts darüber liegen, was den Sicherheitsfilter `SAR < Open[1]` / `SAR > Open[1]` reproduziert.
4. **Höherer Zeitrahmen Momentum** – ein 14-Perioden-Momentum-Indikator auf dem konfigurierbaren höheren Zeitrahmen (Standard
   1 Stunde) muss bei mindestens einer der letzten drei geschlossenen Bars um mindestens die Kauf-/Verkaufsschwellen von 100
   abweichen. Dies implementiert das `MomLevelB/MomLevelS`-Trio ohne direkten Zugriff auf Indikator-Buffer.
5. **Monatlicher MACD** – eine MACD-Reihe im monatlichen Kerzenstrom (Standard 30-Tage-Bars) hält die Hauptlinie für Longs über
   dem Signal und für Shorts darunter. Derselbe MACD-Filter treibt auch die optionale Ausstiegslogik an, die Positionen schließt,
   wenn sich die Linien in entgegengesetzter Richtung kreuzen.
6. **Orderverarbeitung** – wenn ein entgegengesetztes Setup erscheint, schließt die Strategie zunächst die bestehende Position
   und wartet dann auf die nächste Bar, um Trades in der neuen Richtung zu eröffnen. Die Volumenskalierung folgt der Martingal-
   Regel des EA über `LotExponent` und den verlustempfindlichen `IncreaseFactor`.

## Risikomanagement

- **Stop Loss / Take Profit** – Abstände werden in MetaTrader-"Punkten" eingegeben und über `Security.PriceStep` in absolute
  Preise umgerechnet. Für fraktionale FX-Ticks (Preisschritt < 1) multipliziert die Implementierung den Schritt mit 10, um die
  Pip-Behandlung des EA zu imitieren.
- **Break-Even ("kein Verlust")** – sobald sich der Preis um `BreakEvenTriggerSteps` bewegt, wird der Stop virtuell zum
  Einstieg plus dem konfigurierten Offset verschoben. Wenn der Preis durch dieses Niveau zurückfällt, wird die Position zum
  Marktpreis geschlossen.
- **Trailing Stop** – ein kerzenbasierter Trailing Stop beobachtet das höchste Hoch (für Longs) oder das niedrigste Tief (für
  Shorts). Wenn der Rückgang `TrailingStopSteps` überschreitet, wird die Position geschlossen, was das ursprüngliche
  `OrderModify`-Verhalten dupliziert.
- **Geldziele** – `UseProfitTargetMoney`, `UseProfitTargetPercent` und `EnableMoneyTrailing` arbeiten mit schwebenden P&L,
  gemessen über `PriceStep` × `StepPrice`. Der Port hält die Take-Profit-, Prozentziel- und Trailing-Drawdown-Logik
  (`MoneyTrailingStop`) unverändert.
- **Eigenkapital-Stop** – `UseEquityStop` verfolgt den Höchststand von (Anfangskapital + realisiertem P&L + schwebendem
  Gewinn). Wenn der aktuelle Drawdown `TotalEquityRisk` Prozent dieses Höchststands überschreitet, wird jede Position
  geschlossen, was `AccountEquityHigh()` und `TotalEquityRisk` vom EA repliziert.
- **Martingal-Dimensionierung** – jeder zusätzliche Trade in derselben Richtung skaliert das Volumen mit `LotExponent`.
  Aufeinanderfolgende Verluste erhöhen das nächste Basisvolumen um `IncreaseFactor` pro Verlust und bieten dieselbe "adaptive"
  Lot-Dimensionierung wie der MQL `IncreaseFactor`-Zweig.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Primärer Arbeitszeitrahmen (Standard 15-Minuten-Kerzen). |
| `MomentumCandleType` | Höherer Zeitrahmen für den Momentum-Filter (Standard 1-Stunden-Kerzen). |
| `MacdCandleType` | Zeitrahmen für den MACD-Trendfilter (Standard 30-Tage-Kerzen). |
| `FastMaPeriod`, `SlowMaPeriod` | Längen der gewichteten gleitenden Durchschnitte, die den Trendfilter definieren. |
| `MomentumPeriod` | Momentum-Länge auf dem höheren Zeitrahmen. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Minimale absolute Abweichung von 100, die erforderlich ist, um Long-/Short-Trades zu erlauben. |
| `StopLossSteps`, `TakeProfitSteps` | Schutz-Stop- und Zielabstände in Preisschritten. Auf null setzen zum Deaktivieren. |
| `TrailingStopSteps` | Trailing-Stop-Abstand in Preisschritten. |
| `UseMoveToBreakeven`, `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | Break-Even-Aktivierungsschalter, Auslöseabstand und Offset. |
| `UseProfitTargetMoney`, `ProfitTargetMoney` | Geldbasiertes schwebende Gewinnziel aktivieren und konfigurieren. |
| `UseProfitTargetPercent`, `ProfitTargetPercent` | Prozentbasiertes schwebendes Gewinnziel aktivieren und konfigurieren. |
| `EnableMoneyTrailing`, `MoneyTrailingTakeProfit`, `MoneyTrailingStop` | Geld-Trailing-Auslöser und maximaler erlaubter Rückgang in Kontowährung. |
| `UseEquityStop`, `TotalEquityRisk` | Eigenkapital-Drawdown-Kontrolle aktivieren und den erlaubten Prozentsatz des Höchstkapitals festlegen. |
| `BaseVolume`, `LotExponent`, `IncreaseFactor`, `MaxTrades` | Martingal-Dimensionierungsoptionen: Anfangslot, Multiplikator, verlustbasierter Zuwachs und maximale Ergänzungen. |
| `UseExitByMacd` | Positionen schließen, wenn die MACD-Hauptlinie das Signal gegen den Trade kreuzt. |

## Verwendung

1. Hängen Sie die Strategie an ein Wertpapier und stellen Sie sicher, dass `Security.PriceStep` und `Security.StepPrice`
   ausgefüllt sind. Diese Werte werden verwendet, um pip-basierte Eingaben und Geldziele in absolute Zahlen zu übersetzen.
2. Passen Sie `CandleType`, `MomentumCandleType` und `MacdCandleType` an, wenn Sie den Scalper auf verschiedenen Zeitrahmen
   ausführen möchten. Die Standardwerte replizieren das ursprüngliche 15-Minuten-/1-Stunden-/monatliche Setup.
3. Stimmen Sie die pip-basierten Abstände (`StopLossSteps`, `TakeProfitSteps`, `TrailingStopSteps`, Break-Even-Einstellungen) auf
   die Tick-Größe des Instruments ab. Beginnen Sie mit den bereitgestellten Standardwerten und erhöhen Sie diese für volatilere
   Märkte.
4. Legen Sie Geldverwaltungspräferenzen fest: Entscheiden Sie, ob monetäre oder prozentuale Take Profits verwendet werden sollen,
   aktivieren Sie das Geld-Trailing und konfigurieren Sie den Eigenkapital-Stop, wenn Sie ein Sicherheitsnetz gegen tiefe
   Drawdowns wünschen.
5. Starten Sie die Strategie. Sie wird automatisch alle erforderlichen Kerzendatenströme abonnieren, Indikatoren zeichnen (falls
   ein Chart verfügbar ist) und beginnen, Signale zu evaluieren, sobald jeder Indikator genug Geschichte hat.

## Hinweise und Unterschiede zum Original-EA

- Der Port verwendet das aggregierte Positionsmodell von StockSharp. Wenn ein entgegengesetztes Signal erscheint, wird die
  aktuelle Position zuerst geschlossen und die neue Richtung auf der nächsten Kerze bewertet, was das Verhalten deterministisch
  hält.
- Geldbasierte Berechnungen basieren auf `Security.PriceStep` und `Security.StepPrice`. Wenn der Handelsplatz diese Werte nicht
  bereitstellt, werden die Geldziele übersprungen (der schwebende Gewinn wird als null gemeldet), genau wie in den Code-
  Kommentaren angegeben.
- `IncreaseFactor` fügt `IncreaseFactor × aufeinanderfolgende_Verluste` zum nächsten Basisvolumen hinzu, anstatt die freie
  Marge zu verwenden (die in der Sandbox-Umgebung nicht verfügbar ist). Dies erfasst dennoch die Absicht, die Größe nach
  Verlustserien zu erhöhen.
- Alle Entscheidungen werden auf abgeschlossenen Kerzen getroffen, um Doppelzählungen von Signalen zu vermeiden, was den
  Bar-für-Bar-Prüfungen der MetaTrader-Implementierung entspricht.
- Die Strategie zeichnet dieselben Indikatoren im Chart, wenn ein Visualisierer verfügbar ist, was das Debugging unterstützt
  und den Port leicht mit dem EA vergleichbar macht.

Überprüfen Sie sorgfältig die Tick-Größe, den Schrittpreis und die Volumen-Einschränkungen Ihres Brokers vor dem Live-Handel.
Diese Werte wirken sich direkt darauf aus, wie pip-basierte Abstände und Geldziele innerhalb der Strategie umgerechnet werden.
