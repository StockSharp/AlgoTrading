# Stoch Levels-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Stoch Levels Strategy** ist eine direkte Umsetzung des MetaTrader 4 Expertenberaters `Stoch.mq4`. Das ursprüngliche Skript basiert auf täglichen Sitzungsgrenzen, berechnet benutzerdefinierte Preisniveaus aus der vorherigen Kerze und platziert zwei ausstehende Aufträge für die kommende Sitzung. Diese C#-Version behält die gleiche Handelsidee bei und implementiert sie mit der übergeordneten Strategie API von StockSharp.

Die Strategie berechnet eine synthetische Handelsspanne, indem sie den Höchst-/Tief-Spread der vorherigen Kerze um einen konfigurierbaren Multiplikator erweitert (Standard: `1.1`). Es positioniert dann:

- Eine **Verkaufslimit**-Order über dem vorherigen Schlusskurs bei der Hälfte der erweiterten Spanne.
- Eine **Kauflimit**-Order unterhalb des vorherigen Schlusskurses bei der Hälfte der erweiterten Spanne.

Immer wenn eine ausstehende Order ausgeführt wird, fügt die Strategie sofort Bracket-Exits (Stop-Loss und Take-Profit) hinzu, wobei die in den Preisschritten definierten Abstände verwendet werden. Alle ausstehenden Exposures und ausstehenden Aufträge werden zu Beginn jedes neuen Handelstages gelöscht, was den Mitternachts-Reset-Block aus dem MQL-Skript widerspiegelt.

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie (standardmäßig täglich) und warten Sie auf vollständig fertige Kerzen.
2. Wenn eine neue Sitzung eintrifft:
   - Schließen Sie alle offenen Positionen und stornieren Sie alle Schutz- oder Einstiegsaufträge.
   - Berechnen Sie den erweiterten Bereich `range * RangeMultiplier` anhand der vorherigen Kerze.
   - Geben Sie neue Verkaufs- und Kauflimitaufträge bei `Close + range / 2` bzw. `Close - range / 2` auf.
3. Erstellen Sie bei Auftragsausführung passende Stop-Loss- und Take-Profit-Aufträge unter Verwendung der angeforderten Preisschritt-Offsets.
4. Wenn eine Schutzanordnung ausgelöst wird, stornieren Sie die Geschwister-Schutzanordnung und warten Sie auf das nächste Zurücksetzen der Sitzung.

## Parameter
| Name | Beschreibung | Standard | Notizen |
| --- | --- | --- | --- |
| `TakeProfitPoints` | Take-Profit-Distanz gemessen in Preisschritten. | `20` | Entspricht der `TakeProfit`-Eingabe im MQL-Skript. Auf `0` setzen, um die Take-Profit-Order zu deaktivieren. |
| `StopLossPoints` | Stop-Loss-Distanz gemessen in Preisschritten. | `40` | Entspricht der `StopLoss`-Eingabe im MQL-Skript. Auf `0` setzen, um die Stop-Loss-Order zu deaktivieren. |
| `RangeMultiplier` | Auf den vorherigen Kerzenbereich angewendeter Multiplikator (`High - Low`). | `1.1` | Entspricht dem hartcodierten Erweiterungsfaktor `1.1` in MQL. |
| `OrderVolume` | Volumen für jede ausstehende Bestellung. | `1` | Spiegelt den Parameter `Lots`. |
| `CandleType` | Kerzenserie, die die Handelssitzung definiert. | `Daily` | Passen Sie an, ob die Strategie auf andere Zeitrahmen angewendet werden soll. |

Alle Parameter werden über `Param()` konfiguriert, um Optimierung und UI-Bindung zu unterstützen.

## Risikomanagement
- Long-Einträge erhalten eine schützende **Verkaufsstopp**- und **Verkaufslimit**-Klammer; Shorts erhalten die gespiegelten **Buy-Stop**- und **Buy-Limit**-Exits.
- Die Größe der Bestellungen beträgt `OrderVolume`. Wenn eine Seite der Klammer ausgeführt wird, wird die verbleibende Schutzanordnung aufgehoben, um doppelte Ausgänge zu vermeiden.
- Bei jeder neuen Kerze erfolgt ein vollständiger Flat-Reset, um sicherzustellen, dass die Strategie nicht über die aktuelle Sitzung hinaus ausgesetzt ist.

## Konvertierungshinweise
- Die MQL-Implementierung verwendete MetaTrader globale Variablen, um doppelte Bestellungen zu verhindern; Die C#-Version verfolgt die zuletzt verarbeitete Sitzung intern (`_lastProcessedDay`).
- Die Nacht-Schließschleife wurde in den `ResetOrders()`-Helfer übersetzt, der alle ausstehenden Aufträge storniert und einen Marktglättungsbefehl sendet, wenn eine Position übrig bleibt.
- Stop-Loss- und Take-Profit-Level werden explizit durch StockSharp-Ordermethoden neu erstellt, anstatt in `OrderSend`-Parameter eingebettet zu werden.
- Die im MQL-Skript vorhandenen Trailing-Stop-, Money-Management- und Risikoeingaben wurden dort nicht verwendet und werden in diesem Port weiterhin nicht unterstützt.

## Nutzungstipps
1. Hängen Sie die Strategie an ein Wertpapier an und stellen Sie `OrderVolume`, Stop-Abstände und Kerzentyp so ein, dass sie mit dem gehandelten Instrument übereinstimmen.
2. Stellen Sie sicher, dass die Sicherheit einen ordnungsgemäßen `PriceStep`; Wenn nicht, greift die Strategie auf `1` zurück und protokolliert eine Warnung.
3. Da Aufträge nur einmal pro abgeschlossener Kerze neu berechnet werden, behalten Sie den standardmäßigen täglichen Zeitrahmen bei, um ihn an das ursprüngliche Verhalten anzupassen.
4. Überprüfen Sie die Protokolle, um den Arbeitsablauf für die tägliche Zurücksetzung, die Auftragserteilung und den Schutzauftragsanhang zu bestätigen.

## Dateien
- `CS/StochLevelsStrategy.cs` – Hauptstrategieumsetzung.
- `README.md`, `README_zh.md`, `README_ru.md` – mehrsprachige Dokumentation für die umgesetzte Strategie.
