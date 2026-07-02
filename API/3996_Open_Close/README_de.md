# Open Close-Strategie (ID 3996)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den MetaTrader 4-Experten `open_close.mq4`. Es funktioniert auf einem einzelnen Instrument und vergleicht den Eröffnungs- und Schlusskurs der letzten Kerze mit dem vorherigen. Wenn keine Position aktiv ist, werden starke Ein-Takt-Bewegungen (Gap-and-Reversal-Muster) ausgeblendet. Während eines Handels wird die Position entweder dann geschlossen, wenn sich das Muster umkehrt oder wenn ein Schwellenwert für den Floating-Loss-Schutz überschritten wird.

## Handelslogik
### Einreisebestimmungen
- Wird nur gehandelt, wenn die vorherige Kerze verarbeitet wurde (der ursprüngliche `Volume[0] == 1`-Schutz).
- Long-Einstieg: Die aktuelle Kerze öffnet sich über dem vorherigen Eröffnungskurs **und** schließt unter dem vorherigen Schlusskurs. Die Strategie kauft das konfigurierte Volumen zum Marktpreis.
- Short-Einstieg: Die aktuelle Kerze öffnet sich unter dem vorherigen Eröffnungskurs **und** schließt über dem vorherigen Schlusskurs. Die Strategie verkauft Leerverkäufe zum Marktwert.

Es kann immer nur eine Position aktiv sein. Neue Signale werden ignoriert, bis die offene Position geschlossen wird.

### Ausgangsregeln
1. **Risikoschutz:** Der variable PnL wird anhand des durchschnittlichen Einstiegspreises gemessen. Wenn der nicht realisierte Verlust `MaximumRisk × Portfolio.CurrentValue` übersteigt, schließt die Strategie die Position sofort. Die ursprüngliche MQL-Version verwendete `AccountMargin`, was hier mit der besten verfügbaren Portfoliobewertung angenähert wird.
2. **Musterumkehr:**
   - Long-Positionen werden geschlossen, wenn die nächste Kerze weiter nach unten geht (`open < previous open` und `close < previous close`).
   - Short-Positionen werden geschlossen, wenn die nächste Kerze weiter nach oben geht (`open > previous open` und `close > previous close`).

## Positionsgrößen
- Die Standardbestellgröße wird von `MaximumRisk` abgeleitet. Die Strategie multipliziert den verfügbaren Kontowert mit `MaximumRisk` und dividiert das Ergebnis durch `1000`, wodurch die MetaTrader-Berechnung von `AccountFreeMargin * MaximumRisk / 1000` nachgeahmt wird.
- Wenn die Kontoinformationen nicht verfügbar sind, wird der Fallback-Parameter `InitialVolume` verwendet.
- Nach mehr als einem aufeinanderfolgenden Verlustgeschäft wird die Losgröße um `volume × losses / DecreaseFactor` reduziert, wodurch die MetaTrader-Schleife über den Verlauf der geschlossenen Geschäfte reproduziert wird.
- Es wird ein handelbares Mindestvolumen von `0.1` Lots erzwungen, bevor die Menge an den Volumenschritt des Instruments und die Börsenlimits angepasst wird.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `InitialVolume` | `decimal` | `0.1` | Fallback-Losgröße, die verwendet wird, wenn keine Eigenkapitalinformationen verfügbar sind. |
| `MaximumRisk` | `decimal` | `0.3` | Bruchteil des Kontowerts, der sowohl die Positionsgröße als auch den maximal tolerierten Floating-Verlust steuert. |
| `DecreaseFactor` | `decimal` | `100` | Der Reduktionsfaktor wird nach mehr als einem Verlusthandel in Folge angewendet. |
| `CandleType` | `DataType` | `15m` Zeitrahmen | Zur Bewertung des Musters verwendete Kerzenserie. |

## Implementierungshinweise
- Die Strategie abonniert die ausgewählte Kerzenserie und verarbeitet **nur fertige Kerzen**, die der `Volume[0] > 1`-Bedingung im ursprünglichen Experten entsprechen.
- Der variable PnL wird anhand der aktuellen Position der Strategie und des letzten Schlusskurses geschätzt, da StockSharp die Kennzahlen `AccountProfit` und `AccountMargin` von MetaTrader nicht offenlegt.
- Aufeinanderfolgende Verluste werden durch abgeschlossene Geschäfte verfolgt, sodass sich `DecreaseFactor` wie die ursprüngliche Schleife über den Handelsverlauf verhält.
- Bei der Volumenausrichtung werden `Security.VolumeStep`, `MinVolume` und `MaxVolume` berücksichtigt, um mit den Exchange-Anforderungen kompatibel zu bleiben.
- Charts werden mit Kerzen und eigenen Trades gefüllt, wenn ein Chartbereich für visuelles Debuggen verfügbar ist.

## Nutzungstipps
- Wählen Sie ein Kerzenintervall, das dem in MetaTrader bei der Kalibrierung des ursprünglichen Experten verwendeten entspricht.
- Passen Sie `MaximumRisk` und `DecreaseFactor` an, um die Aggressivität der Losgrößenregel anzupassen.
- Da die Strategie konträr ist, funktioniert sie am besten bei Instrumenten, die häufige Einzelbalken-Überdehnungen und Snap-Back-Bewegungen aufweisen.
