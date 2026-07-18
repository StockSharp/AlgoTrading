# SuperForexV2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
SuperForexV2 ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `SuperForexV2.mq4`. Das ursprüngliche Drehbuch kombiniert eine kurzfristige
Relative-Stärke-Index-Oszillator (RSI) mit festen Take-Profit-, Stop-Loss- und Trailing-Stop-Abständen. Die C#-Implementierung
baut den gleichen Entscheidungsprozess mit dem High-Level StockSharp API neu auf: Die Strategie beobachtet fertige Kerzen und reagiert auf RSI
Schwellenüberschreitungen und verwaltet eine einzelne Nettoposition mithilfe von Pip-basierten Risikolimits.

## Handelslogik
1. **Indikatorpipeline**
   - Abonniert die konfigurierbare Kerzenserie (standardmäßig 15-Minuten-Balken) und speist jeden fertigen Balken in einen RSI-Indikator ein.
   - Die Länge von RSI ist konfigurierbar und standardmäßig auf den ursprünglichen MT4-Wert von 4 eingestellt.
2. **Dynamische Positionsgrößenbestimmung**
   - Vor jedem Einstieg leitet die Strategie eine Arbeitslosgröße aus dem aktuellen Portfoliowert dividiert durch `BalanceToVolumeDivider` ab.
   - Das resultierende Volumen wird durch `InitialVolume` (Fallback, wenn der Saldo unbekannt ist) und `MaxVolume` begrenzt und dann auf gerundet
Lautstärkeschritt des Instruments.
3. **Eintrittsregeln**
   - Wenn keine offene Position vorhanden ist und RSI unter `RsiLowerLevel` fällt, wird eine Market-Buy-Order platziert.
   - Wenn RSI über `RsiUpperLevel` steigt, wird ein Marktverkaufsauftrag übermittelt.
4. **Exit- und Risikomanagement**
   - Jede Position speichert absolute Stop-Loss- und Take-Profit-Level, die aus den Pip-basierten Abständen berechnet werden.
   - Bei jeder fertigen Kerze prüft die Strategie, ob der Balken diese Niveaus berührt hat; Ist dies der Fall, wird die Position zum Marktwert geschlossen.
   - Ein Trailing Stop ahmt die MT4-Logik nach: Sobald der Preis um mindestens `TrailingStopPips` gestiegen ist, wird der Stop näher herangezogen, sodass der
Der aktuelle Gewinn ist festgeschrieben.
   - Positionen werden auch immer dann geschlossen, wenn der RSI das entgegengesetzte Extrem erreicht (z. B. werden Long-Positionen geschlossen, wenn RSI das obere Niveau überschreitet).
5. **Positionsbereich**
   - Der Bot spiegelt das „Ein Trade pro Symbol“-Verhalten von EA wider, indem er ein Flat Book erzwingt, bevor neue Einträge ausgewertet werden.

## Parameter
| Name | Beschreibung | Standard | Notizen |
| --- | --- | --- | --- |
| `CandleType` | Kerzenserien, die die Indikatorberechnungen steuern. | `15m` Zeitrahmen | Akzeptiert alle vom Connector unterstützten `DataType`. |
| `RsiPeriod` | RSI Lookback-Länge. | `4` | Muss größer als Null sein. |
| `RsiUpperLevel` | Überkaufter Schwellenwert für Short- und Long-Exits. | `62` | Entspricht der MT4-Eingabe `Pos`. |
| `RsiLowerLevel` | Überverkaufter Schwellenwert für Long- und Short-Exits. | `42` | Entspricht der MT4-Eingabe `Neg`. |
| `TakeProfitPips` | Take-Profit-Distanz, ausgedrückt in Pips. | `109` | Auf `0` setzen, um den Take-Profit zu deaktivieren. |
| `StopLossPips` | Stop-Loss-Distanz, ausgedrückt in Pips. | `9` | Auf `0` setzen, um den Stop-Loss zu deaktivieren. |
| `TrailingStopPips` | Trailing-Stop-Distanz, ausgedrückt in Pips. | `6` | Auf `0` setzen, um das Nachlaufverhalten zu deaktivieren. |
| `InitialVolume` | Fallback-Losgröße, wenn der Portfoliosaldo nicht verfügbar ist. | `0.1` | Wird auch verwendet, wenn die dynamische Größenanpassung einen nicht positiven Wert ergibt. |
| `MaxVolume` | Maximal zulässiges Volumen pro Eintrag. | `100` | Verhindert eine Überskalierung der ausgleichsbasierten Größenbestimmung. |
| `BalanceToVolumeDivider` | Auf den Kontostand angewendeter Teiler zur Berechnung des Volumens. | `10000` | Repliziert die MT4-Formel `Lots = AccountBalance()/10000`. |

## Implementierungshinweise
- Die Kerzenverarbeitung erfolgt erst nach `CandleStates.Finished`, um das Tick-End-Verhalten von MT4 nach `start()` widerzuspiegeln und gleichzeitig zu vermeiden
unvollständige Daten.
- Pip-Abstände werden mithilfe des `PriceStep` des Instruments in absolute Preise umgerechnet. Für 3- und 5-stellige Forex-Symbole der Code
multipliziert den Schritt mit zehn, sodass der „Pip“ StockSharp mit der Punktdefinition MetaTrader übereinstimmt.
- Stop-Loss-, Take-Profit- und Trailing-Levels werden intern gespeichert und mit Kerzenhochs und -tiefs verglichen, weil StockSharp
verwaltet Stops auf Orderebene im MT4-Stil nicht automatisch.
- Die Strategie rundet das berechnete Volumen auf das nächste gültige Los und berücksichtigt dabei `MinVolume`, `MaxVolume` und `VolumeStep`.
Grenzen, die durch das Wertpapier aufgedeckt werden.
- Es ist jeweils nur eine Nettoposition zulässig; Die Einstiegslogik wird vorzeitig beendet, wenn die Strategie bereits Long oder Short ist.

## Unterschiede im Vergleich zur MT4-Version
- Der StockSharp-Port funktioniert bei fertigen Kerzen statt bei einzelnen Ticks, sodass Intrabar-Stopp- oder Zieltreffer auf dem erkannt werden
Nächste Bar schließen.
- Der `AccountFreeMargin()`-Schutz von MetaTrader wird durch ein sichereres, aus dem Gleichgewicht abgeleitetes Volumen ersetzt. wenn der Stecker das nicht bereitstellen kann
Portfoliowert wird der Fallback `InitialVolume` verwendet, anstatt abzubrechen.
- Die Stop-Loss- und Take-Profit-Werte der Order werden nicht an den Broker übermittelt. Stattdessen schließt die Strategie Positionen einmal pro Level zum Marktwert
wird verletzt, da hochrangige StockSharp-Aufträge auf strategiegesteuerten Exits basieren.
- Die Eingabe `NumeroMagico` zum Filtern von MT4-Bestellungen ist in StockSharp unnötig und wurde weggelassen.
- Protokollierungsmeldungen vom Original EA werden nicht reproduziert; Im weiteren Verlauf sollten die eigenen Protokollierungsfunktionen von StockSharp genutzt werden
Instrumentierung ist erforderlich.
