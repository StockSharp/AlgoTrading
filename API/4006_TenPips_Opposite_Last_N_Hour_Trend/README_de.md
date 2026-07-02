# Zehn Pips gegenüber der letzten N-Stunden-Trendstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine getreue Umsetzung des MetaTrader-Experten **10pipsOnceADayOppositeLastNHourTrend**. Der Handel erfolgt genau einmal am Tag zu einer konfigurierbaren Stunde und nimmt bewusst die entgegengesetzte Seite der Preisänderung ein, die über die letzten *N* abgeschlossenen stündlichen Kerzen beobachtet wurde. Die Logik ist für Währungspaare mit fünfstelligen Preisen konzipiert, aber die C#-Version passt die Pip-Größe automatisch anhand der `PriceStep` des Instruments und der Anzahl der Dezimalstellen an.

Zur ausgewählten Handelszeit prüft die Strategie den Schlusskurs von vor `HoursToCheckTrend` Stunden und vergleicht ihn mit dem Schlusskurs der zuletzt abgeschlossenen stündlichen Kerze:

- Wenn der ältere Schlusskurs **höher** ist, ist der Markt gefallen (bärisch), sodass die Strategie eine **Long-Position** eröffnet.
- Otherwise the market has been rising (bullish), therefore it opens a **short** position.

Positionen werden durch Schutzstopps, einen täglichen zeitbasierten Ausstieg oder manuell geschlossen, wenn sich der Markt außerhalb des Handelsfensters befindet.

## Geldmanagement

Die Positionsgrößenbestimmung spiegelt die Martingalleiter des ursprünglichen Experten wider:

1. Das Basisvolumen stammt von `FixedVolume`. Wenn der Wert auf Null gesetzt ist, greift die Strategie auf die risikobasierte Dimensionierung zurück, wobei `Portfolio.CurrentValue * MaximumRisk / 1000` auf eine Dezimalstelle gerundet wird.
2. Das Volumen ist durch `MinimumVolume`, `MaximumVolume`, die Volumengrenzen des Instruments und eine Soft-Cap in Höhe von `Portfolio.CurrentValue / 1000` Lots begrenzt.
3. Nach jedem abgeschlossenen Trade wird das Ergebnis gespeichert (bis zu den letzten fünf Trades). Bei der Vorbereitung eines neuen Eintrags durchsucht die Strategie diesen Verlauf und multipliziert die Losgröße entsprechend dem ersten gefundenen Verlust unter Verwendung der Sequenz `FirstMultiplier` … `FifthMultiplier`. Dies reproduziert die verschachtelten `OrderSelect`-Prüfungen aus der MQL-Version.

## Risk controls

- `StopLossPips`, `TakeProfitPips` und `TrailingStopPips` arbeiten in Pip-Einheiten. Der Port berechnet die Pip-Größe mit dem standardmäßigen 3/5-Dezimalmultiplikator für Forex-Symbole neu.
- Trailing Stops sind für Long- und Short-Positionen symmetrisch. In the original MQL code the short-side trail never triggered because of a sign error; Die C#-Version behebt dieses Problem, sodass sich beide Richtungen identisch verhalten.
- `OrderMaxAge` schließt jede Position, die länger als die konfigurierte Dauer (standardmäßig 21 Stunden) bestehen bleibt.
- Außerhalb der zulässigen Handelszeit liquidiert die Strategie alle offenen Positionen, um bis zur nächsten Sitzung unverändert zu bleiben.
- `MaxOrders` schützt vor versehentlichen Wiedereintritten, indem es verlangt, dass keine offenen Positionen oder aktiven Aufträge vorhanden sind, wenn ein neues Signal ausgewertet wird.

## Detaillierter Arbeitsablauf

1. Abonnieren Sie stündliche Kerzen (der Zeitrahmen kann mit `CandleType` geändert werden).
2. Sammeln Sie den Schlusskurs jeder fertigen Kerze in einem kleinen Rollpuffer.
3. Bei der ersten fertigen Kerze zur erlaubten Stunde:
   - Überprüfen Sie den Portfolio-/Verbindungsstatus und stellen Sie sicher, dass keine Position offen ist.
   - Stellen Sie sicher, dass wir mindestens `HoursToCheckTrend` historische Kerzen zum Vergleich haben.
   - Bestimmen Sie die Richtung, indem Sie den aktuellen Schlusskurs mit dem Schlusskurs vor `HoursToCheckTrend` Balken vergleichen.
   - Berechnen Sie die Losgröße mithilfe der oben genannten Money-Management-Routine und senden Sie eine Marktorder.
4. Während eine Position offen ist, gilt die Strategie:
   - Bewertet Stop-Loss-, Take-Profit- und Trailing-Levels anhand der Höchst-/Tiefstkurse der Kerze.
   - Aktualisiert den Trailing Stop nach neuen Höchstständen (für Long-Positionen) oder Tiefstständen (für Short-Positionen).
   - Tracks the entry timestamp so it can enforce `OrderMaxAge`.
   - Zeichnet den realisierten Gewinn/Verlust auf, wenn der Handel geschlossen wird, um die Martingal-Multiplikatoren zu versorgen.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `FixedVolume` | Feste Losgröße. Auf `0` setzen, um die risikobasierte Größenanpassung zu verwenden. | `0.1` |
| `MinimumVolume` | Harte Untergrenze für das Auftragsvolumen. | `0.1` |
| `MaximumVolume` | Harte Obergrenze für das Bestellvolumen. | `5` |
| `MaximumRisk` | Anteil des Eigenkapitals, der verwendet wird, wenn `FixedVolume = 0`. | `0.05` |
| `MaxOrders` | Maximale gleichzeitige Bestellungen/Positionen. | `1` |
| `TradingHour` | Tageszeit (0–23), zu der neue Trades zulässig sind. | `7` |
| `HoursToCheckTrend` | Rückblickfenster in Stunden für den Trendvergleich. | `30` |
| `OrderMaxAge` | Maximale Lebensdauer einer Position. | `21h` |
| `StopLossPips` | Stop-Loss-Distanz in Pips. | `50` |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. | `10` |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips. | `0` (deaktiviert) |
| `FirstMultiplier` … `FifthMultiplier` | Lot multipliers applied when the most recent losing trade is found at the respective depth. | `4`, `2`, `5`, `5`, `1` |
| `CandleType` | Zeitrahmen für das Kerzenabonnement. | `1 hour` |

## Unterschiede zum ursprünglichen MQL-Experten

- Martingale-Größe, Orderalterung und Handelsfensterlogik werden eins zu eins reproduziert. Die einzige bewusste Änderung ist der symmetrische Short-Side-Trailing-Stop, um den Vorzeichenfehler im Originalskript zu beheben.
- Alle Schutzniveaus werden mit Marktaufträgen für die nächste abgeschlossene Kerze ausgeführt, da StockSharp-Strategien bei der Verwendung von High-Level-Helfern keine separaten Stop-/Limit-Orders registrieren. Dies entspricht dem Verhalten des ursprünglichen Experten, als seine Stop-Orders ausgelöst wurden.
- Das Kontoguthaben wird aus `Portfolio.CurrentValue` gelesen. Wenn der Adapter dieses Feld nicht bereitstellt, greift die Strategie auf die Basis `Volume` (Standard `1`) zurück.
- Die Liste der zulässigen Handelszeiten spiegelt das ursprüngliche Array von `0…23` wider. To restrict trading to specific days you can edit `_tradingDayHours` inside the constructor.

## Nutzungshinweise

- Funktioniert am besten bei stündlichen Forex-Daten, bei denen Pip-Größenberechnungen mit der `PriceStep` ×10-Heuristik gültig sind.
- Stellen Sie immer sicher, dass `Security.VolumeStep`, `VolumeMin` und `VolumeMax` vom Konnektor festgelegt sind, damit die Strategie die Losgrößen korrekt anpassen kann.
- Da Einträge nur einmal pro fertiger Kerze ausgewertet werden, sollte die Strategie vor der gewählten Handelsstunde gestartet werden, damit das erste Signal des Tages nicht verpasst wird.
