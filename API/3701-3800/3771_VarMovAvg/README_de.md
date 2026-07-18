# VarMovAvg-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die VarMovAvg-Strategie ist ein Stop-and-Reverse-System, das vom MetaTrader 4 Expert Advisor `VarMovAvg_v0011` abgeleitet wurde. Es verwendet einen adaptiven variablen gleitenden Durchschnitt (VMA), um die Trendrichtung zu messen, und wartet auf ein zweistufiges Rückzugsmuster (im Original EA als Balken A und Balken B bezeichnet), bevor die Position umgekehrt wird. Während eine Position aktiv ist, schützt ein auf dem gleitenden Durchschnitt basierender Trailing-Stop die Gewinne und dreht den Handel um, wenn die entgegengesetzte Bar-A-/Bar-B-Sequenz abgeschlossen ist.

## Handelslogik
1. **Adaptive VMA** – Der benutzerdefinierte `VariableMovingAverage`-Indikator repliziert die MT4-Formel:
   - Das Effizienzverhältnis vergleicht den aktuellen Schlusskurs mit dem Schlusskurs vor `AmaPeriod` Balken und dividiert ihn durch die kumulierte absolute Preisbewegung.
   - Der Glättungskoeffizient interpoliert zwischen den schnellen und langsamen Perioden und wird genau wie der ursprüngliche `G`-Wert auf den Parameter `SmoothingPower` angehoben.
2. **Signalerkennung (Balken A / Balken B)** – Zwei unabhängige Zustandsmaschinen verfolgen lange und kurze Setups:
   - *Balken A*: Der Preis bewegt sich um `SignalPipsBarA` (in Pips) über den VMA hinaus in die potenzielle Handelsrichtung.
   - *Balken B*: Der Preis verlängert sich um weitere `SignalPipsBarB` Pips in die gleiche Richtung und fixiert so den extremen Preis.
   - *Einstieg*: Wenn der Schlusskurs in das durch `SignalPipsTrade ± EntryPipsDiff` definierte Einstiegsband zurückkehrt, erfolgt der Einstieg (oder die Umkehrung) der Strategie mithilfe von Marktaufträgen.
3. **Trailing Stop und Umkehr** – Während eine Position offen ist, wird ein gleitender Durchschnitt, der auf Höchst- (für Shorts) oder Tiefstständen (für Long-Positionen) berechnet wird, um `StopMaShift` Balken verschoben und um `StopPipsDiff` aufgefüllt.
   - Wenn die Kerze das Stop-Level durchbricht, wird die Position geschlossen.
   - Wenn die entgegengesetzte Balken-A-/Balken-B-Sequenz ausgelöst wird, während eine Position besteht, gibt die Strategie eine einzelne Marktorder mit der Größe `|Position| + Volume` aus, um die Richtung sofort umzukehren, was dem EA-Verhalten entspricht.

## Parameter
| Parameter | Beschreibung | MT4-Quelle |
|-----------|-------------|------------|
| `AmaPeriod` | Von der VMA verwendetes Lookback-Fenster. | `prm.vma.periodAMA` |
| `FastPeriod` | Schneller Glättungsfaktor innerhalb des VMA. | `prm.vma.nfast` |
| `SlowPeriod` | Langsamer Glättungsfaktor innerhalb des VMA. | `prm.vma.nslow` |
| `SmoothingPower` | Exponent `G`, angewendet auf den adaptiven Koeffizienten. | `prm.vma.G` |
| `SignalPipsBarA` | Entfernung von der VMA, die erforderlich ist, um Balken A zu akzeptieren. | `prm.sig.pipsBarA` |
| `SignalPipsBarB` | Zusätzlicher Abstand erforderlich, um Bar B zu akzeptieren. | `prm.sig.pipsBarB` |
| `SignalPipsTrade` | Versatz vom äußersten Balken B zur Eintrittslinie. | `prm.sig.pipsTrade` |
| `EntryPipsDiff` | Akzeptierte Toleranz um die Eintrittslinie. | `prm.entry.diff` |
| `StopPipsDiff` | Auf den gleitenden Durchschnitt des Trailing-Stops angewendeter Offset. | `prm.stop.diff` |
| `StopMaPeriod` | Zeitraum des Stop-Moving-Average. | `prm.mastop.period` |
| `StopMaShift` | Verschiebung (Balken) des Stop-Moving-Average. | `prm.mastop.shift` |
| `StopMaMethod` | Methode des gleitenden Durchschnitts (`MODE_SMA`, `EMA`, `SMMA`, `LWMA`). | `prm.mastop.method` |
| `CandleType` | Arbeitszeitrahmen. | Zeitrahmen des Diagramms |

> **Pip-Umrechnung** – Alle Pip-Abstände werden mit `Security.PriceStep` multipliziert, sofern verfügbar. Wenn das Instrument keinen konfigurierten Schritt hat, werden die Rohwerte in Preiseinheiten interpretiert, wodurch der EA-Fallback repliziert wird.

## Nutzungshinweise
- Die Strategie basiert auf `SubscribeCandles` und läuft vollständig auf fertigen Kerzen; Die Einstiegsbandlogik spiegelt die Tick-für-Tick-Prüfungen des EA anhand der Schlusskurse wider.
- Schutzaufträge werden durch Marktaustritte modelliert, wenn die Kerze das Stop-Level überschreitet, was dem EA-Verhalten entspricht, da Stop-Orders bei jedem Tick neu berechnet wurden.
- Die Verschiebung des gleitenden Durchschnitts wird über einen FIFO-Puffer implementiert, um sicherzustellen, dass `StopMaShift = 0` den neuesten Wert verwendet und positive Verschiebungen die angeforderte Anzahl von Balken zurückblicken.
- Nach jedem Trade (Einstieg, Umkehr oder Stop-Treffer) werden beide Signal-Tracker in den neutralen Zustand zurückgesetzt, um doppelte Orders zu vermeiden, indem sie die `STATUS_TRADE`-Reset-Logik in MetaTrader emulieren.

## Schnellstart
1. Fügen Sie die Strategie einer StockSharp-Umgebung hinzu und weisen Sie ein Instrument mit einer gültigen `PriceStep`- und Tick-Größe zu.
2. Konfigurieren Sie den Zeitrahmen bis `CandleType` (der ursprüngliche Experte wurde auf Intraday-Charts wie M5 getestet).
3. Passen Sie die Pip-Abstände und Trailing-Parameter an die Kursgenauigkeit des Brokers an.
4. Starten Sie die Strategie; Es wechselt zwischen Long- und Short-Positionen, wenn die Bedingungen von Bar A/Bar B erfüllt sind.

## Unterschiede zum Original EA
- Die StockSharp-Version funktioniert bei geschlossenen Kerzen statt bei der Tick-für-Tick-Ausführung. Das Eingabetoleranzband hält das Trigger-Timing nahe am MT4-Verhalten.
- Die Stop-Loss-Handhabung wird durch die Überprüfung der Kerzenextreme implementiert, anstatt MT4-Aufträge zu erteilen/zu ändern, da StockSharp-Strategien Exits typischerweise programmgesteuert verwalten.
- Der Indikator `VariableMovingAverage` wird direkt in C# implementiert und stellt die Glättungsleistung bereit, wodurch der ungenutzte Parameter `dK` eliminiert wird, der in der Quelle MQL vorhanden war.
