# AMA-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die AMA-Trader-Strategie repliziert das Verhalten des ursprünglichen MetaTrader 5 Experten "AMA Trader". Sie kombiniert Kaufmans Adaptive Moving Average (AMA) mit dem Relative Strength Index (RSI), um in Trades gegen kurzfristige Pullbacks einzusteigen, solange der Preis auf der vorherrschenden Seite des adaptiven Trendfilters bleibt. Die StockSharp-Implementierung verwendet die High-Level-API mit Kerzenabonnements und Indikatorbindung, um nah an der ursprünglichen Logik zu bleiben und gleichzeitig vollständig mit dem StockSharp-Ausführungsmodell kompatibel zu sein.

## Marktannahmen
- **Instrumententyp**: für Spot-FX oder CFD konzipiert, aber auf jedes trendende Instrument anwendbar, das Averaging unterstützt.
- **Zeitrahmen**: standardmäßig Minutenkerzen, konfigurierbar über den `CandleType`-Parameter.
- **Sitzungen**: keine explizite Sitzungsbehandlung. Signale werden auf jeder fertigen Kerze ausgewertet.

## Indikatoren
1. **Kaufman Adaptive Moving Average (AMA)**
   - Glättet die Preisbewegung mit Parametern für die schnellen und langsamen Glättungskonstanten (`AmaFastPeriod`, `AmaSlowPeriod`) und die Mittelungslänge (`AmaLength`).
   - Definiert die primäre Trendrichtung. Long-Trades werden nur berücksichtigt, wenn der Schlusskurs über AMA liegt; Short-Trades nur wenn er darunter liegt.
2. **Relative Strength Index (RSI)**
   - Mit Periode `RsiLength` auf dem Kerzenschluss ausgewertet.
   - `StepLength` steuert, wie viele aktuelle RSI-Werte einen überkauften/überverkauften Zustand bestätigen müssen. Ein Wert von 0 fällt auf die Überprüfung nur des letzten Balkens zurück, was der MQL-Implementierung entspricht, bei der `StepLength == 0` als 1 behandelt wird.
   - `RsiLevelDown` (Standard 30) und `RsiLevelUp` (Standard 70) definieren überverkaufte und überkaufte Schwellenwerte.

## Handelslogik
1. **Balkenvalidierung**
   - Trades werden nur auf fertigen Kerzen und wenn die Strategie online und handelszugelassen ist ausgewertet.
2. **Gewinnmanagement vor neuen Einstiegen**
   - Wenn der nicht realisierte Gewinn aller offenen Positionen `ProfitTarget` übersteigt, schließt die Strategie jede offene Position und wartet auf das nächste Signal.
   - Wenn der realisierte Gewinn seit dem letzten Reset um mehr als `WithdrawalAmount` wächst, werden alle Positionen geschlossen und der realisierte Gewinn-Checkpoint wird aktualisiert. Dies imitiert die Rückzugsmechanik des ursprünglichen Experten (es wird kein tatsächliches Bargeld entfernt; nur der Checkpoint wird zurückgesetzt).
3. **Long-Einstiege**
   - Bedingung: Schlusskurs > AMA und mindestens einer der inspizierten RSI-Werte liegt unter `RsiLevelDown`.
   - Aktion: eine Market-Kauforder senden. Wenn das aktuelle Long-Exposure Geld verliert (negativer nicht realisierter PnL basierend auf dem nachverfolgten durchschnittlichen Einstandspreis), wird eine zusätzliche Averaging-Kauforder gesendet.
4. **Short-Einstiege**
   - Bedingung: Schlusskurs < AMA und mindestens einer der inspizierten RSI-Werte liegt über `RsiLevelUp`.
   - Aktion: eine Market-Verkauforder senden. Wenn das aktuelle Short-Exposure Verluste macht, wird eine zusätzliche Averaging-Verkauforder gesendet.
5. **Positionsverfolgung**
   - Ausführungen werden in `OnOwnTradeReceived` verarbeitet. Separate Durchschnittspreise und Volumen werden für Long- und Short-Exposure verfolgt, was genaue nicht realisierte PnL-Schätzungen ermöglicht, auch wenn der Markt zwischen Käufen und Verkäufen wechselt.

## Risikomanagement
- **Averaging-Volumen**: jeder Einstieg verwendet den festen `LotSize`. Bei Verlusten verdoppelt der Algorithmus durch Hinzufügen einer zusätzlichen Order in dieselbe Richtung.
- **Nicht realisiertes Gewinnziel**: `ProfitTarget` (Standard 50 Geldeinheiten) erzwingt einen vollständigen Exit, wenn schwebende Gewinne das angegebene Level erreichen.
- **Realisierter Gewinn-Checkpoint**: `WithdrawalAmount` (Standard 1000) schließt alle Positionen, sobald der kumulierte realisierte PnL den Schwellenwert übersteigt, woraufhin sich der Checkpoint auf den aktuellen realisierten PnL zurücksetzt.
- **Manuelle Absicherung**: kein automatischer Stop-Loss oder Take-Profit ist über das nicht realisierte Gewinnziel hinaus konfiguriert. Benutzer können bei Bedarf externe Risikokontrollen aktivieren.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Kerzendatentyp oder Zeitrahmen für Indikatorberechnungen. |
| `LotSize` | Festes Volumen für jede Marktorder. |
| `RsiLength` | RSI-Mittelungsperiode. |
| `StepLength` | Anzahl der untersuchten aktuellen RSI-Werte (0 fällt auf 1 zurück). |
| `RsiLevelUp` | RSI-Überkauft-Schwellenwert für Short-Signale. |
| `RsiLevelDown` | RSI-Überverkauft-Schwellenwert für Long-Signale. |
| `AmaLength` | AMA-Glättungsperiode. |
| `AmaFastPeriod` | Schnelle AMA-Glättungskonstante. |
| `AmaSlowPeriod` | Langsame AMA-Glättungskonstante. |
| `ProfitTarget` | Nicht realisierter Gewinn, der erforderlich ist, um alle Positionen zu glätten (0 deaktiviert die Regel). |
| `WithdrawalAmount` | Realisierter Gewinnzuwachs, der einen vollständigen Exit auslöst (0 deaktiviert die Regel). |

## Implementierungshinweise
- High-Level-API-Nutzung: Kerzen werden über `SubscribeCandles` abonniert, und AMA/RSI werden über `.Bind` an das Abonnement gebunden. Der Verarbeitungsdelegat empfängt rohe Dezimalwerte, was manuellen Indikatorzugriff vermeidet.
- Die Positionsüberwachung stützt sich auf private Akkumulatoren, die innerhalb von `OnOwnTradeReceived` aktualisiert werden. Dies spiegelt die MQL-Experten-Inspektion von Positionen wider, ohne auf verbotene aggregierende Getter zurückzugreifen.
- Orders werden mit `BuyMarket` und `SellMarket` gesendet, unter Verwendung des aktuellen `LotSize`. Das Glätten verwendet explizite Volumenargumente, damit sowohl Long- als auch Short-Exposure bereinigt werden können.
- Die StockSharp-Version verwendet den Kerzenschlusskurs anstatt der MetaTrader-Ask/Bid-Prüfung bei der Auswertung der AMA-Beziehung, was die nächste verfügbare Information in einem kerzenbasierten Workflow ist.

## Unterschiede zum MetaTrader-Experten
- `WithdrawalAmount` aktualisiert einen internen Checkpoint anstatt `TesterWithdrawal` aufzurufen, weil der StockSharp-Backtester synthetische Abhebungen nicht unterstützt.
- AMA-Verschiebungs- und angewandte Preisoptionen aus dem ursprünglichen EA sind nicht exponiert. Die StockSharp-Indikatoren arbeiten mit Kerzenschlusspreisen.
- Kommissionen und Swaps werden nicht explizit zur nicht realisierten PnL-Berechnung hinzugefügt; die StockSharp-Ausführungsumgebung verarbeitet Gebühren intern, wenn Trades abgerechnet werden.

## Verwendungstipps
- Erwägen Sie, die Strategie mit portfolioweiten Risikolimits oder dem integrierten Schutzmodul zu kombinieren, wenn das Averaging für das gehandelte Instrument zu aggressiv ist.
- Optimieren Sie AMA- und RSI-Einstellungen pro Instrument. Niedrigere Zeitrahmen profitieren oft von kürzeren AMA-Perioden und breiteren RSI-Schwellenwerten.
- Überwachen Sie Drawdowns wenn `StepLength` > 1, da das Averaging bei starken Gegentrendbewegungen mehrfach ausgelöst werden kann.
