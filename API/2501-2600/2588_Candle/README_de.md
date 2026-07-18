# Kerzen-Strategie (Candle)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Kerzen-Strategie (Candle)** ist ein direkter Port des klassischen MT5-Experten „Candle.mq5". Sie bewertet die Farbe jeder abgeschlossenen Kerze im gewählten Zeitrahmen und hält die Position im Einklang mit dem letzten Schlusskurs. Bullische Kerzen treiben die Strategie in eine Long-Position, bärische in eine Short-Position, und flache Kerzen lassen die Position unverändert. Das Risiko wird durch Pip-basierte Take-Profit- und Trailing-Stop-Abstände kontrolliert, die über die Tick-Größe des Instruments in absolute Preise umgerechnet werden.

Die Strategie reagiert erst nach vollständiger Ausbildung einer Kerze, um Lärm innerhalb der Bar zu vermeiden. Ein obligatorischer Rückblick (`MinBars * 2` abgeschlossene Kerzen) validiert, dass das Chart ausreichend Historie enthält, während eine konfigurierbare Abkühlzeit zwischen Handelsvorgängen wartet. Dies erzeugt eine getreue StockSharp-Implementierung der ursprünglichen MetaTrader-Logik ohne Abhängigkeit von Low-Level-Serienzugriff.

## Handelsstrategie
### Vorbereitung
- Verarbeitet Kerzen, die von `CandleType` bereitgestellt werden; keine weiteren Datenquellen erforderlich.
- Wartet, bis mindestens `2 * MinBars` abgeschlossene Kerzen verarbeitet wurden, bevor Einstiege erlaubt sind.
- Handelt nur, wenn die Strategie online, geformt und zur Orderausführung berechtigt ist.
- Erzwingt das `TradeCooldown`-Intervall (Standard: 10 Sekunden) zwischen jeweils zwei Handelsvorgängen.

### Einstiegs- und Umkehrregeln
1. **Flacher Zustand:**
   - Long einsteigen (`BuyMarket`), wenn eine Kerze über ihrer Eröffnung schließt.
   - Short einsteigen (`SellMarket`), wenn eine Kerze unter ihrer Eröffnung schließt.
2. **Bestehende Position:**
   - Bei einer Long-Position gegenüber einer bärischen Kerze `|Position| + Volume` verkaufen, um zu schließen und sofort zu einer Short-Position der Größe `Volume` umzukehren.
   - Bei einer Short-Position gegenüber einer bullischen Kerze `|Position| + Volume` kaufen, um zu schließen und sofort zu einer Long-Position der Größe `Volume` umzukehren.
3. **Neutrale Kerzen:**
   - Wenn Schluss gleich Eröffnung ist, wird keine manuelle Aktion durchgeführt; nur die Schutzorders können den Trade beenden.

### Risikomanagement und Ausstiege
- `StartProtection` legt einen in Pips gemessenen Take-Profit und Trailing Stop fest. Die Strategie multipliziert jeden Pip-Wert mit `(PriceStep * 10)`, um die MetaTrader-Anpassung für 3- und 5-stellige Notierungen nachzubilden.
- Der Trailing Stop wird nur aktiviert, wenn `TrailingStopPips` größer als null ist; er folgt dem Preis automatisch, sobald sich der Trade in die günstige Richtung bewegt.
- Der Take-Profit schließt die Position, wenn der konfigurierte Abstand erreicht ist. Jedes Schutzniveau storniert die entgegengesetzte Order nach der Ausführung.
- Manuelle Umkehrungen durch die Kerzenfarbe glätten ebenfalls das vorherige Engagement vor dem Öffnen der neuen Position.

## Parameter
- `CandleType` – Zeitrahmen der zu analysierenden Kerzenserie (Standard: 15-Minuten-Kerzen).
- `TakeProfitPips` – Abstand zum Take-Profit-Ziel in Pips (Standard: 50).
- `TrailingStopPips` – Trailing-Stop-Abstand in Pips (Standard: 30).
- `MinBars` – Mindestanzahl von Bars vor dem ersten Trade (Standard: 26; Strategie wartet auf 52 abgeschlossene Kerzen).
- `TradeCooldown` – Wartezeit nach jeder Handelsaktion (Standard: 10 Sekunden).

Setzen Sie die `Volume`-Eigenschaft der Strategie auf die gewünschte Ordergröße. Bei einer Marktumkehr sendet die Strategie automatisch genügend Volumen, um sowohl die vorherige Position zu beenden als auch die neue zu etablieren.

## Implementierungshinweise
- Nur abgeschlossene Kerzen (`CandleStates.Finished`) werden verarbeitet. Dies spiegelt den MetaTrader-Experten wider, der sich auf geschlossene Barwerte stützte, die über `CopyOpen/CopyClose` abgerufen wurden.
- Der Code verwendet StockSharps High-Level-API: `SubscribeCandles` für Daten, `Bind` zur Verarbeitung eingehender Bars und `BuyMarket`/`SellMarket` zur Orderausführung.
- Schutzorders werden von `StartProtection` verwaltet, sodass keine manuelle Stop-Limit-Orderverwaltung notwendig ist.
- Die Pip-Größenberechnung `PriceStep * 10` reproduziert die MQL-„Digits-Adjust"-Logik für Symbole, die mit 3 oder 5 Dezimalstellen notiert sind.
- Da Einstiege durch den Körper der letzten Kerze ausgelöst werden, bleibt die Strategie tendenziell kontinuierlich im Markt und wechselt die Seite, wenn sich die Kerzenfarbe ändert.

Passen Sie die Pip-Abstände, die Abkühlzeit und den Zeitrahmen an das gehandelte Instrument an. Die Standardkonfiguration spiegelt das ursprüngliche MT5-Beispiel wider, kann aber über das Parameterframework von StockSharp optimiert werden.
