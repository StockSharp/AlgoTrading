# Hinterhalt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Hinterhalt-Strategie umgibt den Markt kontinuierlich mit einem Paar Buy-Stop- und Sell-Stop-Aufträgen. Die ausstehenden Aufträge werden
in einem konfigurierbaren Abstand über dem besten Ask und unter dem besten Bid platziert, mit einer dynamischen Überschreibung, die eine
Mindestdistanz basierend auf dem aktuellen Spread erzwingt. Sobald eine Seite ausgelöst wird, baut die Strategie sofort beide Aufträge
neu auf, sodass der Markt von beiden Richtungen "im Hinterhalt" bleibt. Ein einfacher eigenkapitalbasierter Schutzschalter kann
Positionen abflachen, sobald ein tägliches Gewinnziel oder Verlustziel erreicht ist.

Diese C#-Implementierung repliziert das Verhalten des ursprünglichen MetaTrader 5-Experten von Zuzabush. Sie arbeitet ausschließlich mit
Level-1-Notierungen und benötigt keine Kerzen oder Indikatoren. Jede Entscheidung wird durch Echtzeit-Bid/Ask-Änderungen getrieben,
daher ist die Strategie am besten für liquide Instrumente mit engen Spreads geeignet.

## Handelslogik

1. **Marktdatenaufnahme**
   - Die Strategie abonniert Level-1-Updates und verfolgt den neuesten besten Bid und Best Ask.
   - Berechnungen werden gestoppt, bis beide Seiten des Orderbuchs verfügbar sind und die Strategie handeln darf.
2. **Eigenkapital-Schutzmaßnahmen**
   - Der realisierte PnL (`PnL`) und die unrealisierte Komponente aus dem aktuellen Bid/Ask und `PositionPrice` werden summiert.
   - Wenn das kombinierte Eigenkapital `EquityTakeProfit` überschreitet oder unter `-EquityStopLoss` fällt, wird die aktuelle
     Nettoposition mit einem Marktauftrag abgeflacht. Ausstehende Aufträge bleiben intakt und entsprechen dem ursprünglichen
     Expertenverhalten.
3. **Platzierung ausstehender Aufträge**
   - Der Spread in Preiseinheiten wird mit `MaxSpreadPoints` verglichen. Wenn der Spread zu breit ist, werden keine neuen Aufträge
     platziert.
   - Andernfalls wird eine Distanz als `max(IndentationPoints * step, spread * 3)` berechnet. Dieser Wert repliziert die MT5-Logik,
     entweder den Benutzereinzug zu respektieren oder drei Spreads zu erzwingen, wenn der Broker `StopsLevel` null ist.
   - Ein Buy-Stop-Auftrag wird bei `ask + Distanz` platziert und ein Sell-Stop bei `bid - Distanz`. Preise werden auf den nächsten
     Tick normalisiert. Nur ein aktiver Auftrag pro Seite ist erlaubt; veraltete Aufträge werden bereinigt, wenn ihr Status auf
     `Done`, `Failed` oder `Canceled` wechselt.
4. **Trailing ausstehender Aufträge**
   - Wenn `TrailingStopPoints` größer als null ist, berechnet die Strategie periodisch (nicht häufiger als `Pause`) die Stop-Distanz
     mithilfe von `max((TrailingStopPoints + TrailingStepPoints) * step, spread * 3)` neu und registriert die Aufträge erneut, wenn
     die Änderung einen halben Tick überschreitet.
   - Trailing hält die Aufträge nahe am Markt, während die Mindestdistanz eingehalten wird, die ein vorzeitiges Auslösen vermeidet.

Das Endergebnis ist eine gitterartige Ausbruchsmaschine, die ständig darauf wartet, dass der Preis in eine der Richtungen ausschlägt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `IndentationPoints` | Basisdistanz in Punkten zwischen dem Markt und jedem ausstehenden Stop-Auftrag. |
| `MaxSpreadPoints` | Maximaler zulässiger Spread (in Punkten). Aufträge werden ausgesetzt, während der Spread breiter ist. |
| `TrailingStopPoints` | Basis-Trailing-Distanz in Punkten für bestehende ausstehende Aufträge. Auf null setzen, um Trailing zu deaktivieren. |
| `TrailingStepPoints` | Zusätzlicher Puffer, der zur Basis-Trailing-Distanz hinzugefügt wird. |
| `Pause` | Mindestzeit zwischen zwei Trailing-Neuberechnungen. Der Standard entspricht der Ein-Sekunden-Pause des MT5-Experten. |
| `EquityTakeProfit` | Eigenkapitalgewinn in Kontowährung, der ein sofortiges Abflachen der Position auslöst. |
| `EquityStopLoss` | Zulässiger Eigenkapital-Drawdown, bevor die offene Position geschlossen wird. |
| `Volume` | Auftragsgröße aus der `Strategy`-Basisklasse. Das Broker-Minimum verwenden, um den MT5-Standard nachzuahmen. |

Alle Preisabstände werden von Punkten in tatsächliche Preiseinheiten umgerechnet unter Verwendung von `Security.PriceStep`. Falls das
Instrument keinen Preisschritt bietet, wird ein Fallback-Wert von 1 verwendet.

## Praktische Hinweise

- Da die Strategie nur mit Stop-Aufträgen arbeitet, sind keine Kerzen oder Indikatoren erforderlich. Sie kann bei Backtests ausgeführt
  werden, die keine historischen Kerzen liefern, solange Level-1-Daten verfügbar sind.
- Broker, die einen Nicht-Null-`StopsLevel` erzwingen, sollten `IndentationPoints` so konfigurieren, dass der resultierende
  Preisunterschied die Börsenpflicht erfüllt. Das Dreifach-Spread-Sicherheitsnetz dient als sekundäre Absicherung.
- Der Eigenkapitalfilter ist absichtlich leicht berührend und storniert keine ausstehenden Aufträge. Dies spiegelt das ursprüngliche
  Hinterhalt-Verhalten wider und ermöglicht neue Trades nach dem Abflach-Ereignis ohne manuelle Eingriffe.
- Slippage und Auftragserfüllungstoleranz werden vom verbundenen Broker oder Simulator gesteuert. `Volume` und Parameterwerte anpassen,
  um der Instrumentenvolatilität zu entsprechen.

Diese Dokumentation bietet absichtlich das maximale Detailniveau, damit sowohl diskretionäre als auch algorithmische Trader die
Konvertierung verstehen und die Strategie für ihren Ausführungsort anpassen können.
