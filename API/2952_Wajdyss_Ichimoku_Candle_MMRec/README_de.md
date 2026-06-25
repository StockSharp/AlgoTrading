# Wajdyss Ichimoku Candle MMRec-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein direkter Port des MetaTrader-Experten *Exp_wajdyss_Ichimoku_Candle_MMRec*. Sie rekonstruiert den "wajdyss Ichimoku candle"-Indikator,
indem sie die Ichimoku-Basislinie (Kijun) berechnet und jede abgeschlossene Kerze in einen von vier Farbzuständen einordnet. Das System sucht dann nach einer
Umkehrung in diesen Farben, um die jüngste Bewegung auszublenden. Wenn der vorherige Balken über dem Kijun lag und der letzte Signalbalken darunter fällt,
schließt der Algorithmus jedes Short-Exposure und öffnet einen Long-Trade. Der umgekehrte Übergang wechselt in eine Short-Position. Ein adaptives Geldmanagement-Modul
repliziert die ursprüngliche MMRec-Logik, indem es die Positionsgröße nach einer konfigurierbaren Anzahl von Verlust-Trades in derselben Richtung reduziert.

Die konvertierte Version verwendet die StockSharp High-Level-API. Kerzen werden über einen einzigen `SubscribeCandles`-Aufruf geliefert, und der Kijun-Level
wird mit `Highest`/`Lowest`-Indikatoren berechnet. Handelsentscheidungen werden nur bei abgeschlossenen Kerzen ausgewertet, um das Verhalten in Echtzeit- und
historischen Modi deterministisch zu halten.

## Kerzen-Einfärbe-Logik
Jede geschlossene Kerze erhält einen numerischen Farbindex, der dem ursprünglichen MQL5-Indikator entspricht:

| Farbe | Bedingung | Bedeutung |
|-------|-----------|-----------|
| `0` | Schluss unter Kijun und bearisher Körper | Starke bearishe Stimmung unter der Basislinie |
| `1` | Schluss unter Kijun aber bullisher Körper | Schwache bullishe Reaktion unterhalb der Basislinie |
| `2` | Schluss über Kijun aber bearisher Körper | Schwache bearishe Reaktion oberhalb der Basislinie |
| `3` | Schluss über Kijun und bullisher Körper | Starke bullishe Fortsetzung oberhalb der Basislinie |

## Signal-Logik
Signale werden bei abgeschlossenen Kerzen durch Vergleich der Farbe zweier historischer Balken generiert:

- **Long-Setup**: Der Balken bei `SignalBarShift + 1` hatte eine Farbe größer als `1` (Preis über Kijun) und der Balken bei `SignalBarShift` hat eine Farbe unter `2`
  (Preis bewegte sich unter Kijun). Die Strategie schließt optional eine offene Short-Position und kann ein neues Long eröffnen.
- **Short-Setup**: Der Balken bei `SignalBarShift + 1` hatte eine Farbe unter `2` (Preis unter Kijun), während der Balken bei `SignalBarShift` eine Farbe über `1`
  druckt (Preis bewegte sich über Kijun). Die Strategie schließt optional bestehende Longs und kann eine Short-Position eingehen.

Der Parameter `SignalBarShift` entspricht dem `SignalBar`-Eingang der MetaTrader-Version. Der Standardwert `1` bedeutet, dass das Signal die letzte vollständig
geschlossene Kerze und die davor verwendet. Eine Erhöhung des Shifts verzögert Einstiege um die angeforderte Anzahl von Balken.

## Geldmanagement
Das MMRec-Modul führt eine kurze Historie der Trade-Ergebnisse pro Richtung. Wenn die letzten `LossTriggerCount` Trades in eine Richtung alle Verlierer waren,
wechselt die Strategie zur reduzierten Ordergröße (`ReducedVolume`). Nach einem profitablen Trade oder wenn weniger als die angeforderte Anzahl von Trades verfügbar
ist, wird das Standard-Volumen (`NormalVolume`) wiederhergestellt. Dies spiegelt das Verhalten von `BuyTradeMMRecounter` und `SellTradeMMRecounter` aus der
ursprünglichen MQL-Bibliothek wider.

## Risikomanagement
Schützende Stop-Loss- und Take-Profit-Levels werden in Preisschritten ausgedrückt. Wenn eine Long-Position offen ist, prüft die Strategie, ob das Kerzen-Tief
`Einstieg - StopLossPoints * PriceStep` erreicht hat oder ob das Hoch `Einstieg + TakeProfitPoints * PriceStep` berührt hat. Die Short-Seite spiegelt die
Logik wider. Die Stops werden einmal pro abgeschlossener Kerze ausgewertet, ähnlich dem Quell-EA, der auf serverseitigen Orders mit festem Abstand basierte.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `CandleType` | Kerzen-Datentyp (Zeitrahmen) für den Indikator | 1-Stunden-Kerzen |
| `KijunLength` | Lookback der Ichimoku-Basislinie | 26 |
| `SignalBarShift` | Anzahl geschlossener Balken, die vor der Auswertung des Farbübergangs übersprungen werden | 1 |
| `BuyPosOpen` / `SellPosOpen` | Öffnung von Positionen in jede Richtung aktivieren oder deaktivieren | `true` |
| `BuyPosClose` / `SellPosClose` | Schließen bestehender Positionen beim entgegengesetzten Signal erlauben | `true` |
| `NormalVolume` | Standard-Ordervolumen | `1` |
| `ReducedVolume` | Ordervolumen nach der konfigurierten Anzahl von Verlusten | `0.1` |
| `LossTriggerCount` | Anzahl aufeinanderfolgender Verlust-Trades vor der Größenreduzierung | `2` |
| `StopLossPoints` | Stop-Abstand in Preisschritten (auf `0` setzen zum Deaktivieren) | `1000` |
| `TakeProfitPoints` | Take-Profit-Abstand in Preisschritten (auf `0` setzen zum Deaktivieren) | `2000` |

## Verwendungshinweise
- Die Strategie öffnet Trades nur, wenn der Farbübergang Erschöpfung signalisiert und die relevante Richtung aktiviert ist.
- Volume-Skalierung erfordert, dass die Plattform Trade-Ergebnisse meldet; in Backtests werden die von der Strategie generierten Exits die Verlusthistorie automatisch aktualisieren.
- Wenn kein Preisschritt für das Instrument definiert ist, werden die Stop-Loss- und Take-Profit-Eingaben ignoriert.
- Das Setzen von `SignalBarShift` auf `0` imitiert eine sofortige Reaktion auf die letzte abgeschlossene Kerze, erhöht aber das Risiko von Whipsaws.
