# VLT Trader Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **VLT Trader Filter-Strategie** ist ein Volatilitätskontraktions-Ausbruchssystem, das aus der ursprünglichen MQL-Implementierung konvertiert wurde. Es überwacht aktuelle Kerzenspannen und bereitet Stop-Orders vor, wann immer die zuletzt abgeschlossene Kerze zur kleinsten Spanne in einem konfigurierbaren historischen Fenster wird. Ziel ist es, explosive Bewegungen nach einer engen Konsolidierungsphase zu erfassen.

## Handelslogik

1. **Neue Balkenverarbeitung** – die Strategie wertet Bedingungen nur einmal pro neuer Kerze aus. Die aktuelle Kerze muss unter dem Hoch der vorherigen Kerze öffnen, um das Handeln von Gaps zu vermeiden, die durch das Ausbruchsniveau springen.
2. **Volatilitätsfilter** – die Spanne der zuletzt abgeschlossenen Kerze wird mit der kleinsten Spanne unter den letzten `CandleCount` abgeschlossenen Kerzen verglichen, deren Spanne unter `MaxCandleSizePips` liegt. Wenn die jüngste Kerze strikt kleiner ist, ist das Setup gültig.
3. **Einstiegsplatzierung** – wenn das Setup gültig ist, werden zwei Stop-Orders vorbereitet:
   - Ein **Buy Stop** `10` Pips über dem vorherigen Hoch, wenn die Nettoposition nicht long ist.
   - Ein **Sell Stop** `10` Pips unter dem vorherigen Tief, wenn die Nettoposition nicht short ist.
   Bestehende ausstehende Orders desselben Typs werden vor dem Registrieren neuer storniert.
4. **Risikomanagement** – sobald eine Stop-Order ausgelöst wird und eine Position eröffnet, werden automatisch Schutzorders angehängt:
   - Take-Profit bei `TakeProfitPips` über/unter dem Einstandspreis.
   - Stop-Loss bei `StopLossPips` unter/über dem Einstandspreis.
   Schutzorders werden storniert, wenn die Position auf null zurückkehrt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `Volume` | Mit jeder Stop-Order gesendetes Ordervolumen. |
| `TakeProfitPips` | Abstand in Pips für die Take-Profit-Order nach dem Einstieg. |
| `StopLossPips` | Abstand in Pips für den Schutz-Stop nach dem Einstieg. |
| `MaxCandleSizePips` | Obergrenze für die historischen Kerzenspannen, die im Volatilitätsfilter berücksichtigt werden. |
| `CandleCount` | Anzahl der historischen Kerzen zur Suche nach der minimalen akzeptablen Spanne. |
| `CandleType` | Für die Analyse verwendeter Kerzen-Zeitrahmen. |

## Implementierungshinweise

- Die Pip-Größe wird vom Preisschritt des Instruments abgeleitet. Wenn der Schritt kleiner oder gleich `0.001` ist, wird er mit `10` multipliziert, um die MetaTrader-Pip-Definition für 3- oder 5-Dezimalinstrumente zu emulieren.
- Kerzenspannen werden in einer FIFO-Warteschlange mit maximal `CandleCount` Elementen gespeichert, was dem historischen Scanning des ursprünglichen Expert Advisors entspricht.
- Alle Orders werden über die High-Level-StockSharp-API erstellt (keine manuelle Order-Registrierung) und automatisch storniert, wenn sie veraltet sind oder die Position schließt.
- Kommentare im Code sind auf Englisch verfasst, während README-Dateien ausführliche mehrsprachige Dokumentation bereitstellen.
