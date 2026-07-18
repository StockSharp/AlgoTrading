# The 20s Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Konvertierung des MetaTrader-Expert-Advisors **Exp_The_20s_v020**. Sie reproduziert die ursprüngliche "The 20s"-Indikatorlogik, die nach Ausbruchsmustern nach einem Volatilitätssqueeze sucht. Der Algorithmus analysiert abgeschlossene Kerzen aus einem konfigurierbaren Zeitrahmen und reagiert, wenn der Preis durch 20%-Bänder um den Bereich der vorherigen Bar ausbricht. Die Implementierung behält das High-Level-Feel der StockSharp-API bei und exponiert alle Trading-Berechtigungen, sodass Long- oder Short-Aktionen unabhängig aktiviert oder deaktiviert werden können.

## Signallogik
Der Indikator überwacht die neuesten Kerzen und berechnet Referenzniveaus aus der vorherigen Bar:

1. Den Bereich der vorherigen Kerze messen: `range = high[1] - low[1]`.
2. Zwei Schwellenwerte um diese Bar aufbauen:
   - `top = high[1] - range * Ratio`
   - `bottom = low[1] + range * Ratio`
3. Die aktuelle Kerze gegen die Schwellenwerte und den `LevelPoints`-Abstand (in Preis umgerechnet mit dem `PriceStep` des Instruments) vergleichen.

Der ursprüngliche Code exponiert zwei Berechnungsmodi:

- **Mode1 (Standard)** – sucht nach einem falschen Ausbruch innerhalb des 20%-Bandes auf der vorherigen Kerze, gefolgt von einer starken Ablehnung auf der aktuellen Kerze. Abhängig von `IsDirect` kauft die Strategie den Rückgang (`Direct = true`) oder verkauft ihn (`Direct = false`).
- **Mode2** – erfordert eine Serie von drei sich ausdehnenden Kerzen vor dem Signal. Wenn die Kompression nach unten ausbricht und der Preis unter dem unteren Band eröffnet, wird eine Richtung ausgelöst; wenn er über dem oberen Band eröffnet, wird die entgegengesetzte Richtung ausgelöst. `IsDirect` kehrt erneut die Richtung um, um dem ursprünglichen EA-Verhalten zu entsprechen.

Der `SignalBar`-Parameter verschiebt die Ausführung um mehrere Bars (0 = aktuelle Kerze, 1 = vorherige Kerze, usw.). Dies reproduziert die Fähigkeit des Expert-Advisors, auf ältere Signale zu reagieren, sobald sie vollständig gebildet sind.

## Handelsverwaltung
- **Einstiege**: `AllowLongEntry` und `AllowShortEntry` kontrollieren, ob neue Positionen geöffnet werden. Der `OrderVolume`-Parameter definiert die Handelsgröße für jede neue Position.
- **Positionsumkehrungen**: Wenn ein bullisches Signal erscheint, deckt die Strategie zunächst jede Short-Exposition (`AllowShortExit`) ab und öffnet dann optional eine Long-Position. Das bärische Signal spiegelt diese Logik für Long-Positionen wider.
- **Stops & Ziele**: `StopLossPoints` und `TakeProfitPoints` werden in Instrument-Punkten gemessen. Sie werden mit `PriceStep` in Preise umgerechnet und bei jeder abgeschlossenen Kerze ausgewertet. Wenn ein Niveau berührt wird, wird die Position sofort geschlossen.
- **Direktmodus**: Das Setzen von `IsDirect` auf `true` ahmt die ursprünglichen Indikatorausgaben nach. Das Umschalten auf `false` invertiert die Pfeilrichtungen, was nützlich ist, wenn das Verhalten auf Märkten mit unterschiedlichen Eigenschaften gespiegelt werden soll.

## Parameter
- `OrderVolume` – Standard `1`. Lotgröße für neue Positionen.
- `StopLossPoints` – Standard `1000`. Schutz-Stop in Punkten (`0` deaktiviert ihn).
- `TakeProfitPoints` – Standard `2000`. Gewinnziel in Punkten (`0` deaktiviert es).
- `AllowLongEntry` / `AllowShortEntry` – Long/Short-Einstiege aktivieren.
- `AllowLongExit` / `AllowShortExit` – der Strategie erlauben, bestehende Positionen bei entgegengesetzten Signalen zu schließen.
- `SignalBar` – Standard `1`. Anzahl der Bars, die gewartet werden soll, bevor auf ein Signal reagiert wird.
- `LevelPoints` – Standard `100`. Abstand, der Ausbrüche über die vorherigen Bar-Extrema hinaus bestätigt.
- `Ratio` – Standard `0.2`. Breite der 20%-Bänder um die vorherige Kerze.
- `IsDirect` – Standard `false`. Behält das ursprüngliche Kauf/Verkauf-Mapping bei `true`, kehrt es bei `false` um.
- `Mode` – Standard `Mode1`. Wählt zwischen den zwei Berechnungsalgorithmen.
- `CandleType` – Standard `H1`-Zeitrahmen. Definiert das Abonnement für die Berechnungen.

## Hinweise
- Die Strategie arbeitet nur auf abgeschlossenen Kerzen; partielle Kerzen werden ignoriert, um vorzeitige Trades zu vermeiden.
- Alle Log-Einträge und Inline-Kommentare sind auf Englisch, um den Code konsistent mit StockSharp-Beispielen zu halten.
- Die Stop- und Ziel-Verwaltung wird innerhalb der Strategie behandelt und ist nicht von zusätzlichen Orders abhängig, was das Verhalten über Simulatoren und Live-Broker hinweg portierbar macht.
- Sie können die Strategie an jedes Instrument anhängen. Stellen Sie nur sicher, dass die `PriceStep`-Eigenschaft verfügbar ist, damit punktbasierte Abstände korrekt umgerechnet werden.
- Erwägen Sie, `Mode2` mit einem größeren `SignalBar` auf höheren Zeitrahmen zu kombinieren, um das "auf Bestätigung warten"-Verhalten des EA zu emulieren.
