# EES Hedger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die EES Hedger-Strategie spiegelt das Verhalten des klassischen MetaTrader Expert Advisors, der automatisch Positionen absichert, die von einem anderen Handelssystem oder von manuellen Tradern eröffnet wurden. Sobald das überwachte Konto eine Position eröffnet, die dem konfigurierten Filter entspricht, eröffnet die Strategie sofort eine Gegenposition mit eigenen Parametern. Dadurch wird das gerichtete Exposure neutralisiert, während der ursprüngliche Trade weiterläuft.

Der Algorithmus basiert auf der High-Level-StockSharp-API. Er überwacht Kontotrades, eröffnet Hedge-Positionen und verwaltet Schutzorders durch Stop-Loss-, Take-Profit- und Trailing-Stop-Logik. Das Trailing-Management folgt der Originalimplementierung eng und rückt den Stop nur dann vor, wenn die Preisbewegung sowohl die Stop-Distanz als auch das Trailing-Inkrement überschreitet.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `HedgeVolume` | Fixes Volumen für die Hedge-Order. Hängt nicht von der externen Tradegröße ab. |
| `StopLossPips` | Abstand in Pips für den Schutz-Stop-Loss des Hedges. Auf null setzen, um den anfänglichen Stop zu überspringen. |
| `TakeProfitPips` | Abstand in Pips für die Take-Profit-Order. Auf null setzen, um das Ziel wegzulassen. |
| `TrailingStopPips` | Abstand in Pips, der für das Trailing verwendet wird, sobald sich der Preis günstig bewegt. |
| `TrailingStepPips` | Minimale Pip-Bewegung, die erforderlich ist, bevor der Trailing Stop erneut bewegt wird. Muss positiv sein, wenn Trailing aktiv ist. |
| `OriginalOrderComment` | Optionaler Kommentarfilter. Nur Trades, deren Kommentar mit diesem Wert übereinstimmt (Groß-/Kleinschreibung wird ignoriert), werden abgesichert. Leer lassen, um auf jeden Trade zu reagieren. |
| `HedgerOrderComment` | Optionaler Kommentar zur Identifizierung der eigenen Hedge-Trades der Strategie. Wenn angegeben, werden Trades mit demselben Kommentar ignoriert, um erneutes Hedging zu vermeiden. |

## Verhalten

1. **Trade-Erkennung** – die Strategie abonniert `NewMyTrade`-Ereignisse des Connectors. Jeder Trade, der vom ausgewählten Instrument kommt und die Kommentarfilter passiert, wird als externes Einstiegssignal behandelt.
2. **Hedge-Ausführung** – sobald ein qualifizierender Trade gesehen wird, sendet die Strategie eine Marktorder in der Gegenrichtung mit `HedgeVolume`.
3. **Schutz-Setup** – nach jeder eigenen Füllung storniert der Algorithmus bestehende Schutzorders und registriert neue Stop-Loss- und Take-Profit-Orders gemäß dem aktuellen durchschnittlichen Positionspreis.
4. **Trailing Stop** – jedes eingehende Trade-Tick wird zur Auswertung der Trailing-Regeln verwendet. Sobald sich der Preis um mindestens `TrailingStopPips + TrailingStepPips` zugunsten des Hedges bewegt hat, wird der Stop näher an den Preis gerückt. Bei Long-Positionen folgt der Stop unter dem Markt, bei Shorts über ihm.
5. **Positions-Reset** – wenn die Hedge-Position vollständig geschlossen ist (z. B. durch Stop oder Ziel), storniert die Strategie automatisch die verbleibenden Schutzorders und wartet auf den nächsten externen Trade.

## Verwendungshinweise

- Die Strategie setzt voraus, dass der Konto-Connector alle Kontotrades meldet, einschließlich der von anderen Systemen generierten.
- Die Pip-Berechnung passt sich dem Instrument-Preisschritt an und multipliziert bei 3- oder 5-stelligen Kursen mit zehn, um die MQL-Punktanpassung nachzuahmen.
- `OriginalOrderComment` so einstellen, dass er mit dem Kommentar des Primärsystems übereinstimmt, wenn nur bestimmte Trades gespiegelt werden sollen. Beim Absichern manueller Trades leer lassen.
- Sicherstellen, dass `TrailingStepPips` größer als null bleibt, wann immer Trailing aktiviert ist, um vorzeitige Beendigung beim Start zu vermeiden.
- Da der Hedger immer ein festes Volumen verwendet, empfiehlt es sich, `HedgeVolume` so anzupassen, dass der Hedge das durchschnittliche Exposure des Primärsystems abdeckt.
