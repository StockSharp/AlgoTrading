# Hoop Master Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertiert vom MetaTrader-5-Expert-Advisor **„Hoop master 2"** von Vladimir Karputov.
- Baut eine Ausbruchsbox um den aktuellen Preis und bewaffnet sowohl Kauf- als auch Verkaufsstop-Orders jedes Mal, wenn eine neue Kerze schließt.
- Reproduziert automatisch das MT5-Verhalten, die Lotgröße nach einem Verlust-Trade zu verdoppeln und nach einem profitablen Zyklus zurückzusetzen.

## Handelslogik
1. Die konfigurierte Kerzenserie abonnieren und nur abgeschlossene Kerzen abwarten. Eine neue Kerze fungiert als „Tick", der die ausstehenden Orders neu bewaffnet.
2. Wenn die Strategie flach ist:
   - Ein **Buy-Stop** `IndentPips` Punkte über dem letzten Schluss platzieren.
   - Ein **Sell-Stop** `IndentPips` Punkte unter dem letzten Schluss platzieren.
   - MetaTrader-Pips in absolute Preiseinheiten umwandeln mit dem Instrument-`PriceStep` und der Bruchziffern-Anpassung (×10 für 3 oder 5 Dezimalstellen-Kurse).
3. Jede ausstehende Order speichert ihre eigenen Stop-Loss- und Take-Profit-Niveaus. Sobald die Order ausgeführt wird, wird die gegenüberliegende Order storniert und der gespeicherte Schutz mit nativen Börsenorders neu erstellt (`SellStop`/`SellLimit` für Longs, `BuyStop`/`BuyLimit` für Shorts).
4. Wenn eine Schutzorder die Position schließt, wird die verbleibende angehängte Order storniert, um doppelte Ausstiege zu vermeiden.
5. Optionale Trailing-Stop-Logik bewegt den Schutz-Stop zugunsten des Trades, sobald der Preis mindestens `TrailingStopPips` vorgerückt ist und die Verbesserung `TrailingStepPips` überschreitet.
6. Nach jedem flach-zu-flach-Handelszyklus wird der realisierte PnL ausgewertet. Ein negativer Zyklus multipliziert das Arbeitsvolumen mit `LossMultiplier`; andernfalls wird das Volumen auf das Basis-`Volume` zurückgesetzt.

## Parameter
| Parameter | Beschreibung | Standard | Hinweise |
|-----------|-------------|---------|-------|
| `Volume` | Basis-Ordergröße beim Bewaffnen neuer ausstehender Orders. | Strategie-`Volume`-Eigenschaft | Verdoppelt nach einem Verlustzyklus gemäß `LossMultiplier`. |
| `StopLossPips` | Stop-Loss-Abstand in MetaTrader-Pips. | `25` | Mit Pip-Größen-Helfer in Preis umgewandelt. `0` deaktiviert den Stop. |
| `TakeProfitPips` | Take-Profit-Abstand in MetaTrader-Pips. | `70` | In Preis umgewandelt. `0` deaktiviert das Ziel. |
| `TrailingStopPips` | Abstand zwischen Preis und Trailing-Stop. | `0` | Auf `0` setzen zum Deaktivieren des Trailings. |
| `TrailingStepPips` | Mindestverbesserung bevor der Trailing-Stop bewegt wird. | `5` | Nur verwendet wenn `TrailingStopPips` größer als null ist. |
| `IndentPips` | Offset zum letzten Schluss beim Bewaffnen ausstehender Orders. | `15` | Stellt sicher, dass Stop-Orders außerhalb des unmittelbaren Preisrauschens sitzen. |
| `LossMultiplier` | Multiplikator für den nächsten Zyklus nach einem Verlust. | `2` | Implementiert die Martingal-Positionsgrößengebung des MT5-EA. |
| `CandleType` | Kerzentyp/-zeitrahmen, der das Neu-Bewaffnen auslöst. | `1-Stunden-Zeitrahmen` | Ändern, um dem bei Tests verwendeten Chart zu entsprechen. |

## Geldmanagement und Schutz
- Jeder ausgeführte Einstieg baut sofort seinen Stop-Loss und Take-Profit als echte Börsenorders neu auf, damit die Schutzmaßnahmen auch bei Strategietrennung funktionieren.
- `StartProtection()` wird beim Start aufgerufen, um verirrte Positionen aus vorherigen Läufen zu liquidieren.
- Trailing-Logik passt bestehende Stop-Orders statt Marktausstiege zu senden an, was das Verhalten konsistent mit MT5-Modifikationen hält.

## Implementierungshinweise
- Folgt der hochrangigen StockSharp-API: Kerzenabonnements, `BuyStop`/`SellStop` für Einstiege und `BuyLimit`/`SellLimit` für Take-Profit-Orders.
- Alle Textkommentare im Code sind auf Englisch, während externe Dokumentation (dieses README und Übersetzungen) detaillierte Beschreibungen für Benutzer bereitstellt.
- MetaTrader-Pip-Konvertierung berücksichtigt Bruchziffern-Symbole (3 oder 5 Dezimalstellen) durch Multiplizieren des Broker-Schritts mit 10, entsprechend der `m_adjusted_point`-Logik des ursprünglichen EA.
