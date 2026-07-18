# Blau C-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein StockSharp-Port des MetaTrader Expert Advisors **Exp_BlauCMomentum**. Sie handelt auf einem einzelnen Instrument mit Kerzen aus einem konfigurierbaren Zeitrahmen und interpretiert Blaus dreifach geglättetes Momentum in einem von zwei Modi:

* **Breakdown-Modus** – reagiert darauf, dass die Momentum-Linie das Nullniveau kreuzt.
* **Twist-Modus** – reagiert auf Änderungen in der Richtung der geglätteten Momentum-Steigung.

Der Indikator wird auf einem externen Zeitrahmen berechnet und kann optional verschiedene angewendete Preise für die Momentum-Berechnung verwenden. Positionen werden mit Marktaufträgen eröffnet und können mit integrierten Stop-Loss- und Take-Profit-Modulen geschützt werden.

## Funktionsweise
1. Kerzen des gewählten Zeitrahmens abonnieren.
2. Blau C-Momentum berechnen:
   * Das rohe Momentum ist die Differenz zwischen zwei angewendeten Preisen, die durch `MomentumLength` Balken getrennt sind.
   * Das rohe Momentum wird dreimal durch die gewählte Methode des gleitenden Durchschnitts geglättet und auf Preisschritte skaliert (×100/Point).
3. Die geglättete Indikatorhistorie für durch `SignalBar` definierte Balkenverschiebungen speichern.
4. Signale generieren:
   * **Breakdown** – wenn der vorherige Balken über null lag und der Signalbalken unter oder gleich null ist, Long eröffnen/flippen; wenn der vorherige Balken unter null lag und der Signalbalken über oder gleich null ist, Short eröffnen/flippen. Optionale Ausstiegflags schließen die Gegenseite, wenn der vorherige Balken die Nulllinie kreuzt.
   * **Twist** – zwei vorherige Balken vergleichen; wenn das Momentum nach oben beschleunigt (vorheriger &lt; älterer) und der Signalbalken bestätigt, Long eröffnen/flippen; wenn das Momentum nach unten beschleunigt (vorheriger &gt; älterer) und der Signalbalken bestätigt, Short eröffnen/flippen. Optionale Ausstiegflags schließen die Gegenseite unter gleicher Bedingung.
5. `MoneyManagement` und `MarginModes` zur Positionsgröße verwenden. Negative Werte bedeuten festes Volumen; positive Werte riskieren oder allozieren einen Bruchteil des Portfoliowertes. Eine einfache Zeitsperre verhindert sofortige Wiedereinstiege innerhalb derselben Kerze.

## Parameter
| Gruppe | Name | Beschreibung |
|-------|------|-------------|
| Handel | `MoneyManagement` | Kapitalanteil für die Positionsgröße. Negativer Wert = festes Volumen. |
| Handel | `MarginModes` | Interpretation des Money Managements (`FreeMarginShare`, `BalanceShare`, `FreeMarginRisk`, `BalanceRisk`). Risiko-Modi verwenden Stop-Loss-Abstand und `StepPrice`. |
| Risiko | `StopLossPoints` | Stop-Loss-Abstand in Preisschritten des Instruments (auf `0` setzen zum Deaktivieren). |
| Risiko | `TakeProfitPoints` | Take-Profit-Abstand in Preisschritten des Instruments (auf `0` setzen zum Deaktivieren). |
| Handel | `SlippagePoints` | Erlaubter Slippage (zur Kompatibilität beibehalten, nicht für Auftragsplatzierung verwendet). |
| Handel | `EnableLongEntry`, `EnableShortEntry` | Eröffnung von Long-/Short-Positionen erlauben. |
| Handel | `EnableLongExit`, `EnableShortExit` | Schließen bestehender Positionen gemäß dem Indikator erlauben. |
| Logik | `EntryModes` | `Breakdown` oder `Twist`. |
| Daten | `CandleType` | Zeitrahmen für Indikatorberechnungen (Standard 4h). |
| Indikator | `SmoothingMethod` | Methode des gleitenden Durchschnitts: `Simple`, `Exponential`, `Smoothed`, `LinearWeighted`, `Jurik`, `TripleExponential`, `Adaptive`. |
| Indikator | `MomentumLength` | Durchschnittstiefe des rohen Momentums (Balken zwischen den beiden Preiswerten). |
| Indikator | `FirstSmoothLength`, `SecondSmoothLength`, `ThirdSmoothLength` | Längen der drei Glättungsstufen. |
| Indikator | `Phase` | Jurik-Phasenparameter (verwendet wenn Glättungsmethode `Jurik` ist). |
| Indikator | `PriceForClose`, `PriceForOpen` | Angewendete Preise für das Momentum (siehe Codekommentare für Formeln). |
| Logik | `SignalBar` | Balkenindex für Signale (0 = aktuell geschlossener Balken, 1 = vorheriger Balken, etc.). |

## Verwendungshinweise
* Strategie einem Wertpapier zuweisen und die Kerzenserie konfigurieren. Der Handels-Zeitrahmen ist derselbe wie der Indikator-Zeitrahmen.
* Das High-Level-API-Schutzmodul wird automatisch aktiviert, wenn Stop-/Take-Profit-Werte positiv sind.
* Margin-Modi sind Annäherungen, da StockSharp kein MetaTrader-ähnliches Balance-/Freie-Margin-Konzept aufzeigt. Risikobasierte Modi verlassen sich auf `StopLossPoints` und `Security.StepPrice`.
* Erweiterte Glättungsmethoden aus der Originalbibliothek (Parabolic, VIDYA, JurX) werden auf die nächstgelegenen verfügbaren StockSharp-Indikatoren abgebildet (`TripleExponential` ≈ T3, `Adaptive` ≈ KAMA).
* Der Slippage-Parameter wird zur Vollständigkeit beibehalten, aber es werden Marktaufträge verwendet, daher ist der Wert informativ.

## Erste Schritte
1. Verbindung, Portfolio und Wertpapier in Ihrer StockSharp-Umgebung konfigurieren.
2. Instanz von `BlauCMomentumStrategy` erstellen, `Security`, `Portfolio` und gewünschte Parameter zuweisen.
3. `Start()` aufrufen; die Strategie abonniert Kerzen, berechnet den Indikator und handelt automatisch.
4. Logs für Informationen über geöffnete/geschlossene Positionen und Indikatorzustände überwachen.

## Risikohinweis
Diese Strategie wird zu Bildungszwecken bereitgestellt. Validieren Sie immer die Leistung mit historischen und Vorwärtstests, bevor Sie sie auf einem Live-Konto ausführen. Passen Sie die Risikoeinstellungen an Ihr Kapital und die Marktbedingungen an.
