# Elite eFibo Trader v2.1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Elite eFibo Trader v2.1 stellt den Expert Advisor MetaTrader wieder her, der Orders der Größe Fibonacci in eine Richtung stapelt und dabei einen gemeinsamen Schutzstopp teilt. Der StockSharp-Port behält das ursprüngliche Verhalten bei: Eine einzelne Marktorder startet eine Folge von Stop-Orders im Abstand von `LevelDistancePips`, und jede gefüllte Stufe erhöht das Risiko entsprechend der Fibonacci-Progression. Die Strategie schließt sofort den gesamten Korb, sobald der gemeinsame Stop erreicht wird oder wenn der variable Gewinn `MoneyTakeProfit` erreicht.

Der Algorithmus ist absichtlich gerichtet. Setzen Sie `OpenBuy` auf `true` (und `OpenSell` auf `false`), um bullische Pullbacks zu handeln, oder legen Sie die Schalter um, um die bärische Variante auszuführen. Es ist jeweils nur ein Leiter aktiv, was die Einzelzykluslogik aus dem MQL4-Skript widerspiegelt.

## Datenanforderungen
- Abonniert den Handelsstrom, um den neuesten Ausführungspreis abzurufen, der für die Platzierung auf der Leiter, die Trailing-Logik und die Money-Take-Profit-Bewertung verwendet wird.
- Verlässt sich auf die Sicherheitsmetadaten (`PriceStep`, `StepPrice`, `VolumeStep`), um Pip-Eingaben im MetaTrader-Stil in Börsenpreise und Losgrößen zu übersetzen.

## Leiterbau
1. Wenn kein Risiko besteht und der Handel erlaubt ist, prüft die Strategie die Richtungswechsel. Genau einer von `OpenBuy` oder `OpenSell` muss wahr sein; andernfalls wird keine Leiter gestartet.
2. Die erste Fibonacci-Ebene wird zum Marktpreis geöffnet. Nachfolgende Stufen werden als Stop-Orders geplant, die um `LevelDistancePips * pipSize` vom Referenzpreis versetzt sind, der beim Start der Leiter aufgezeichnet wurde.
3. Die Volumina stammen aus den Parametern `Level1Volume` … `Level14Volume` und werden auf die Sicherheit `VolumeStep` normalisiert.
4. Alle Ebenen erben den gleichen Stopp-Offset: `StopLossPips * pipSize`. Der Stop-Preis wird pro Ausführung berechnet und später verschärft, sodass jede aktive Order das nächstgelegene Schutzniveau aufweist.

## Stoppen Sie die Verwaltung
- Für jede ausgeführte Order werden der Einstiegspreis und der aus dem Pip-Offset abgeleitete Anfangsstopp gespeichert.
- Bei jedem Handelstick bewertet die Strategie alle offenen Stopps neu und richtet sie auf den engsten Wert auf der Leiter aus (höchster Stopp für Long-Positionen, niedrigster Stopp für Shorts), um die wiederholten `OrderModify`-Anrufe von MetaTrader nachzuahmen.
- Wenn der letzte Handelspreis einen gemeinsamen Stop überschreitet, storniert die Strategie die verbleibenden ausstehenden Aufträge und schließt den gesamten Korb mit Marktaufträgen.

## Geldmanagement
- Der nicht realisierte Gewinn wird aus den Instrumenten `PriceStep` und `StepPrice` berechnet, sodass das Cash-Ziel die `OrderProfit()`-Werte von MetaTrader widerspiegelt.
- Wenn der variable Gewinn `MoneyTakeProfit` erreicht oder überschreitet, werden alle Positionen geschlossen und ausstehende Aufträge sofort storniert.
- Wenn `TradeAgainAfterProfit` den Wert `false` hat, bleibt die Strategie nach Erreichen des Geldziels inaktiv, bis sie manuell neu gestartet wird.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| `OpenBuy` | Erlauben Sie der Strategie, eine bullische Leiter aufzubauen (muss exklusiv mit `OpenSell` sein). |
| `OpenSell` | Erlauben Sie der Strategie, eine bärische Leiter aufzubauen (muss ausschließlich mit `OpenBuy` sein). |
| `TradeAgainAfterProfit` | Nehmen Sie den Handel wieder auf, nachdem der Korb aufgrund des Geld-Take-Profits geschlossen wurde. |
| `LevelDistancePips` | Abstand in MetaTrader Pips zwischen aufeinanderfolgenden Stop-Orders. |
| `StopLossPips` | Abstand in MetaTrader Pips, der zur Ableitung des Schutzstopps für jedes gefüllte Level verwendet wird. |
| `MoneyTakeProfit` | Cash-Profit-Ziel, das den gesamten Korb schließt. |
| `Level1Volume` … `Level14Volume` | Für jede Fibonacci-Ebene verwendete Volumes; auf Null setzen, um eine Stufe zu überspringen. |

## Hinweise zur Implementierung
- Die Pip-Konvertierung folgt der MetaTrader-Konvention: Wenn das Symbol 3 oder 5 Dezimalstellen hat, ist der effektive Pip gleich `PriceStep * 10`.
- `StartProtection()` wird beim Start einmal aufgerufen, um die integrierten StockSharp-Sicherheitsprüfungen zu aktivieren.
- Die gemeinsame Stopplogik hält absichtlich alle Aufträge synchron; Sobald ein engerer Stopp erscheint, wird er auf alle aktiven Ebenen übertragen.
- Ausstehende Aufträge werden automatisch bereinigt, wenn die Leiter flach ist, wodurch die mehreren `subCloseAllPending()`-Aufrufe im MQL-Code repliziert werden.

## Anwendungstipps
- Stellen Sie sicher, dass `PriceStep`, `StepPrice` und `VolumeStep` auf dem Gerät konfiguriert sind. Andernfalls können Pip-Umrechnungen oder Geldziele ungenau sein.
- Mittelungssysteme können große Engagements schnell akkumulieren. Überprüfen Sie die Volumengrenzen und Margin-Anforderungen, bevor Sie die Strategie live ausführen.
- Deaktivieren Sie `TradeAgainAfterProfit`, um das einmalige Verhalten zu reproduzieren, bei dem EA den Handel stoppt, nachdem ein profitabler Warenkorb geschlossen wurde.
