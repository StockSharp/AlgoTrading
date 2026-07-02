# Strategie TwentyPipsOnceADayStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Port des MetaTrader-Experten **20pipsOnceADayOppositeLastNHourTrend**, implementiert mit dem StockSharp-High-Level-API. Die Strategie handelt einmal pro konfigurierter Stunde und eröffnet eine konträre Position gegen die Drift der letzten `N` stündlichen Kerzen. Die Positionsgröße folgt einer Martingalleiter, die das Los nur dann erhöht, wenn ein aktueller Handel mit einem Verlust endete. Die Implementierung erzwingt außerdem einen täglichen Handelsplan, einen optionalen Trailing-Schutz und eine maximale Haltedauer.

## Handelslogik

1. Die Strategie abonniert stündliche Kerzen (konfigurierbar über `CandleType`).
2. Wenn eine Kerze schließt und die nächste Stunde mit `TradingHour` übereinstimmt, wertet die Strategie die Richtung aus:
   - Vergleichen Sie den Schlusskurs der letzten abgeschlossenen Stunde mit dem Schlusskurs vor `HoursToCheckTrend` Stunden.
   - If the market fell over that interval, open a long position (fade the bearish drift).
   - Wenn der Markt steigt, eröffnen Sie eine Short-Position.
3. Es kann jeweils nur eine Position aktiv sein (gesteuert durch `MaxOrders`).
4. Jeder Trade erbt einen festen Take-Profit und einen optionalen Stop-Loss/Trailing-Stop, beide ausgedrückt in Pips im Verhältnis zur Pip-Größe des Instruments.
5. Wenn die Position länger als `OrderMaxAgeSeconds` offen bleibt oder die nächste Stunde außerhalb der durch `TradingDayHours` definierten zulässigen Sitzung liegt, schließt die Strategie den Handel zwangsweise.

## Money-Management

- `FixedVolume` definiert das Basislos. Setzen Sie es auf `0`, um das Los aus dem Portfoliowert mit `RiskPercent` abzuleiten. Die risikobasierte Dimensionierung spiegelt die ursprüngliche EA-Logik wider: `(portfolio value * RiskPercent) / 1000`.
- After the base lot is calculated it is clamped by both the instrument's `VolumeMin/VolumeMax/VolumeStep` and the user-defined `MinVolume` / `MaxVolume` bounds.
- Eine Martingalleiter erhöht das nächste Los nur dann, wenn der jeweilige historische Handel mit Verlust schloss:
  - `FirstMultiplier` gilt, wenn der letzte Trade verloren ging.
  - `SecondMultiplier` gilt, wenn der letzte Trade gewonnen hat, der vorherige jedoch verloren hat.
  - Die Kette wird bis `FifthMultiplier` fortgesetzt und entspricht der ursprünglichen fünfstufigen Eskalation.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `FixedVolume` | Festes Handelsvolumen. Verwenden Sie `0`, um die risikobasierte Größenanpassung zu aktivieren. |
| `MinVolume` / `MaxVolume` | Untere und obere Grenzen werden nach der Größenbestimmung angewendet. |
| `RiskPercent` | Portfolio-Prozentsatz in Volumen umgerechnet, wenn `FixedVolume` gleich Null ist. |
| `MaxOrders` | Maximale Anzahl gleichzeitig offener Positionen (Standard `1`). |
| `TradingHour` | Stunde des Tages (0-23), zu der neue Geschäfte beginnen können. |
| `TradingDayHours` | Durch Kommas getrennte Stunden oder Bereiche (z. B. `0-7,13-22`), die weiterhin für offene Positionen in Frage kommen. Wenn die nächste Stunde außerhalb dieser Menge liegt, wird die Strategie beendet. |
| `HoursToCheckTrend` | Rückblick in stündlichen Kerzen, die für den konträren Vergleich verwendet werden. |
| `OrderMaxAgeSeconds` | Maximale Haltezeit in Sekunden, bevor ein Ausgang erzwungen wird. |
| `FirstMultiplier` … `FifthMultiplier` | Martingale Multiplikatoren, die den in den letzten fünf geschlossenen Geschäften festgestellten Verlusten zugewiesen wurden. |
| `StopLossPips` | Anfängliche Stop-Loss-Distanz in Pips. Zum Deaktivieren auf `0` setzen. |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips. Zum Deaktivieren auf `0` setzen. |
| `TakeProfitPips` | Nehmen Sie die Gewinnentfernung in Pips. |
| `CandleType` | Candle type used for signal generation (defaults to 1-hour time frame). |

## Risk Controls and Exits

- **Take-Profit/Stop-Loss**: Konfiguriert über `TakeProfitPips` und `StopLossPips` mit automatischer Umrechnung in Instrumentenpreiseinheiten.
- **Trailing Stop**: Wenn aktiviert, wird der Stop nachgezogen, sobald der Trade mehr als die konfigurierte Anzahl an Pips gewinnt.
- **Timeout-Exit**: Positionen, die älter als `OrderMaxAgeSeconds` sind, werden zum aktuellen Schlusskurs der Kerze geschlossen.
- **Sitzungsfilter**: Positionen werden geschlossen, wenn die kommende Stunde nicht in `TradingDayHours` enthalten ist.

## Nutzungshinweise

- Die Strategie funktioniert mit jedem Instrument, das stündliche Kerzen und einen gültigen `PriceStep` bietet. Wenn das Instrument gebrochene Pips (3 oder 5 Dezimalstellen) verwendet, wird die Pip-Größe automatisch angepasst.
- Um das Verhalten von MetaTrader zu reproduzieren, führen Sie die Strategie auf einem einzelnen Instrument aus, wobei `CandleType` auf einen stündlichen Zeitrahmen eingestellt ist, und behalten Sie den Standardwert `TradingDayHours` (0-23) bei, um den Handel über den ganzen Tag hinweg zu ermöglichen.
- Die Martingalleiter geht von höchstens fünf relevanten historischen Berufen aus. Durch das Zurücksetzen der Strategie wird dieser Verlauf gelöscht.
- Da die Strategie zu Beginn der konfigurierten Stunde unter Verwendung geschlossener Kerzendaten handelt, erfolgt die Ausführung zu dem Preis, der zu Beginn der neuen Stunde verfügbar ist.

## Dateien

- `CS/TwentyPipsOnceADayStrategy.cs` – Haupt-C#-Implementierung.
- `README.md` – Englische Dokumentation (diese Datei).
- `README_zh.md` – Chinesische Dokumentation.
- `README_ru.md` – Russische Dokumentation.

Auf Python-Ports wird bei dieser Konvertierung bewusst verzichtet.
