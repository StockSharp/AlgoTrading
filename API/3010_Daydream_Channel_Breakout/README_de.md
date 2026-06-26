# Daydream-Kanalausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Daydream Channel Breakout ist eine direkte Konvertierung des ursprünglichen MetaTrader-Experten-Advisors „Daydream" in das StockSharp High-Level-Strategie-Framework. Die Logik handelt gegen extreme Bewegungen: Wenn der Kurs die untere Donchian-Kanallinie durchbricht, kauft der Algorithmus in Erwartung einer Erholung; wenn der Kurs über die obere Linie steigt, wird eine Short-Position eröffnet. Alle Ausstiege werden über einen „virtuellen" Take-Profit in Pips abgewickelt, sodass keine nativen Exchange-Orders im Buch verbleiben.

## Strategielogik

- Einen Donchian-Kanal aus den `ChannelPeriod` abgeschlossenen Kerzen aufbauen (die aktuelle Kerze ist ausgeschlossen, entsprechend der MT5-Implementierung).
- **Long** einsteigen, wenn der Schlusskurs unter das vorherige untere Band fällt. Bestehende Short-Positionen werden implizit geschlossen, weil das Ordervolumen den absoluten Positionsbetrag einschließt.
- **Short** einsteigen, wenn der Schlusskurs über das vorherige obere Band bricht. Bestehende Long-Positionen werden auf die gleiche Weise geschlossen.
- Pro Kerze ist nur ein Einstieg erlaubt. Nach dem Senden einer Order wartet die Strategie auf die nächste Balkeneröffnung, um ein neues Signal zu erzeugen.
- Jede offene Position wird auf ein virtuelles Gewinnziel überwacht. Wenn der unrealisierte Gewinn `TakeProfitPips` übersteigt (in Kursabstand über die Pip-Größen-Heuristik umgerechnet), wird die Position zu Marktpreisen geschlossen.

## Parameter

| Name | Beschreibung | Standard | Hinweise |
| --- | --- | --- | --- |
| `OrderVolume` | Losgröße, die mit jedem neuen Trade gesendet wird. Der tatsächliche Orderbetrag umfasst auch den absoluten Wert der Gegenposition, um vor dem Umkehren zu glätten. | `0.1` | Entspricht der MT5-Standard-Losgröße. |
| `TakeProfitPips` | Virtueller Take-Profit-Abstand in Pips. | `50` | Die Pip-Größe wird aus `Security.PriceStep` abgeleitet; 3- oder 5-stellige Instrumente werden automatisch mit 10 multipliziert. |
| `ChannelPeriod` | Anzahl der abgeschlossenen Kerzen für die Berechnung des Donchian-Kanals. | `25` | Verwendet denselben Rückblick wie der ursprüngliche EA. |
| `CandleType` | Für Berechnungen abonnierter Kerzentyp. | `TimeSpan.FromHours(1).TimeFrame()` | Kann auf beliebigen StockSharp-Kerzentyp geändert werden. |

## Signalfluss

1. **Datenabonnement**: Die Strategie abonniert den über den Parameter `CandleType` bereitgestellten Kerzentyp und bindet einen Donchian-Kanal-Indikator mittels `BindEx`.
2. **Virtueller Take-Profit-Check**: Die erste Aktion bei jeder abgeschlossenen Kerze ist die Messung des Abstands zwischen dem Schlusskurs und dem durchschnittlichen Einstiegspreis. Wenn der Schwellenwert erreicht wird, wird die Position geschlossen und kein neuer Einstieg für diesen Balken bewertet.
3. **Kanalaktualisierung**: Sobald sowohl obere als auch untere Bänder verfügbar sind, werden die vorherigen Werte zwischengespeichert, um die „shift=1"-Logik aus MQL widerzuspiegeln. Signale verwenden das vorherige Band, nicht das mit der aktuellen Kerze aktualisierte.
4. **Einstiegsentscheidung**:
   - Kurs < vorheriges unteres Band → kaufen `OrderVolume + Math.Max(0, -Position)`.
   - Kurs > vorheriges oberes Band → verkaufen `OrderVolume + Math.Max(0, Position)`.
5. **Protokollierung und Visualisierung**: Für jeden Einstieg und Take-Profit-Ausstieg werden informative Protokollnachrichten erzeugt. Wenn ein Diagrammbereich in Designer oder anderen StockSharp-Produkten verfügbar ist, werden Kerzen, der Donchian-Kanal und Trades automatisch gezeichnet.

## Risikomanagement

- Nur ein virtueller Take-Profit ist implementiert. Im ursprünglichen Algorithmus gibt es keinen Stop-Loss oder Trailing-Ausstieg, daher muss das Risiko extern kontrolliert werden (z. B. mit Schutz auf Portfolio-Ebene).
- Da Orders durch Addition der absoluten Position umkehren, kann die Strategie in dieselbe Richtung pyramidisieren, wenn aufeinanderfolgende Signale über verschiedene Kerzen auftreten.
- Der Pip-Größen-Helfer multipliziert den Preisschritt für 3- oder 5-stellige Symbole mit zehn, um die MT5-`Point()`-zu-Pip-Konvertierung zu emulieren. Für Instrumente mit unkonventionellen Tick-Größen können Sie die Logik überschreiben oder einen benutzerdefinierten Abstand durch Anpassen von `TakeProfitPips` verwenden.

## Verwendungshinweise

- Die Strategie ist für Mean-Reversion-Verhalten gedacht. Sie funktioniert am besten auf seitwärtsgerichteten Märkten, wo überstreckte Bewegungen zur Umkehr neigen.
- Backtests sollten realistische Spread- und Provisionseinstellungen einschließen, da Einstiege bei Marktorders nach Kanalbrüchen erfolgen.
- Erwägen Sie, die Strategie mit Session-Filtern oder volatilitätsbasierten Stops zu koppeln, wenn Sie an Live-Börsen handeln.
- Die Implementierung basiert ausschließlich auf der StockSharp High-Level-API (keine manuellen Indikator-Sammlungen oder historische Downloads), sodass sie mit Designer, Shell und Runner von Haus aus kompatibel ist.
