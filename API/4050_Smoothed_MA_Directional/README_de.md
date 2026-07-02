# Geglättete MA-Richtungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-High-Level-API-Port des MetaTrader 4-Experten `oc08_vy_m0moqesu15` aus dem Ordner `MQL/8615`. Der ursprüngliche Experte richtet seine Position an einem einzelnen geglätteten gleitenden Durchschnitt (SMMA) aus und fügt jeder Order feste Stop-Loss- und Take-Profit-Level hinzu. Die C#-Version behält das gleiche Richtungsverhalten bei, übernimmt aber idiomatische StockSharp-Komponenten.

## Handelsidee

- **Richtungsfehler:** Der Kursschluss über dem geglätteten gleitenden Durchschnitt weist auf einen Aufwärtstrend hin; Ein Schlusskurs darunter signalisiert einen Abwärtstrend.
- **Positionsausrichtung:** Die Strategie versucht immer, eine einzelne Position in Richtung des erkannten Trends beizubehalten. Wenn der Markt die Seiten wechselt, kehrt er die Position sofort um.
- **Risikokontrolle:** Jeder Einstieg ist durch Stop-Loss- und Take-Profit-Offsets geschützt, die in Preisschritten ausgedrückt werden. Der `StartProtection`-Helfer von StockSharp ersetzt die manuelle SL/TP-Zuweisung im ursprünglichen MQ4-Code.
- **Ausführungsstil:** Aufträge werden als Marktaufträge bei Kerzenschluss übermittelt und reproduzieren die `OrdersTotal()==0`-Logik des MetaTrader-Experten.

## Wie es funktioniert

1. Beim Start abonniert die Strategie Kerzen des konfigurierten Zeitrahmens und verknüpft einen `SmoothedMovingAverage`-Indikator mit dem ausgewählten Zeitraum.
2. Wenn eine Kerze endet, wird der Indikatorwert mit dem Schlusskurs der Kerze verglichen.
3. Wenn der Schlusskurs über dem SMMA liegt und die Strategie flach oder short ist, sendet sie einen Marktkauf in der Größe, um das Short-Engagement (falls vorhanden) abzudecken und eine Long-Position zu eröffnen.
4. Wenn der Schlusskurs unter dem SMMA liegt und die Strategie flach oder long ist, sendet sie einen Marktverkauf in der Größe, um das Long-Engagement (falls vorhanden) abzudecken und eine Short-Position zu eröffnen.
5. Schützende Stop-Loss- und Take-Profit-Abstände werden einmalig zu Beginn unter Verwendung des aktuellen Wertpapiers `PriceStep` konfiguriert. Wenn beide Offsets auf Null gesetzt sind, ist der Schutz deaktiviert.
6. Die Diagrammausgabe (Kerzen, Indikatoren, Trades) wird automatisch gezeichnet, wenn die Strategie in Umgebungen ausgeführt wird, die einen Diagrammbereich freigeben.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `StopLossPoints` | 100 | Stop-Loss-Distanz in Preisschritten. Auf `0` setzen, um den Stopp zu deaktivieren.
| `TakeProfitPoints` | 100 | Take-Profit-Distanz in Preisschritten. Auf `0` setzen, um das Ziel zu deaktivieren.
| `MaPeriod` | 12 | Zeitraum des geglätteten gleitenden Durchschnitts, der zur Messung des Trends verwendet wird.
| `TradeVolume` | 1 | Marktauftragsvolumen. Die Strategie schreibt diesen Wert beim Start auch in `Strategy.Volume`.
| `CandleType` | 15-minütiger Zeitrahmen | Kerzentyp (Zeitrahmen), der den Indikator und die Signale steuert.

Alle Parameter sind über StockSharp Designer/Runner konfigurierbar und umfassen Optimierungsbereiche für automatisierte Tests.

## Unterschiede zur MetaTrader-Version

- Die margenbasierte Losgröße (`Lots`/`Prots`) wird durch einen festen `TradeVolume`-Parameter ersetzt. Dadurch bleibt das Verhalten deterministisch und kompatibel mit der Portfolio-Abstraktion von StockSharp.
- Stop-Loss und Take-Profit werden von `StartProtection` anstelle von manuellen Auftragsänderungen gehandhabt, wobei die ursprünglichen Offsets abgeglichen werden, aber StockSharp-Grundelemente verwendet werden.
- Die Strategie ignoriert unvollendete Kerzen, um vorzeitige Trades zu vermeiden, und spiegelt die `New_Bar`-Flagge in MQ4 wider.

## Praktische Hinweise

- Stellen Sie sicher, dass die verbundene Sicherheit einen gültigen `PriceStep` bereitstellt. Wenn nicht, greift die Strategie bei der Berechnung von SL/TP-Abständen auf einen Einheitsschritt von `1` zurück.
- Die Länge des Indikators wird mit dem aktuellen Parameterwert jeder Kerze synchronisiert, sodass Live-Parameteranpassungen möglich sind.
- Um das ursprüngliche Verhalten zu reproduzieren, konfigurieren Sie denselben Zeitrahmen wie das Diagramm, in dem sich der MQ4-Experte befand, und halten Sie das Handelsvolumen im Einklang mit Ihrer gewünschten Kontraktgröße.
