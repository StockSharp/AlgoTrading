# Constituents EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den **Constituents EA** aus `MQL/22595` in die StockSharp High-Level-API. Sie recreiert die ursprüngliche
Logik, zwei ausstehende Orders um den jüngsten Bereich zu einer bestimmten Stunde zu platzieren, während der Workflow mit dem
Order-Handling und den Risikoschutzhelfern von StockSharp kompatibel bleibt.

## Wie die Strategie funktioniert

1. **Geplante Aktivierung** – am Ende jeder Kerze prüft die Strategie, ob der nächste Balken bei `StartHour` beginnt. Nur
   in diesem Moment werden neue ausstehende Orders in Betracht gezogen, was den MetaTrader-Code widerspiegelt, der auf die
   Geburt des Balkens reagierte, dessen Öffnungszeit mit der konfigurierten Stunde übereinstimmt.
2. **Bereichserkennung** – das höchste Hoch und das niedrigste Tief unter den vorherigen `SearchDepth` abgeschlossenen Kerzen
   werden mit `Highest`/`Lowest`-Indikatoren verfolgt. Diese beiden Preise definieren die Ausbruchs-/Mean-Reversion-Niveaus für
   die Order-Platzierung.
3. **Preisabstandsfilter** – aktuelle beste Bid/Ask-Kurse werden aus dem Order-Book-Feed gestreamt. Orders werden nur platziert,
   wenn der Abstand zwischen dem Kurs und dem Kandidatenpreis größer oder gleich `MinOrderDistancePips` ist (in absoluten Preis
   umgerechnet mit `PointValue`). Dies reimplementiert die ursprüngliche Freeze-Level-Validierung und verhindert ungültige
   ausstehende Orders.
4. **Order-Stil-Auswahl** – `PendingOrderMode` wählt zwischen Limit-Orders (Buy-Limit am Tief, Sell-Limit am Hoch) oder
   Stop-Orders (Buy-Stop über dem Hoch, Sell-Stop unter dem Tief). Beide Orders werden gleichzeitig eingereicht, genau wie im
   MetaTrader-Skript.
5. **Risikoabsicherung** – der eingebaute `StartProtection`-Helfer hängt Stop-Loss- und Take-Profit-Niveaus an, ausgedrückt in
   absoluten Preisschritten (`StopLossPips`/`TakeProfitPips`). Mindestabstandsprüfungen gegen `MinStopDistancePips` replizieren
   die MT5-Anforderung, dass Schutzorders den Symbol-Stop-Level respektieren müssen.
6. **Order-Verwaltung** – wenn eine ausstehende Order ausgeführt wird, wird die entgegengesetzte Order sofort storniert. Während
   des Balken-Intervalls platziert die Strategie keine zusätzlichen Orders, solange aktive existieren, was dem Quell-EA-Verhalten
   entspricht.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `StartHour` | Stunde (0-23), wenn das neue Paar ausstehender Orders erstellt wird. |
| `SearchDepth` | Anzahl vorheriger abgeschlossener Kerzen zur Berechnung des Hoch/Tief-Bereichs. |
| `PendingOrderMode` | `Limit` repliziert die Mean-Reversion-Variante, `Stop` platziert Ausbruchs-Orders. |
| `StopLossPips` | Stop-Loss-Abstand in Pips (umgerechnet mit `PointValue`). Auf 0 setzen zum Deaktivieren. |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. Auf 0 setzen zum Deaktivieren. |
| `PointValue` | Pip-Wert in Preiseinheiten. Auf 0 setzen zur Auto-Erkennung aus `Security.PriceStep`/`MinStep`. |
| `MinOrderDistancePips` | Mindestabstand zwischen aktuellem Bid/Ask und dem ausstehenden Preis, modelliert Freeze-Level-Prüfungen. |
| `MinStopDistancePips` | Mindestabstand für Stop/Take, spiegelt `StopsLevel`-Prüfungen wider. |
| `CandleType` | Zeitrahmen für Bereichsberechnung und Planungslogik. |

`Strategy.Volume` steuert die Ordergröße; positiv halten, damit `BuyLimit`, `SellLimit`, `BuyStop` und `SellStop` Orders
einreichen können.

## Verwendung

1. Die Strategie an ein Wertpapier anhängen und `CandleType` auf den gewünschten Zeitrahmen setzen.
2. `StartHour` und `SearchDepth` genauso wie in den MT5-Eingaben konfigurieren. Die `Min*Pips`-Schwellenwerte anpassen, wenn
   der Broker Mindestabstände zwischen Orders und dem Marktpreis erzwingt.
3. `PointValue` kalibrieren, wenn die Auto-Erkennung aus den Sicherheitsmetadaten nicht möglich ist (zum Beispiel bei
   synthetischen Instrumenten).
4. `StopLossPips` und `TakeProfitPips` so setzen, dass sie dem ursprünglichen EA entsprechen. Das Schutzmodul hängt
   automatisch Stops und Ziele an, sobald eine Order ausgeführt wird.
5. Ein positives `Volume` angeben und die Strategie starten. Sie abonniert Kerzen- und Order-Book-Daten, platziert beide
   ausstehenden Orders beim geplanten Balken und storniert die entgegengesetzte Order, wenn ein Trade ausgeführt wird.

## Unterschiede gegenüber dem ursprünglichen EA

- Der MetaTrader-`MoneyFixedMargin`-Risiko-Modus (prozentbasierte Dimensionierung) ist nicht portiert. StockSharp-Benutzer
  sollten `Strategy.Volume` direkt konfigurieren oder die Strategie mit einem externen Positionsgrößen-Modul umhüllen.
- Freeze-Level- und Stop-Level-Prüfungen werden durch die konfigurierbaren Parameter `MinOrderDistancePips` und
  `MinStopDistancePips` ausgedrückt, da die entsprechenden Exchange-Metadaten nicht immer über StockSharp verfügbar sind.
- Die Order-Platzierung erfolgt, wenn die vorherige Kerze schließt und der kommende Balken bei `StartHour` beginnt. Dies ist
  funktional identisch mit der MT5-Implementierung, die auf die Geburt des neuen Balkens reagierte.
- Alle Kommentare innerhalb des Quellcodes wurden ins Englische übersetzt, während die externe Dokumentation aus Bequemlichkeit
  in mehreren Sprachen verfügbar ist.

Die Abstände und die Handelszeit an das geplante Instrument anpassen. Auf Märkten mit breiten Spreads sind möglicherweise
größere `MinOrderDistancePips`- oder Pip-Werte erforderlich, um sofortige Ablehnung durch den Broker zu vermeiden.
