# MultiMartinStrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

`MultiMartinStrategy` ist die StockSharp-Umsetzung des MQL5-Expertenberaters **MultiMartin**. Der ursprüngliche Roboter ist ein Mehrwährungs-Martingal, das bei Umkehrsignalen zwischen Long- und Short-Trades wechselt und die Ordergröße nach verlorenen Geschäften erhöht. Dieser Port behält die Kernlogik des Geldmanagements bei und verwendet gleichzeitig das übergeordnete API von StockSharp für die Auftragsweiterleitung, Positionsüberwachung, optionale Trailing Stops und die Handhabung von Broker-Ablehnungen.

Die Strategie eröffnet kontinuierlich eine einzelne Marktposition auf dem konfigurierten Instrument. Nach jedem Ausstieg behält es entweder die Richtung bei (wenn der Handel profitabel war) oder kehrt die Richtung um (wenn der Handel Geld verloren hat). Verlierende Trades lösen einen Martingalschritt aus, der das nächste Ordervolumen vervielfacht, bis eine konfigurierbare Obergrenze erreicht ist.

## Handelslogik

1. **Eintragsauswahl**
   - Die Strategie verwendet einen Zeitfilter, um den Handel auf ein Intraday-Fenster zu beschränken. Außerhalb dieses Fensters werden keine neuen Einträge übermittelt.
   - Wenn keine Position offen ist und sich der Broker nicht im Cooldown-Zustand befindet, sendet die Strategie eine Marktorder in die aktuelle Richtung. Die erste Richtung ist benutzerdefiniert (Kauf oder Verkauf).
2. **Martingale Größe**
   - Nach jedem Verlust wird das nächste Auftragsvolumen mit dem Parameter `Factor` multipliziert.
   - Die Multiplikation wird durch `Limit` begrenzt, was die maximale Anzahl aufeinanderfolgender Verdoppelungen definiert. Sobald die Obergrenze überschritten wird, wird das Volumen auf den Basiswert `Volume` zurückgesetzt.
   - Bei profitablen Trades wird das Volumen immer auf die Basisgröße zurückgesetzt und die Handelsrichtung beibehalten.
3. **Exit-Management**
   - Stop-Loss- und Take-Profit-Distanzen werden in Preispunkten ausgedrückt und mit dem Instrument `PriceStep` in absolute Distanzen umgerechnet.
   - Optionale Trailing-Modi verschieben den Stop-Loss auf die Gewinnschwelle oder verfolgen ihn linear hinter dem Preis.
   - Ausstiege werden durch Marktaufträge abgewickelt, sobald die Candle-Extreme entweder die Stop- oder Take-Schwelle überschreiten.
4. **Abwicklung von Makler-Ablehnungen**
   - Wenn eine Bestellung abgelehnt wird, tritt für die Strategie eine Abklingzeit ein, die von `SkipBadTime` gesteuert wird. Während der Abklingzeit werden keine neuen Einträge versucht. Die Option `Forever` deaktiviert den Handel für den Rest der Sitzung.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `UseTimeFilter` | Aktivieren oder deaktivieren Sie das Intraday-Handelsfenster. |
| `HourStart` | Inklusive Stunde (0-23), wann der Handel aktiv wird. |
| `HourEnd` | Exklusive Stunde (1-24), wenn der Handel stoppt. Unterstützt Nachtfenster (z. B. 22-2). |
| `Volume` | Basisauftragsvolumen in Losen oder Verträgen. |
| `Factor` | Der Multiplikator wird auf das nächste Auftragsvolumen nach einem Verlustgeschäft angewendet. |
| `Limit` | Maximale Anzahl aufeinanderfolgender Multiplikationen, bevor die Lautstärke zurückgesetzt wird. |
| `StopLossPoints` | Stop-Loss-Distanz, ausgedrückt in Instrumentenpunkten. Auf 0 setzen, um den Stopp zu deaktivieren. |
| `TakeProfitPoints` | Take-Profit-Distanz, ausgedrückt in Instrumentenpunkten. Auf 0 setzen, um das Ziel zu deaktivieren. |
| `StartDirection` | Erste Handelsrichtung (`Buy` oder `Sell`). |
| `SkipBadTime` | Nach einer abgelehnten Marktorder wird eine Abklingzeit angewendet. `Forever` blockiert weitere Einträge. |
| `TrailMode` | Trailing-Modus: `None`, `Breakeven` oder `Straight` (lineares Trailing). |
| `CandleType` | Kerzenserie zur Verwaltung von Exits und Zeitfilterung. |

## Unterschiede zur MQL5-Version

- Der Port StockSharp handelt ein einzelnes Wertpapier pro Strategieinstanz. Starten Sie mehrere Instanzen, um mehrere Symbole abzudecken.
- Das Stop-Loss- und Take-Profit-Management erfolgt kerzenbasiert; Füllungen werden mit Marktaufträgen ausgeführt, sobald die Kerzenspanne die Schwellenwerte berührt.
- Bei Broker-Ablehnungen wird der `OnOrderFailed`-Rückruf von StockSharp verwendet, um die Abklingzeit von `SkipBadTime` anstelle des globalen Timers von MQL5 auszulösen.
- Trailing-Stop-Optionen wurden mithilfe der Logik auf Strategieebene anstelle direkter Orderänderungsaufrufe neu implementiert.

## Nutzungshinweise

- Konfigurieren Sie `Security` und `Portfolio`, bevor Sie mit der Strategie beginnen.
- Stellen Sie sicher, dass `Volume` mit den Losgrößen- und Teilvolumenregeln des Instruments kompatibel ist.
- Setzen Sie `StopLossPoints`/`TakeProfitPoints` auf Null, um die entsprechenden Schutzanordnungen zu deaktivieren.
- Wählen Sie beim Backtesting einen Kerzentyp, der dem historischen Datensatz entspricht (z. B. 1-Minuten-Kerzen für Forex-Paare).
- Um das ursprüngliche Multi-Symbol-Verhalten zu simulieren, stellen Sie mehrere Strategieinstanzen mit unterschiedlichen Wertpapieren und Parametern bereit.

## Risikowarnungen

Martingale Geldmanagement ist von Natur aus riskant. Verluststrähnen können das Engagement exponentiell steigern und die verfügbare Marge schnell aufzehren. Verwenden Sie konservative Volumeneinstellungen, testen Sie historische Daten und wenden Sie strenge Risikokontrollen an, bevor Sie die Strategie in der Produktion anwenden.
