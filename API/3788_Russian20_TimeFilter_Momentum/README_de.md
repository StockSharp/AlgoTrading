# Russian20 Zeitfilter-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Russian20 Time Filter Momentum Strategy** ist eine Umsetzung des MetaTrader 4-Expertenberaters `Russian20-hp1.mq4`, der ursprünglich von Gordago Software Corp. vertrieben wurde. Der Algorithmus kombiniert einen 20-Perioden einfachen gleitenden Durchschnitt (SMA) mit einem 5-Perioden-Momentum-Indikator, der anhand von 30-Minuten-Kerzen ausgewertet wird. Positionen werden nur eröffnet, wenn Preisdynamik und Trendrichtung übereinstimmen, optional beschränkt auf ein benutzerdefiniertes Intraday-Handelsfenster.

## Handelslogik
- **Datenhäufigkeit:** Verwendet den konfigurierbaren Kerzentyp (Standard: 30-Minuten-Kerzen, passend zu `PERIOD_M30` aus dem MT4-Skript). Alle Signale werden nur bei vollständig geschlossenen Kerzen ausgewertet, um der Bar-Close-Ausführung des ursprünglichen Experten treu zu bleiben.
- **Indikatoren:**
  - Einfacher gleitender Durchschnitt mit einstellbarer Länge (Standard 20).
  - Momentum-Indikator mit konfigurierbarem Lookback (Standard 5) und einem neutralen Level von 100, genau wie in MetaTrader.
- **Langer Einstieg:** Wird ausgelöst, wenn die folgenden Bedingungen auf dem letzten geschlossenen Balken übereinstimmen:
  1. Der Schlusskurs liegt über dem SMA.
  2. Das Momentum liegt über dem neutralen Schwellenwert (Standard 100).
  3. Der aktuelle Schlusskurs ist höher als der vorherige Kerzenschlusskurs.
- **Kurzer Eintrag:** Wird ausgelöst, wenn:
  1. Der Schlusskurs liegt unter dem SMA.
  2. Das Momentum liegt unter der neutralen Schwelle.
  3. Der aktuelle Schlusskurs ist niedriger als der vorherige Schlusskurs.
- **Ausgangsregeln:**
  - Long-Positionen werden geschlossen, wenn das Momentum wieder auf oder unter den Schwellenwert fällt oder wenn das Take-Profit-Ziel (falls aktiviert) erreicht wird.
  - Short-Positionen werden geschlossen, wenn das Momentum den Schwellenwert erreicht oder überschreitet oder wenn das Take-Profit-Ziel erreicht wird.

## Sitzungsfilter
Das MetaTrader-Skript bot ein optionales Handelsfenster (Standard 14:00–16:00). Der Port StockSharp zeigt dasselbe Verhalten über die Parameter `UseTimeFilter`, `StartHour` und `EndHour` an. Wenn der Filter aktiv ist, überspringt die Strategie sowohl Ein- als auch Ausstiege außerhalb der ausgewählten Stunden und spiegelt damit die Frührückkehrlogik des ursprünglichen Experten wider.

## Risikomanagement
Die MQL4-Version fügte jeder Bestellung einen festen Take-Profit von 20 Pip hinzu. Die Konvertierung behält diese Funktion bei und drückt den Abstand in „Pips“ aus, wobei über den `PriceStep` des Instruments automatisch die gebrochene Pip-Preisgestaltung (3/5 Dezimalstellen) angepasst wird. Wenn Sie `TakeProfitPips` auf Null setzen, wird das Gewinnziel vollständig deaktiviert.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | 30-Minuten-Kerzen | Datentyp, der für Preis-/Indikatorberechnungen verwendet wird. |
| `MovingAverageLength` | 20 | Rückblick auf den Trendfilter SMA. |
| `MomentumPeriod` | 5 | Rückblick auf den Momentum-Indikator. |
| `MomentumThreshold` | 100 | Neutrales Momentum-Niveau, das für Ein- und Ausstiege verwendet wird. |
| `TakeProfitPips` | 20 | Gewinnzielentfernung in Pips. Null deaktiviert das Ziel. |
| `UseTimeFilter` | falsch | Aktiviert den Intraday-Handelssitzungsfilter. |
| `StartHour` | 14 | Inklusive Startstunde des Handelsfensters (0–23). |
| `EndHour` | 16 | Einschließlich der Endstunde des Handelsfensters (0–23). |

Alle Parameter werden über `StrategyParam<T>` definiert, sodass sie in der Benutzeroberfläche sichtbar und für die Optimierung bereit bleiben.

## Implementierungshinweise
- Verwendet das übergeordnete `SubscribeCandles().Bind(...)` API, sodass Indikatorwerte ohne manuelle Serienverwaltung direkt in die Verarbeitungsroutine gestreamt werden.
- Speichert nur den aktuellsten Schlusskurs, um aufeinanderfolgende Kerzen zu vergleichen, wodurch umfangreiche historische Abfragen vermieden werden und die Richtlinien zur Repository-Leistung eingehalten werden.
- Berechnet den Pip-Multiplikator automatisch von `Security.PriceStep` neu und stellt so korrekte Take-Profit-Abstände über Forex-Symbole mit 4/5-stelliger Preisgestaltung sicher.
- Fügt optionale Chart-Rendering-Hooks (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) für eine bequeme visuelle Analyse hinzu, wenn die Host-Umgebung dies unterstützt.

## Nutzungstipps
- Passen Sie den Kerzentyp an den Zeitrahmen an, in dem Sie handeln möchten. Für Forex-Paare ist die ursprüngliche 30-Minuten-Einstellung ein vernünftiger Ausgangspunkt.
- Wenn `UseTimeFilter` aktiviert ist, stellen Sie sicher, dass `StartHour` kleiner oder gleich `EndHour` ist. Wenn Sie die Startstunde später als die Endstunde festlegen, wird der Handel praktisch deaktiviert, da die MT4-Logik die Verarbeitung außerhalb des angegebenen Intervalls einfach übersprungen hat.
- Da der Experte nie einen Stop-Loss verwendet hat, sollten Sie erwägen, die Strategie beim Handel mit Live-Kapital mit zusätzlichen Risikokontrollen zu kombinieren (manuell oder über StockSharp-Schutzfunktionen).
