# CMO Duplex Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie ist eine StockSharp-Portierung des MetaTrader 5-Experten `Exp_CMO_Duplex.mq5`. Sie teilt die Logik in zwei unabhängige Legs
(Long und Short) auf, die beide auf Nulllinien-Kreuzungen des Chande Momentum Oscillators (CMO) reagieren. Jedes Leg kann seine eigene
Kerzenserie, Periode und Signal-Offset verbrauchen, was es ermöglicht, asymmetrische Konfigurationen auf demselben Instrument zu betreiben.

## Funktionsweise

- Die Strategie abonniert einen oder zwei Kerzen-Feeds, je nachdem ob die Long- und Short-Legs dasselbe `DataType` verwenden.
- Jedes Leg besitzt seine eigene CMO-Indikatorinstanz. Der Indikator wird nur auf fertigen Kerzen ausgewertet.
- Die `SignalBar`-Einstellung definiert, wie viele abgeschlossene Kerzen im Verlauf für die Kreuzungslogik verwendet werden sollen. Ein Wert von 0
  bedeutet «die aktuellste geschlossene Bar verwenden», `1` verwendet die vorherige Bar, `2` verwendet die davor, und so weiter.
- **Long-Leg:** Wenn der ausgewählte CMO-Wert von über null auf null oder darunter kreuzt, tritt die Strategie in (oder dreht in) eine Long-
  Position, wenn Long-Einstiege erlaubt sind. Long-Ausstiege werden ausgelöst, wenn der ältere CMO-Wert unter null liegt oder wenn Stop-Loss-/Take-Profit-Level
  berührt werden.
- **Short-Leg:** Spiegelt die Long-Logik. Ein Kreuz von unter null auf null oder darüber öffnet (oder dreht in) eine Short-Position und
  das entgegengesetzte Vorzeichen des CMO-Werts oder die konfigurierten Stops schließen die Position.
- Positionsdrehungen verwenden `Volume` plus jede gegensätzliche Exposition, sodass eine einzelne Marktorder die vorherige Position schließt und
  die neue öffnet.
- `StartProtection()` ist beim Start aktiviert, sodass die eingebauten StockSharp-Risikokontrollen aktiv bleiben.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `LongCandleType` | Kerzentyp, der vom Long-Leg verwendet wird. |
| `LongCmoPeriod` | Periode des CMO-Indikators auf der Long-Seite. |
| `LongSignalBar` | Anzahl der geschlossenen Bars zwischen der aktuellen Zeit und der für Signale analysierten Bar (0 = aktuellste geschlossene Bar). |
| `EnableLongEntries` | Erlaubt oder blockiert das Öffnen neuer Long-Positionen. |
| `EnableLongExits` | Erlaubt oder blockiert das Schließen von Long-Positionen auf Oszillatorsignale. |
| `LongStopLossPoints` | Stop-Loss-Abstand in Preisschritten für Long-Trades (0 deaktiviert den Stop). |
| `LongTakeProfitPoints` | Take-Profit-Abstand in Preisschritten für Long-Trades (0 deaktiviert das Ziel). |
| `ShortCandleType` | Kerzentyp, der vom Short-Leg verwendet wird. |
| `ShortCmoPeriod` | Periode des CMO-Indikators auf der Short-Seite. |
| `ShortSignalBar` | Anzahl der geschlossenen Bars zwischen der aktuellen Zeit und der für Short-Signale analysierten Bar. |
| `EnableShortEntries` | Erlaubt oder blockiert das Öffnen neuer Short-Positionen. |
| `EnableShortExits` | Erlaubt oder blockiert das Schließen von Short-Positionen auf Oszillatorsignale. |
| `ShortStopLossPoints` | Stop-Loss-Abstand in Preisschritten für Short-Trades (0 deaktiviert den Stop). |
| `ShortTakeProfitPoints` | Take-Profit-Abstand in Preisschritten für Short-Trades (0 deaktiviert das Ziel). |

Die Basis-Eigenschaft `Strategy.Volume` steuert die Standard-Ordergröße. Wenn die Strategie die Richtung drehen muss, sendet sie eine Marktorder,
deren Volumen gleich `Volume + |Position|` ist, was die alte Exposition schließt und die neue in einer einzigen Transaktion öffnet.

## Risikomanagement

- Stop-Loss- und Take-Profit-Level werden bei jeder fertigen Kerze ausgewertet. Für Long-Positionen wird der Stop unter dem Einstieg platziert
  und das Ziel darüber; für Short-Positionen werden die Level gespiegelt.
- Ein Stop oder ein Ziel löst eine sofortige Marktorder zum Schließen der Position aus. Die gleiche Ausstiegsroutine läuft auch, wenn der jeweilige
  Oszillatorwert das falsche Vorzeichen beibehält (unter null für Longs, über null für Shorts).
- Die Distanz auf null zu setzen deaktiviert den entsprechenden Schutz und lässt das Leg rein durch die Oszillatorlogik verwalten.

## Verwendungshinweise

- Die Strategie funktioniert am besten auf Instrumenten, bei denen der CMO nach dem Berühren der Nulllinie umkehrt. Konträre Einstiege werden
  durch den `SignalBar`-Offset absichtlich verzögert, um dem ursprünglichen Experten zu entsprechen.
- Long- und Short-Legs können denselben Kerzen-Feed teilen oder auf verschiedenen Zeitrahmen operieren. Wenn beide dasselbe `DataType` verwenden, verwendet die
  Strategie ein einzelnes Abonnement für bessere Leistung.
- Da die Strategie auf abgeschlossenen Kerzen operiert, wird empfohlen, einen kontinuierlichen Kerzenstrom zu liefern (z. B. über einen
  historischen Backtest oder einen Echtzeit-Feed), um fehlende Signale zu vermeiden.
