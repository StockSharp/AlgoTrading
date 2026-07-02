# Händlerhandel v7.51 RIVOT (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung

Dealers Trade v7.51 ist eine Rasterstrategie im Martingal-Stil, die ursprünglich als MetaTrader 4 Expert Advisor `Dealers_Trade_v_7.51_RIVOT.mq4` bereitgestellt wurde. Der Port hält die ursprüngliche Idee des Handels fern von einer Pivot-basierten Richtungsausrichtung und skaliert in die dominante Seite, wenn der Preis um eine konfigurierbare Pip-Distanz zurückgeht. Die StockSharp-Implementierung verwendet hochrangige Strategiehelfer, um Kerzen zu abonnieren, die Pivot-Zonen zu berechnen und Positionsgröße, Risiko und Ausstiege zu verwalten.

## Handelslogik

1. **Pivot-Framework**
   - Die Strategie erstellt für jede fertige Kerze zwei Referenzpreise:
     - **Klassischer Pivot** (`P`) = `(previous high + previous low + previous close + current open) / 4`.
     - **Floating Pivot** (`FLP`) = `(current high + current low + current close) / 3`.
   - Eine Pips-Lücke zwischen `P` und `FLP` muss größer oder gleich `GapThreshold` sein, um den Handel für den aktuellen Balken zu ermöglichen.

2. **Richtungsfehler**
   - Wenn der Kerzenschluss über beiden Pivots liegt und der Gap-Filter erfüllt ist, wechselt die Tendenz zu **long**.
   - Wenn der Schlusskurs der Kerze unter beiden Pivotpunkten liegt und die Lücke bestätigt ist, wechselt die Tendenz zu **Short**.
   - Die Vorspannung bleibt bestehen, bis die Positionsreihe vollständig geschlossen ist oder nach Ende der Reihe der gegenteilige Zustand eintritt.

3. **Skalierungseinträge**
   - Es kann jeweils nur eine Handelsreihe aktiv sein.
   - Der erste Eintrag folgt sofort der Voreingenommenheit.
   - Zusätzliche Einträge werden nur eröffnet, wenn der Preis gegenüber der aktiven Tendenz um mindestens `PipDistance` Pips seit der letzten Füllung zurückgeht, was der ursprünglichen Martingal-Durchschnittsbildung nachempfunden ist.
   - Jede neue Bestellung multipliziert die vorherige Größe mit `VolumeMultiplier`, überschreitet jedoch niemals `MaxVolume`.
   - Die Anzahl der gestapelten Einträge ist auf `MaxTrades` begrenzt.

4. **Risikokontrollen**
   - Ein harter Stop-Loss bei `StopLoss` Pips vom volumengewichteten Durchschnittseintritt schließt die gesamte Serie ab.
   - Ein fester Take-Profit bei `TakeProfit` Pips sichert Gewinne, sobald der Preis wieder positiv wird.
   - Wenn der Trailing-Stop aktiviert ist, sperrt er Gewinne dynamisch, indem er jedes Mal näher an den Preis heranrückt, wenn er sich um mehr als `TrailingStop` Pips über den durchschnittlichen Einstieg hinaus bewegt.

5. **Bedingungen zurücksetzen**
   - Jeder vollständige Ausstieg (Stop-Loss, Take-Profit, Trailing-Stop oder manuelles Abflachen der Position) setzt die Martingal-Zähler zurück und entfernt die Richtungsabweichung.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `Volume` | 1 | Basisauftragsgröße für den ersten Eintrag. |
| `MaxTrades` | 5 | Maximale Anzahl gemittelter Einträge pro Serie. |
| `PipDistance` | 4 | Vor dem Hinzufügen einer neuen Position ist eine minimale Gegenbewegung (in Pips) erforderlich. |
| `TakeProfit` | 15 | Abstand vom volumengewichteten Durchschnittseintrag bis zum Abschluss des gesamten Rasters im Gewinn. |
| `StopLoss` | 90 | Abstand vom durchschnittlichen Eingang, der einen Schutzausgang auslöst. |
| `TrailingStop` | 15 | Der Trailing-Stop-Offset wird angewendet, sobald sich der Preis positiv entwickelt. auf Null setzen, um das Nachziehen zu deaktivieren. |
| `VolumeMultiplier` | 1.5 | Faktor, der verwendet wird, um die Auftragsgröße für jeden weiteren Eintrag zu erhöhen. |
| `MaxVolume` | 5 | Obergrenze für das Einzelauftragsvolumen nach Anwendung des Multiplikators. |
| `GapThreshold` | 7 | Mindestlücke (in Pips) zwischen dem klassischen und dem Floating-Pivot, die erforderlich ist, um die Voreingenommenheit zu aktivieren. |
| `CandleType` | 15-minütige Zeitrahmenkerzen | Kerzentyp, der für Berechnungen und Entscheidungsfindung verwendet wird. |

Alle Parameter werden über `StrategyParam<T>` konfiguriert, sodass sie im StockSharp Designer oder Strategy Runner optimiert werden können.

## Nutzungshinweise

- Die Strategie basiert ausschließlich auf Kerzendaten; Es ist kein direkter Bid/Ask-Stream auf Tick-Ebene erforderlich. Stellen Sie sicher, dass Ihr Datenanbieter die ausgewählten `CandleType` liefern kann.
- Da StockSharp Positionen standardmäßig aggregiert, verwaltet die Implementierung einen internen volumengewichteten Durchschnitt, um das MT4-Rasterbuch zu emulieren. Kommt es zu Teilfüllungen, sorgt die integrierte Positionsabrechnung dafür, dass die Werte konsistent bleiben.
- Beim Rendern von Diagrammen werden dem Diagrammbereich zwei horizontale Linien (`Pivot` und `FloatingPivot`) hinzugefügt, sofern diese verfügbar sind.
- Es gibt keinen automatischen Reverse-Trading; Das System wartet auf das Ende der laufenden Serie, bevor es einen Bias-Flip akzeptiert.

## Unterschiede zur MQL-Version

- Das ursprüngliche Skript zeichnete mehrere Beschriftungen und Kommentare zum MT4-Diagramm. Der Port behält nur die funktionale Handelslogik bei und ersetzt die visuellen Elemente durch StockSharp Diagrammlinien.
- Kontoschutzfunktionen basierend auf der Gesamtzahl der offenen Bestellungen, manuelle Filterung magischer Zahlen und symbolspezifische Pip-Werttabellen sind in StockSharp nicht erforderlich und wurden weggelassen.
- Der Auftragsabschluss zu genauen Tick-Preisen (`Ask == tp`) im MetaTrader-Code wird durch Preisvergleiche bei Candle-Closings angenähert.
- Das Handelsmanagement wird mit Marktaufträgen (`BuyMarket`/`SellMarket`) anstelle von MT4-Ticketschleifen implementiert. Trailing Stops und Exits erfolgen bei Kerzenaktualisierungen.

## Best Practices

- Testen Sie die Strategie immer im Papierhandel oder in historischen Simulationen mit realistischen Spread-/Provisionsmodellen, bevor Sie live gehen.
- Erwägen Sie, `VolumeMultiplier` oder `MaxTrades` bei hochvolatilen Instrumenten zu senken, um den Drawdown zu kontrollieren.
- Passen Sie für Intraday-Produkte `CandleType` an, um der Datengranularität der ursprünglichen Einrichtung zu entsprechen (der Standardwert ist 15 Minuten, aber EA wurde häufig in M15 und H1 verwendet).

## Dateien

- `CS/DealersTradeV751RivotStrategy.cs` – Haupt-C#-Implementierung.
- `README_zh.md` – Chinesische Dokumentation.
- `README_ru.md` – Russische Dokumentation.
