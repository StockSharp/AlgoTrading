# Williams Percent Directional Index-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Williams Percent Directional Index Strategy** erstellt den MetaTrader 5-Experten „Mt5 Williams % Directional Index EA“ unter Verwendung des High-Level-API von StockSharp nach. Es kombiniert den Williams %R-Oszillator mit dem Average Directional Index (ADX), um Momentum-Wende zu identifizieren, und verlässt sich dann auf den Money Flow Index (MFI) und den Stochastic-Oszillator, um Trades zu beenden. Die Implementierung verarbeitet nur fertige Kerzen und verwendet Indikatorbindungen, sodass jede Entscheidung auf dem zuletzt abgeschlossenen Balken basiert.

## Handelslogik
1. **Trendausrichtung**
   - Williams %R muss bei Long-Trades steigen oder bei Short-Trades fallen. Die Strategie vergleicht die Werte der beiden zuvor fertiggestellten Balken, um die Impulssteigung zu ermitteln.
   - Die Richtungsbewegungskomponente des ADX (`+DI - -DI`) muss auf dem letzten geschlossenen Balken die Nulllinie überschritten haben: Ein negativer zu positiver Übergang bestätigt ein bullisches Momentum, während ein positiver zu negativer Übergang ein bärisches Momentum bestätigt.
2. **Eintrittsregeln**
   - Wenn beide bullischen Bedingungen erfüllt sind und die aktuelle Position flach oder short ist, eröffnet die Strategie eine Marktkauforder.
   - Wenn beide rückläufigen Bedingungen erfüllt sind und die aktuelle Position flach oder lang ist, eröffnet die Strategie einen Marktverkaufsauftrag.
   - Wenn sowohl Long- als auch Short-Signale gleichzeitig auftreten (selten, aber bei identischen Werten möglich), wird der Handel übersprungen, um widersprüchliche Anweisungen zu vermeiden.
3. **Ausgangsregeln**
   - Long-Positionen schließen, wenn entweder der MFI-Wert von vor zwei Balken das überkaufte Niveau überschreitet oder die Stochastic-Hauptlinie ein lokales Tiefpunktmuster (`K[−2] > K[−1] < K[0]`) bildet.
   - Short-Positionen werden geschlossen, wenn entweder der MFI-Wert von vor zwei Balken unter das gespiegelte überverkaufte Niveau (`100 - level`) fällt oder die Stochastic-Hauptlinie ein lokales Spitzenmuster bildet (`K[−2] < K[−1] > K[0]`).
4. **Risikohandhabung**
   - Bei der Konvertierung bleiben die Ein- und Ausstiegsmechanismen des ursprünglichen Expert Advisors erhalten. Stop-Loss- und Trailing-Funktionen aus der Quelle MQL werden nicht reproduziert; Die Risikokontrolle sollte extern verwaltet oder bei Bedarf über StockSharp-Schutzmaßnahmen hinzugefügt werden.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Candle Type` | Zeitrahmen für alle Indikatorberechnungen. | 15-minütiger Zeitrahmen |
| `Williams %R Period` | Rückblickzeitraum, der im Williams %R-Oszillator verwendet wird. | 42 |
| `Directional Period` | Zeitraum für ADX-Berechnungen (beeinflusst +DI/−DI). | 20 |
| `MFI Period` | Länge des Geldflussindex. | 19 |
| `MFI Level` | Überkaufter Schwellenwert, der zum Auslösen von Ausstiegen verwendet wird. Der überverkaufte Wert wird als `100 - value` berechnet. | 79 |
| `Stochastic %K` | %K Periode des stochastischen Oszillators. | 22 |
| `Stochastic %D` | %D Periode des stochastischen Oszillators. | 16 |
| `Stochastic Smoothing` | Zusätzliche Glättung („Verlangsamung“) des stochastischen Oszillators. | 21 |

Alle Parameter werden als `StrategyParam`-Werte angezeigt, sodass sie über die StockSharp-GUI optimiert oder angepasst werden können.

## Nutzungshinweise
- Binden Sie die Strategie an ein beliebiges Instrument und stellen Sie vor dem Start eine entsprechende Lautstärke ein.
- Die Strategie verarbeitet nur abgeschlossene Kerzen (`CandleStates.Finished`) und garantiert so, dass die Indikatorwerte endgültig sind.
- Die Diagrammdarstellung ist aktiviert: Williams %R, ADX, MFI, Stochastic und ausgeführte Trades werden dargestellt, wenn ein Diagrammbereich verfügbar ist.
- Um das ursprüngliche MT5-Verhalten hinsichtlich der Stoppverwaltung wiederherzustellen, sollten Sie bei Bedarf das Hinzufügen von `StartProtection` oder einer benutzerdefinierten Risikologik in Betracht ziehen.

## Unterschiede zur MQL-Version
- Die StockSharp-Konvertierung verwendet Indikatorbindungen anstelle des manuellen Pufferkopierens, aber die logischen Prüfungen, einschließlich Nulldurchgangsvalidierung und Multi-Bar-Muster, folgen dem MT5-Expertenberater.
- Sitzungsfilter, Wiederholungslogik und Trailing-Stop-Verwaltung aus dem MQL-Code werden absichtlich weggelassen, um sich auf die für diese Konvertierung erforderliche Kernsignal-Engine zu konzentrieren.
