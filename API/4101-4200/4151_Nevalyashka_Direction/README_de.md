# Nevalyashka-Richtungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Nevalyashka-Strategie ist eine C#-Portierung des ursprünglichen MetaTrader 4 Expert Advisors `Nevalyashka.mq4`. Der EA ändert wiederholt seine Handelsrichtung: Er eröffnet eine einzelne Marktorder, wartet, bis die Position durch einen Stop-Loss, Take-Profit oder eine manuelle Aktion geschlossen wird, und steigt sofort wieder in die entgegengesetzte Richtung mit demselben Volumen ein. Die StockSharp-Implementierung reproduziert dieses Verhalten und stellt gleichzeitig alle kritischen Einstellungen als Strategieparameter bereit.

## Handelslogik
1. **Initialisierung**
   - Wenn die Strategie startet, berechnet sie die Pip-Größe aus dem `PriceStep` des Instruments. Für 3- und 5-stellige Forex-Symbole wird der Schritt mit 10 multipliziert, um der Punktdefinition MetaTrader zu entsprechen.
   - `StartProtection` ist mit Stop-Loss- und Take-Profit-Abständen konfiguriert, die von Pips in Preispunkte umgewandelt werden. Jeder weiteren Position werden Schutzanordnungen beigefügt.
   - Eine erste Marktorder wird in die durch `InitialDirection` definierte Richtung gesendet (Standard: Short). Das angeforderte Volumen wird unter Verwendung der Werte `VolumeStep`, `MinVolume` und `MaxVolume` des Wertpapiers auf das nächste gültige Los gerundet.

2. **Positionsverfolgung**
   - `OnPositionChanged` erfasst jede Änderung des Nettoengagements. Wenn eine neue Position eröffnet wird, speichert die Strategie das gefüllte Volumen und merkt sich die Handelsseite.
   - Sobald die Position wieder vollständig stabil ist, gibt die Strategie sofort eine neue Marktorder in die entgegengesetzte Richtung aus und verwendet dabei die zuvor gespeicherte Lotgröße wieder.

3. **Fehlerbehandlung**
   - Wenn der Broker eine Auftragsregistrierung ablehnt, wird das Flag für die ausstehende Richtung gelöscht, sodass der Plattformbetreiber es manuell erneut versuchen oder die Parameter anpassen kann, ohne dass der interne Status veraltet ist.

Der resultierende Arbeitsablauf spiegelt die „Rollenspiel“-Idee des ursprünglichen Skripts wider: Der Bot ist immer auf dem Markt und wechselt zwischen Long- und Short-Positionen mit festen Ausgängen.

## Parameter
| Name | Beschreibung | Standard | Notizen |
| --- | --- | --- | --- |
| `StopLossPips` | Abstand des Schutzstopps in Pips. | `50` | Über die Pip-Größenberechnung in Preispunkte umgerechnet; auf `0` setzen, um den Stopp zu deaktivieren. |
| `TakeProfitPips` | Abstand des schützenden Take-Profits in Pips. | `50` | Wird auf die gleiche Weise wie der Stop-Loss umgerechnet; auf `0` setzen, um den Take-Profit zu deaktivieren. |
| `Volume` | Lotgröße, die für den allerersten Handel verwendet wurde. | `1` | Nach der ersten Füllung verwendet die Strategie das tatsächlich ausgeführte Volumen für alle zukünftigen Einträge wieder. |
| `InitialDirection` | Seite der ursprünglichen Marktorder. | `Sell` | Wählen Sie zwischen `Buy` und `Sell`, um der gewünschten Startneigung zu entsprechen. |

## Implementierungshinweise
- Es sind keine Kerzen- oder Indikatorabonnements erforderlich. Die Strategie reagiert ausschließlich auf Positionsereignisse und Auftragsbestätigungen.
- `IsFormedAndOnlineAndAllowTrading()` wird vor jedem Eintrag konsultiert, um sicherzustellen, dass der Connector für den Handel bereit ist.
- Bei der Volumenrundung wird `MidpointRounding.AwayFromZero` verwendet, sodass Bruchteile von Lots immer auf einem handelbaren Niveau statt auf Null einrasten.
- Die Pip-Konvertierungslogik basiert auf Instrumentenmetadaten und nicht auf fest codierten Annahmen, wodurch der Port über FX-, CFD- oder Futures-Symbole mit unterschiedlichen Preisformaten hinweg funktioniert.

## Unterschiede zur MQL-Version
- Die Variante StockSharp stellt die Startrichtung als Parameter bereit, anstatt den anfänglichen Short aus dem MT4-Skript zu erzwingen.
- Stop-Loss- und Take-Profit-Orders werden über `StartProtection` verwaltet, wodurch native Schutzorder erstellt werden, die mit jedem StockSharp-Connector kompatibel sind.
- Durch Ablehnungen von Bestellungen wird der interne Status „Ausstehend“ gelöscht, um die wiederholte Einreichung ungültiger Anfragen zu vermeiden.

Diese Anpassungen behalten den Geist des ursprünglichen Beraters bei und integrieren sich nahtlos in das StockSharp-High-Level-API.
