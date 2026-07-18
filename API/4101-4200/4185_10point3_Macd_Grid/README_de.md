# TenPointThree MACD Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine C#-Portierung des MetaTrader Expert Advisors **10p3v003 (10point3.mq4)**. Es kombiniert einen MACD-Crossover-Trigger mit einer Martingale-Grid-Engine. Die ursprüngliche Logik wurde unter Verwendung des High-Levels API von StockSharp mit den folgenden Schlüsselverhalten repliziert:

- **MACD-Signallogik** – Eine Handelsrichtung wird bestimmt, wenn die MACD-Hauptlinie die Signallinie auf dem verschobenen Balken (`SignalShift`) kreuzt. Bei Long-Einträgen muss der vorherige Signalwert unter `-TradingRangePips` liegen, der aktuelle MACD-Wert muss unter Null bleiben und umgekehrt gilt für Short-Einträge. Signale können optional über `ReverseSignal` invertiert werden.
- **Gitterschichtung** – Nachdem die erste Position eröffnet wurde, sind weitere Eingaben in die gleiche Richtung nur dann zulässig, wenn sich der Preis um mindestens `GridStepPips` gegenüber der letzten Füllung bewegt. Jedes neue Bein multipliziert das Volumen mit `LotMultiplier` (oder mit `1.5`, wenn `MaxTrades > 12`) und ahmt die Martingal-Skalierung von MQL4 nach.
- **Risikoschutz** – Die letzte Etappe wird geschlossen und es werden keine weiteren Einträge hinzugefügt, wenn `OrdersToProtect` oder mehr Geschäfte aktiv sind und der variable Gewinn den Geldschwellenwert überschreitet. Der Schwellenwert basiert entweder auf dem konfigurierten Risikoprozentsatz (Geldmanagement aktiviert) oder auf der Vertragsgrößenheuristik (Geldmanagement deaktiviert).
- **Ausstiege pro Etappe** – Jede Etappe verfolgt ihren eigenen Take-Profit, virtuellen Stop-Loss und Trailing-Stop. Der Stoppabstand entspricht der Originalformel: `InitialStopPips + (MaxTrades - existingOrders) * GridStepPips`. Das Trailing wird erst aktiviert, wenn sich der Preis um `TrailingStopPips + GridStepPips` zugunsten der Position bewegt, und schließt das Bein, wenn der Preis um `TrailingStopPips` zurückgeht.
- **Sitzungsfilter** – Wenn `UseTimeFilter` aktiviert ist, werden keine neuen Grids gestartet, solange die Kerzenzeit genau zwischen `StopHour` und `StartHour` liegt, wodurch der „Gefahrenzeitzone“-Schutz aus dem Skript reproduziert wird.

Bei allen Geldumrechnungen werden die `PriceStep`/`StepPrice`-Metadaten des Wertpapiers verwendet. Wenn die Börse keine Kontraktgröße offenlegt, wird ein Fallback-Wert von `100000` angewendet, der der ursprünglichen Forex-Annahme entspricht.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `CandleType` | Candle-Abonnement, das für die Verarbeitung von MACD verwendet wird (Standard: 30-Minuten-Zeitraum). |
| `Volume` | Basislosgröße für die erste Rasterbestellung. |
| `TakeProfitPips` | Abstand in Pips für den Take-Profit jedes Beins (0 Deaktivierungen). |
| `InitialStopPips` | Basis-Stopp-Distanz in Pips. Der tatsächliche Stopp wächst mit der Anzahl der freien Rasterplätze. |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips, die angewendet wird, nachdem das Bein ausreichend profitabel ist (0 deaktiviert). |
| `MaxTrades` | Maximale Anzahl gleichzeitiger Martingaleinträge. |
| `LotMultiplier` | Multiplikator, der auf das Volumen jedes zusätzlichen Gitterzweigs angewendet wird (wird auf `1.5` überschrieben, wenn `MaxTrades > 12`). |
| `GridStepPips` | Mindestens erforderliche ungünstige Preisbewegung (in Pips), bevor der nächste Rastereintrag geöffnet wird. |
| `OrdersToProtect` | Mindestanzahl aktiver Zweige, bevor der Floating-Profit-Schutz den letzten Trade schließen kann. |
| `UseMoneyManagement` | Ermöglicht die dynamische Lotberechnung basierend auf dem Kontokapital. |
| `AccountType` | Wählt die Risikoformel aus: `0` – Standard (Eigenkapital / 10.000); `1` – Normal (Eigenkapital / 100.000); `2` – Nano (Eigenkapital / 1.000). |
| `RiskPercent` | Prozentsatz des verwendeten Eigenkapitals, wenn die Geldverwaltung aktiviert ist. |
| `ReverseSignal` | Invertiert lange/kurze MACD-Signale. |
| `FastEmaLength`, `SlowEmaLength`, `SignalLength` | MACD Zeiträume (standardmäßig 26.12.9). |
| `SignalShift` | Anzahl der geschlossenen Balken, die für die Crossover-Prüfung verwendet werden (Standard: 1). |
| `TradingRangePips` | MACD Signalband (in Pips), das durchbrochen werden muss, bevor ein Crossover akzeptiert wird. |
| `UseTimeFilter` | Aktiviert den Sitzungswächter basierend auf `StopHour`/`StartHour`. |
| `StopHour`, `StartHour` | Exklusiver Bereich, der die Erstellung eines neuen Rasters blockiert, wenn `UseTimeFilter` wahr ist. |

## Hinweise zum Geldmanagement

Wenn `UseMoneyManagement` deaktiviert ist, wird das Basislos (`Volume`) direkt verwendet. Andernfalls berechnet EA die Losgröße aus dem aktuellen Eigenkapital unter Verwendung der gleichen Formeln wie das ursprüngliche EA:

- Kontotyp **0**: `Ceil(risk% * equity / 10,000) / 10`
- Kontotyp **1**: `risk% * equity / 100,000`
- Kontotyp **2**: `risk% * equity / 1,000`

Die Volumina werden mit `Security.VolumeStep` normalisiert und dann durch `Security.MinVolume`/`MaxVolume` begrenzt.

## Ausführungsworkflow

1. Abonnieren Sie den konfigurierten Kerzenstream und füttern Sie den Indikator MACD über `BindEx`.
2. Aktualisieren Sie bei jeder fertigen Kerze die Trailing-/Stopp-Logik für aktive Beine.
3. Wenn die Crossover-Regeln MACD ausgelöst werden, stellen Sie sicher, dass der Sitzungsfilter den Handel zulässt, die Rasterrichtung mit der vorhandenen Position übereinstimmt und sich der Preis gegenüber der letzten Füllung um `GridStepPips` bewegt hat.
4. Berechnen Sie das nächste Beinvolumen mithilfe des Martingal-Multiplikators und senden Sie eine Marktorder.
5. Überwachen Sie den schwankenden Gewinn; Sobald die Schutzschwelle erreicht ist, schließen Sie das neueste Bein und pausieren Sie bis zur nächsten Kerze.

## Konvertierungshinweise

- Alle Kommentare wurden nach Bedarf in Englisch umgeschrieben.
- High-Level StockSharp API (Kerzen + `BindEx`) wird verwendet. Ein direkter Zugriff auf den Indikatorwert wird vermieden.
- Berechnungen des variablen Gewinns basieren auf `PriceStep`/`StepPrice`. Stellen Sie bei exotischen Instrumenten sicher, dass diese Felder ausgefüllt sind.
- Die Strategie verwaltet intern den Zustand pro Bein, um die Auftragsverwaltung von MQL4 zu emulieren, da StockSharp standardmäßig Positionen aggregiert.
