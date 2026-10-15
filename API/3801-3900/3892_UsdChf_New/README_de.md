# USD/CHF CCI Channel-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **USD/CHF CCI Channel-Stop-Strategie** ist eine StockSharp High-Level-Implementierung des MetaTrader 4-Expertenberaters `UsdChf_new`. Die Strategie wartet auf Ausbrüche des Commodity Channel Index (CCI) im H4-Zeitrahmen und setzt ausstehende Stop-Orders über oder unter dem aktuellen Preis ein. Sobald eine Order ausgeführt wurde, wird die Position durch dieselben Pip-basierten Geldverwaltungsregeln geschützt, die im ursprünglichen Roboter verwendet wurden: ein fester Stop-Loss, optionale Stornierung veralteter ausstehender Orders, Break-Even-Verschiebung und Trailing-Stop-Management.

Diese Konvertierung behält den ursprünglichen Ausführungsablauf bei, umfasst aber den idiomatischen StockSharp-Workflow: Kerzenabonnements, Indikatorbindungen und hochrangige Order-Helfer (`BuyStop`, `SellStop`, `BuyMarket`, `SellMarket`). Alle Risikoabstände sind weiterhin in Pips konfiguriert, um Forex-Benutzern vertraut zu bleiben.

## Handelslogik

1. **Anzeigen und Signale**
   - Berechnen Sie einen CCI mit der konfigurierten Periode für fertige H4-Kerzen.
   - Überwachen Sie die Kanalgrenzen: `+CCI Channel` und `-CCI Channel`.
   - Erkennen Sie Überschneidungen des aktuellen Werts mit dem vorherigen Wert, um Signale zu generieren.
     - Der Übergang **nach oben** durch `-CCI Channel` bereitet einen **Kaufstopp** über dem Preis vor.
     - Der Übergang **nach unten** durch `+CCI Channel` bereitet einen **Verkaufsstopp** unter dem Preis vor.
2. **Ausstehende Bestellungen**
   - Stop-Orders werden vom Kerzenschluss um `Entry Indent (pips)` abgesetzt und auf den Instrumentenschritt gerundet.
   - Es kann jeweils nur eine ausstehende Bestellung aktiv sein. Durch das Erstellen einer neuen Seite wird die Gegenseite aufgehoben.
   - Wenn sich der Markt um mehr als `Cancel Distance (pips)` davon entfernt, wird die ausstehende Order storniert, um eine Jagd nach dem Preis zu vermeiden.
3. **Positionsmanagement**
   - Gefüllte Positionen erben die ursprüngliche Stop-Loss-Distanz.
   - Wenn der Trade mindestens `Break Even (pips)` gewinnt, verschiebt sich der Schutzstopp auf den Einstiegspreis.
   - Nachdem der Gewinn `Trailing Stop (pips)` überschreitet, folgt der Stop dem Preis und behält dabei die konfigurierte Lücke bei.
   - Gegenüberliegende CCI-Crossovers erzwingen einen Positionsausstieg und platzieren eine neue Stop-Order in die neue Richtung.

## Parameter

| Parameter | Beschreibung | Standard | Optimierbar |
|-----------|-------------|---------|-------------|
| `CandleType` | Für CCI-Berechnungen verwendete Kerzenserie (Standard H4). | Zeitrahmen von 4 Stunden | Nein |
| `CciPeriod` | CCI Mittelungszeitraum. | 73 | Ja |
| `CciChannel` | Absoluter CCI-Pegel, der die Kanalgrenzen bildet. | 120 | Ja |
| `EntryIndentPips` | Abstand (in Pips) zwischen Marktpreis und ausstehender Stop-Order. | 30 | Ja |
| `StopLossPips` | Anfängliche Stop-Loss-Distanz in Pips. | 95 | Ja |
| `CancelDistancePips` | Maximale Lücke vor der Stornierung ausstehender Bestellungen. | 30 | Ja |
| `TrailingStopPips` | Trailing-Stop-Distanz nach Aktivierung. | 110 | Ja |
| `BreakEvenPips` | Erforderlicher Gewinn, bevor der Stop auf das Einstiegsniveau verschoben wird. | 60 | Ja |

Alle Pip-Abstände werden mithilfe der Instrumente `PriceStep` und `Decimals` in Preis-Offsets umgewandelt. Bei 3/5-stelligen Forex-Symbolen entspricht der Pip zehn Preisschritten, andernfalls entspricht er einem einzelnen Schritt.

## Nutzungshinweise

1. Verknüpfen Sie die Strategie mit einem USD/CHF-Wertpapier (oder einem anderen Instrument, bei dem ein Pip-basiertes Risikomanagement relevant ist).
2. Legen Sie das gewünschte Handelsvolumen über die Basiseigenschaft `Strategy.Volume` fest.
3. Optimieren Sie optional die Pip-basierten Parameter, um sie an die Vertragsspezifikationen des Brokers anzupassen.
4. Führen Sie Backtests im Designer/Tester durch, um das Verhalten zu validieren, bevor Sie live gehen.

## Konvertierungshinweise

- Der MetaTrader-Experte durchlief die Rohauftragspools. In StockSharp speichert die Strategie Verweise auf die aktiven ausstehenden Bestellungen und verwendet stattdessen Stornierungshelfer auf hoher Ebene.
- Stop-Loss, Break-Even und Trailing werden über explizite Marktaustritte umgesetzt, da die Änderung von Orders auf Brokerseite nicht Teil des High-Level-API ist.
- Alle Inline-Kommentare wurden aus Gründen der Übersichtlichkeit ins Englische übersetzt und erweitert.
