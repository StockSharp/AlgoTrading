# Gselector-Musterwahrscheinlichkeitsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Gselector Pattern Probability**-Strategie ist eine StockSharp-Portierung des MetaTrader 4 „Gselector“-Experten. Es untersucht Richtungsänderungen synthetischer Preisreihen, die aus mehreren Schrittgrößen erstellt wurden, führt Wahrscheinlichkeitsstatistiken für jedes beobachtete Muster und handelt, wenn die Wahrscheinlichkeit einer Fortsetzungsbewegung hoch genug ist. Stop-Loss- und Take-Profit-Abstände werden in der Software simuliert, um das ursprüngliche Expertenverhalten widerzuspiegeln.

## Lernprozess
1. **Synthetische Leitern** – Für jedes konfigurierte Delta-Vielfache erstellt die Strategie eine stufenbasierte Reihe, indem der letzte Schlusskurs jedes Mal aufgezeichnet wird, wenn sich der Markt um die erforderliche Distanz bewegt.
2. **Musterkodierung** – Eine Bitmaske wird erstellt, indem jedes Paar benachbarter Werte innerhalb der Leiter verglichen wird. Steigende Schritte erhalten das Bit `0`, fallende Schritte erhalten das Bit `1`, das die `Ncomb`-Codierung aus der MQL-Implementierung reproduziert.
3. **Ereignisverfolgung** – Wenn ein neues Muster auftritt, startet die Strategie Beobachter für jedes konfigurierte Stoppniveau. Ein Beobachter speichert den Ursprungspreis und wartet, bis sich der Preis um den Schwellenwert nach oben oder unten bewegt.
4. **Wahrscheinlichkeitsaktualisierung** – Sobald ein Beobachter fertig ist, erhöhen Aufwärtsbewegungen die „Wachstums“-Statistik, Abwärtsbewegungen erhöhen die „Abnahme“-Statistik. Ein Vergessensfaktor emuliert die Zerfallslogik (`forg`) des ursprünglichen Experten.
5. **Persistenz im Speicher** – Alle Statistiken werden im Speicher gehalten und beim Start der Strategie zurückgesetzt, was dem Verhalten der MQL-Version entspricht, wenn `ReadHistory` deaktiviert ist.

## Handelslogik
1. Für das aktuelle Muster werden auf jeder Delta-Leiter Fortsetzungswahrscheinlichkeiten berechnet.
2. Ein Kaufsignal erfordert:
   - Wahrscheinlichkeit ≥ `ProbabilityThreshold`.
   - Beobachtungen ≥ `MinSamples`.
   - Die Abklingzeit seit dem letzten Kauf ist abgelaufen.
   - Wenn eine Short-Position besteht, muss die neue Wahrscheinlichkeit die gespeicherte Verkaufswahrscheinlichkeit plus `ProbabilityBuffer` überschreiten.
3. Ein Verkaufssignal spiegelt die Kaufregeln mit vertauschten Wachstums-/Rückgangsrollen wider.
4. Einträge verwenden `BuyMarket` / `SellMarket`, um `OrderSend` zu emulieren. Wenn die entgegengesetzte Position offen ist, schließt die Strategie diese zuerst und reproduziert so das Umkehrverhalten des Expertenberaters.
5. Schutzausstiege werden intern abgewickelt: Stops und Takes werden in Preiseinheiten ausgedrückt, die aus dem Punktwert und dem Stop-Level abgeleitet werden.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Kerzendatentyp, der für den Backtest/die Live-Sitzung verwendet wird. | Zeitrahmen von 1 Minute |
| `ProbabilityThreshold` | Mindestfortsetzungswahrscheinlichkeit, die zur Eröffnung eines Handels erforderlich ist. | 0,8 |
| `BaseDeltaPoints` | Basispunktabstand, der die erste synthetische Leiter definiert. | 1 |
| `DeltaSteps` | Anzahl der auszuwertenden Deltaleitern. | 20 |
| `PatternLength` | Anzahl der Elemente im Leiterverlauf. | 10 |
| `StopLevels` | Anzahl der Stop/Take-Levels. | 1 |
| `StopDistancePoints` | Basis-Stopp/Take-Distanz in Punkten. | 25 |
| `ForgetFactor` | Der Rückgang wird nach jeder Beobachtung auf die Wachstums-/Abnahmezähler angewendet. | 1.05 |
| `MinSamples` | Mindestanzahl abgeschlossener Beobachtungen. | 10 |
| `ProbabilityBuffer` | Zusätzliche Wahrscheinlichkeit erforderlich, um die Gegenposition zu schließen. | 0,05 |
| `FixedVolume` | Basishandelsvolumen. | 1 Los |
| `UseReinvest` | Ermöglicht eine Balance-proportionale Lautstärkeanpassung. | wahr |
| `VolumeMode` | 0 – fest, 1 – Prozent pro 10.000, 2 – Leiter, 3 – linear. | 1 |
| `PercentPer10k` | Prozentsatz pro 10.000 Einheiten im Modus 1. | 3 |
| `BaseDeposit` | Grundeinzahlung für die Modi 2 und 3. | 500 |
| `DepositStep` | Einzahlungserhöhung für die Modi 2 und 3. | 500 |
| `MaxVolume` | Maximale Lautstärkebegrenzung. | 10000 |
| `CooldownFactor` | Anzahl der Kerzenintervalle, die als Reaktivierungs-Cooldown verwendet werden. | 2 |

## Unterschiede zum MQL Expert
- Die dateibasierte Persistenz wurde entfernt; Statistiken werden bei jedem Start der Strategie von Grund auf neu erstellt.
- Bestellungen werden durch `BuyMarket`/`SellMarket` und Software-Stopp-Management anstelle von MT4 ausstehenden Bestellungen simuliert.
- Die Positionsgrößen-Helfer wurden an StockSharp Portfoliodaten angepasst. Liegen keine Eigenkapitalwerte vor, greift die Strategie auf das Fixvolumen zurück.
- Trailing-Stop-Eingaben aus dem Originalcode werden ignoriert, da sie in der MT4-Version nie angewendet wurden.

## Nutzungshinweise
- Hängen Sie die Strategie mit einem gültigen `PriceStep` an ein Wertpapier an. Wenn der Schritt unbekannt ist, fällt die Strategie auf 0,0001 zurück.
- Der Lernprozess erfordert eine Mindestanzahl an Leiteraktivierungen; Erwarten Sie eine Aufwärmphase, bevor der Handel beginnt.
- Durch Erhöhen von `DeltaSteps` oder `PatternLength` steigt die Speichernutzung exponentiell, da das Musterwörterbuch schnell wächst.
- Der Standardwahrscheinlichkeitsschwellenwert (0,8) ist sehr streng. Senken Sie den Wert für häufigere Trades.
