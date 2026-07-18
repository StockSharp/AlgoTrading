# JK BullP AutoTrader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
JK BullP AutoTrader ist ein Expertenberater, der dem Momentum folgt und ursprünglich für MetaTrader 4 geschrieben wurde. Er überwacht die Stärke der Elder Bulls
Indikator und reagiert, wenn der Aufwärtsdruck nachlässt oder negativ wird. Der Port StockSharp behält die einfache Logik des bei
originell und bietet gleichzeitig explizite Parameter, detailliertes Trailing-Management und plattformfreundliche Risikokontrollen.

## Handelslogik
1. Die Strategie abonniert eine konfigurierbare Kerzenserie (standardmäßig 1-Stunden-Kerzen) und berechnet eine 13-Perioden-Exponentialfunktion
gleitender Durchschnitt (EMA), um die Bulls Power-Basislinie zu reproduzieren.
2. Für jede abgeschlossene Kerze wird Bulls Power als Differenz zwischen dem Kerzenhoch und dem EMA-Wert gemessen.
3. Es werden zwei aufeinanderfolgende Bulls Power-Messwerte verglichen:
   - Wenn der vorherige Wert über dem neuesten Wert liegt und der letzte Wert positiv bleibt, eröffnet die Strategie eine Short-Position.
   - Wenn der aktuelle Bulls Power-Wert unter Null fällt, eröffnet die Strategie eine Long-Position.
4. Es kann jeweils nur eine Position aktiv sein, was dem ursprünglichen MQL-Experten entspricht, der neue Aufträge blockierte, während Geschäfte offen waren.

## Risikomanagement und Exits
- **Anfänglicher Stop-Loss / Take-Profit:** Abstände werden in Pips konfiguriert und mithilfe der Wertpapierpreisstufe in Preiseinheiten umgerechnet.
Beide Schutzmaßnahmen werden durch den `StartProtection`-Helfer von StockSharp aktiviert, wodurch das Verhalten nahe an den MetaTrader-Eingaben bleibt.
- **Trailing Stop:** Sobald der variable Gewinn die angegebene Trailing-Distanz überschreitet, wird der Stop-Level von Kerze zu Kerze verschoben.
Anstatt bestehende Stop-Orders zu ändern (wie in MetaTrader), gibt der Hafen eine Market-Order aus, um die Position bei Preis zu schließen
schließt über die nachlaufende Schwelle hinaus. Dies gewährleistet einen rechtzeitigen Ausgang auch dann, wenn Schutzanordnungen vom Veranstaltungsort nicht unterstützt werden.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `OrderVolume` | Marktauftragsgröße, die für Eingaben verwendet wird. | 8.5 |
| `TakeProfitPips` | Take-Profit-Distanz in Pips (umgerechnet in Preiseinheiten). | 500 |
| `StopLossPips` | Stop-Loss-Distanz in Pips. | 20 |
| `TrailingStopPips` | Gewinndistanz in Pips, die den Trailing Stop aktiviert und aufrechterhält. | 10 |
| `EmaPeriod` | Länge des EMA, der von der Bulls Power-Berechnung verwendet wird. | 13 |
| `CandleType` | Datentyp der Kerzen, die die Strategie steuern (Standardzeitrahmen: 1 Stunde). | 1-Stunden-Kerzen |

## Hinweise zur Implementierung
- Die nicht verwendeten Eingaben (`Patr`, `Prange`, `Kstop`, `kts`, `Vts`) aus dem ursprünglichen Skript wurden absichtlich weggelassen, da dies der Fall war
Keine Auswirkung auf die MetaTrader-Logik.
- Pip-Abstände hängen vom Instrument `PriceStep` ab. Wenn keine Schrittdaten verfügbar sind, wird der Wert `1` als konservativer Standardwert verwendet.
- Die Strategie verwendet StockSharps übergeordnetes `Bind` API, verarbeitet nur fertige Kerzen und behält den internen Status bei (`_previousBullsPower`).
passend zu den schichtbasierten MT4-Berechnungen.
- Die Trailing-Logik wird nach jedem Exit automatisch zurückgesetzt, um veraltete Stop-Levels zu vermeiden, wenn eine neue Position eröffnet wird.
