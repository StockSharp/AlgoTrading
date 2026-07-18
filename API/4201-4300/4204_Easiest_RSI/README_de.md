# Einfachste RSI-Strategie (ID 4204)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert vom MetaTrader 4 Expert Advisor **„Easiest RSI“** mit Sitz in `MQL/9827/Easiest_RSI.mq4`.

## Überblick

Der ursprüngliche EA eröffnet Geschäfte, wenn der Relative-Stärke-Index (RSI) die überverkauften/überkauften Zonen überschreitet, und fügt optional bis zu zwei zusätzliche Positionen in die gleiche Richtung hinzu, während sich der Preis weiterhin günstig bewegt. Jede Order verwendet das gleiche Volumen, einen festen Stop-Loss und einen Trailing-Stop, der in kleinen Schritten erhöht wird, sobald der Handel hohe Gewinne erzielt.

Dieser StockSharp-Port hält das Verhalten auf Strategieebene:

- RSI(14), berechnet auf der konfigurierten Kerzenserie, steuert die Signale.
- Long-Trades werden ausgelöst, wenn RSI den überverkauften Schwellenwert nach oben überschreitet; Shorts treten bei Abwärtsüberschreitungen der überkauften Schwelle auf.
- Die Positionsskalierung ahmt die MT4-Durchschnittslogik nach, indem sie jedes Mal, wenn der Preis um `StepPips` steigt, eine neue Order hinzufügt, begrenzt durch `MaxEntries`.
- Initial Stops und Trailing Stops werden intern verwaltet, wobei die Preisabstände in Pips gemessen werden (automatisch angepasst für 4/5-stellige FX-Kurse).
- Der gesamte Status (RSI-Verlauf, letzte Einstiegspreise, Trailing Stops) wird in primitiven Feldern gespeichert, um den Framework-Richtlinien zu folgen.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `LotSize` | `1` | Volumen jeder Marktorder. |
| `StopLossPips` | `50` | Anfänglicher Schutzstopp in Pips (zum Deaktivieren auf Null setzen). |
| `TrailingStopPips` | `50` | Trailing-Stop-Distanz in Pips; Null deaktiviert das Nachziehen. |
| `StepPips` | `20` | Minimaler günstiger Zug, bevor eine zusätzliche Position hinzugefügt wird. |
| `RsiPeriod` | `14` | RSI Länge. |
| `OversoldLevel` | `30` | RSI-Level, das nach oben überschritten werden muss, um lange Einträge auszulösen. |
| `OverboughtLevel` | `70` | RSI-Level, das nach unten überschritten werden muss, um Short-Einträge auszulösen. |
| `MaxEntries` | `3` | Maximale Anzahl aufeinanderfolgender Einträge pro Richtung (entsprechend dem MT4-Limit). |
| `CandleType` | `TimeFrame(5m)` | Kerzentyp/Zeitrahmen, der zur Berechnung von RSI verwendet wird. |

Alle in Pips ausgedrückten Entfernungen werden mithilfe des Instrumentenwerts `Step` in absolute Preise umgerechnet. Bei 5-stelligen FX-Symbolen verdoppelt der Helfer den Schritt, sodass Eingaben wie `50` 5,0 Pips entsprechen, was die ursprüngliche EA-Anleitung widerspiegelt.

## Handelslogik

1. **Signalerkennung** – Die Strategie überwacht nur fertige Kerzen. Es speichert die letzten beiden RSI-Messwerte, um die MT4-Aufrufe `iRSI(..., 1)` und `iRSI(..., 2)` zu replizieren. Durchläuft das Feuer von `OversoldLevel` oder `OverboughtLevel`, sobald die neue Kerze schließt.
2. **Primäre Einträge** – Wenn ein flaches und ein bullisches Kreuz auftritt, wird eine Kaufmarktorder gesendet; Abwärtstrends, wenn sie flach sind, lösen einen Verkaufsauftrag aus.
3. **Skalierung** – Während eine Position offen ist, vergleicht die Strategie den letzten Schluss-/Hochkurs (Long) oder Schluss-/Tiefstkurs (Short) mit dem Preis der letzten Ausführung. Jedes Mal, wenn sich der Preis um mindestens `StepPips` zu seinen Gunsten bewegt, wird eine neue Order mit der Größe `LotSize` aufgegeben, bis zu insgesamt `MaxEntries` Positionen in dieser Richtung.
4. **Stop-Loss** – Bei jeder Füllung wird ein anfänglicher Stop als Positionspreis minus/plus `StopLossPips` neu berechnet. Der aggregierte Stopp hält den weitesten (konservativsten) Abstand ein, sodass die gesamte Position geschützt bleibt.
5. **Trailing** – Nachdem der Handel voranschreitet, wird der Stop anhand des Kerzenhochs (Longs) oder Tiefs (Shorts) näher vorgeschoben. Ein kleiner Puffer, der fünf Mindestpreisschritten entspricht, emuliert die MT4-Anforderung `OrderStopLoss() + 5*Point`, bevor der Stop verschoben wird.
6. **Exit** – Wenn der Preis das verwaltete Stop-Level erreicht, wird die Position zum Marktwert geschlossen. Über den Trailing Stop hinaus wird kein Gewinnziel verwendet.

## Implementierungshinweise

- Aufträge werden über die High-Level-Pipeline `SubscribeCandles().Bind(...)` und Market-Order-Helper (`BuyMarket` / `SellMarket`) gesendet.
- Die Strategie behält `_longOrderPending` / `_shortOrderPending` und Exit-Flags bei, um zu vermeiden, dass die Börse mit doppelten Anfragen überschwemmt wird, während eine Marktorder auf Bestätigung wartet.
- `StartProtection` wird nicht aufgerufen, da die gesamte Schutzlogik explizit so codiert ist, dass sie dem MT4-Verhalten entspricht.
- Da StockSharp mit Nettopositionen arbeitet, wird der Trailing Stop auf das Gesamtengagement angewendet. Das heißt, wenn mehrere Einträge offen sind, steigen alle Lose gemeinsam aus, sobald der kombinierte Stop berührt wird. Das ursprüngliche EA hat den Stop jeder Order einzeln verschoben; Der aggregierte Ansatz behält die Risikokontrolle bei, kann den Korb jedoch etwas früher schließen. Der Unterschied wird zur Transparenz dokumentiert.

## Nutzungstipps

1. Weisen Sie das gewünschte Wertpapier und den gewünschten Connector zu und stellen Sie dann `CandleType` so ein, dass es mit dem Zeitrahmen übereinstimmt, mit dem Sie handeln möchten (z. B. 5-Minuten-EURUSD-Kerzen wie in den Quellkommentaren).
2. Passen Sie die Pip-basierten Parameter entsprechend der Volatilität des Instruments an. Denken Sie daran, die Standardwerte mit 10 zu multiplizieren, wenn Sie es vorziehen, in Rohpunkten für 5-stellige Kurse zu arbeiten, was der MT4-Anleitung entspricht.
3. Optional: Passen Sie `MaxEntries` und `StepPips` an, um zu steuern, wie aggressiv die Strategie im Durchschnitt zu erfolgreichen Trades führt.
4. Führen Sie die Strategie zunächst im Papierhandel aus, um Pip-Konvertierungen und das nachlaufende Verhalten der Symbole Ihres Brokers zu überprüfen.

## Dateien

- `CS/EasiestRsiStrategy.cs` – Strategieumsetzung.
- `README.md` – Dieses Dokument.
- `README_zh.md` – Chinesische Übersetzung.
- `README_ru.md` – Russische Übersetzung.

Auf die Python-Implementierung wird wie gewünscht bewusst verzichtet.
