# Intelligente Trendfolger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Smart Trend Follower Strategy** ist eine StockSharp-Portierung des MetaTrader 5 Expertenberaters *Smart Trend Follower*. Die
Das ursprüngliche System wechselt zwischen einem konträren Crossover mit gleitendem Durchschnitt und einem Trendfolge-Setup, das Stochastik verwendet
Bestätigung. Es skaliert in Positionen mit einem Martingal-ähnlichen Volumenmultiplikator und behält einen gemeinsamen Take-Profit/Stop-Loss bei
für jeden Richtungskorb. Die Version StockSharp behält das gleiche Verhalten bei, wenn die High-Level-Version API (Kerze) verwendet wird
Abonnements, Indikatorbindungen und Marktaufträge).

## Signallogik
Es stehen zwei unabhängige Signal-Engines zur Verfügung, die mit dem Parameter `SignalMode` umgeschaltet werden können:

1. **CrossMa** – repliziert das ursprüngliche konträre Crossover. Wenn der schnelle SMA *unter* den langsamen SMA kreuzt (schnell < langsam
aber vorher schnell > langsam) eröffnet oder mittelt die Strategie Long-Positionen. Wenn das schnelle SMA *über* das langsame kreuzt
SMA (schnell > langsam, aber zuvor schnell < langsam) öffnet oder mittelt Kurzschlüsse.
2. **Trend** – folgt dem ursprünglichen Trendmodus, der eine Bestätigung durch den stochastischen Oszillator erfordert. Ein bullisches Signal
erscheint, wenn der schnelle SMA über dem langsamen SMA bleibt, die Kerze höher schließt als sie öffnete und der stochastische %K-Wert
liegt bei oder unter 30. Ein bärisches Signal erfordert schnell < langsam, einen bärischen Kerzenkörper und stochastischen %K bei oder über 70.

Signale werden nur an fertigen Kerzen ausgewertet. Immer wenn ein neues Signal eintrifft, während Gegenpositionen noch offen sind, wird die
Die Strategie liquidiert zunächst den gegnerischen Korb und verarbeitet erst dann neue Einträge, um an der Richtung des Korbs ausgerichtet zu bleiben
aktuelles Signal.

## Positionsskalierung
Die Strategie reproduziert die Martingallogik MQL:

- Die erste Bestellung verwendet `InitialVolume` Lose.
- Jeder weitere Mittelungsauftrag multipliziert das vorherige Volumen mit `Multiplier` (Werte ≤ 1 deaktivieren das Volumenwachstum).
- Eine neue Durchschnittsorder für die aktive Richtung ist erst zulässig, nachdem sich der Markt um `LayerDistancePips` Pips entfernt hat
vom besten Einstiegspreis des aktuellen Warenkorbs (niedrigster Long-Fill oder höchster Short-Fill).
- Die Volumina werden unter Verwendung der Instrumentengrenzen `VolumeStep`, `VolumeMin` und `VolumeMax` normalisiert, sofern verfügbar.

## Risikomanagement
Für jeden Richtungskorb verfolgt die Strategie einen gemeinsamen Breakeven-Preis (volumengewichteter Durchschnitt aller Füllungen):

- `TakeProfitPips` definiert den Abstand zwischen dem durchschnittlichen Einstiegspreis und einem Korb-Take-Profit. Lange Körbe werden ausgegeben, wenn die
Kerzenhoch erreicht dieses Niveau, Short-Körbe, wenn das Kerzentief dieses Niveau erreicht. Auf 0 setzen, um Take-Profit-Ziele zu deaktivieren.
- `StopLossPips` spiegelt das Verhalten für Schutzausgänge wider. Long-Körbe schließen, wenn das Kerzentief unter den Stop fällt,
kurze Körbe, wenn das Kerzenhoch darüber kreuzt. Auf 0 setzen, um den Schutzstopp zu deaktivieren.

Exit-Orders werden über Market-Orders ausgeführt, wenn die nächste fertige Kerze das Erreichen des Levels bestätigt. Die
Die Strategie behält die Flags `_longExitRequested` und `_shortExitRequested` bei, um doppelte Exit-Übermittlungen während der Ausfüllungen zu vermeiden
noch ausstehend.

## Parameter
| Parameter | Typ | Standard | Beschreibung |
|-----------|------|---------|-------------|
| `SignalMode` | Aufzählung (`CrossMa`, `Trend`) | `CrossMa` | Wählt die Signal-Engine aus (Contrarian Crossover oder Trend mit stochastischem Filter). |
| `CandleType` | `DataType` | 30-minütiger Zeitrahmen | Primäre Kerzenserie, die für Berechnungen und Signalgenerierung verwendet wird. |
| `InitialVolume` | dezimal | `0.01` | Basisauftragsgröße in Lots für die erste Eingabe eines Warenkorbs. |
| `Multiplier` | dezimal | `2` | Der Volumenmultiplikator wird auf jeden weiteren Mittelungsauftrag angewendet. |
| `LayerDistancePips` | dezimal | `200` | Mindest-Pip-Abstand vom besten Eintrag, bevor eine weitere Order in die gleiche Richtung hinzugefügt wird. |
| `FastPeriod` | int | `14` | Periode des schnellen einfachen gleitenden Durchschnitts. |
| `SlowPeriod` | int | `28` | Periode des langsamen einfachen gleitenden Durchschnitts (muss größer als `FastPeriod` sein). |
| `StochasticKPeriod` | int | `10` | Lookback-Länge für die %K-Linie des stochastischen Oszillators. |
| `StochasticDPeriod` | int | `3` | Glättungslänge für die stochastische %D-Linie. |
| `StochasticSlowing` | int | `3` | Zusätzliche Glättung auf %K vor der %D-Berechnung angewendet. |
| `TakeProfitPips` | dezimal | `500` | Abstand in Pips vom durchschnittlichen Eintrag, bei dem der Korb-Take-Profit platziert wird. Zum Deaktivieren auf 0 setzen. |
| `StopLossPips` | dezimal | `0` | Schutzstoppabstand in Pips. Stellen Sie 0 ein, um den harten Stopp zu deaktivieren. |

## Implementierungshinweise
- Die Pip-Größe wird aus den Instrumenten `PriceStep` und `Decimals` abgeleitet und entspricht dem MetaTrader-Begriff von „Punkt“ (z. B.
0,0001 für 5-stellige FX-Kurse).
- Die Positionsverfolgung verwendet zwei Listen von `PositionEntry`-Objekten, um die Ticketabrechnung von MetaTrader widerzuspiegeln. Einträge sind
reduzierter FIFO-Stil, wenn gegensätzliche Trades einen Teil eines Korbs schließen.
- Alle Indikatorberechnungen basieren auf der High-Level-Bindung API (`SubscribeCandles().BindEx(...)`) von StockSharp. Keine manuellen Anrufe
bis `GetValue` sind erforderlich und Indikatoren werden niemals in `Strategy.Indicators` eingefügt.
- Die Strategie ruft `StartProtection()` beim Start auf und ermöglicht es StockSharp, globale Risikokontrollmodule (Break-Even,
Margenkontrollen usw.).
- Da StockSharp Positionen netto nach Richtung konsolidiert, werden entgegengesetzte Positionen vollständig geschlossen, bevor neue Einträge erfolgen
ausgewertet. Dadurch bleibt die Implementierung deterministisch und eng am ursprünglichen EA-Verhalten ausgerichtet.

## Dateien
- `CS/SmartTrendFollowerStrategy.cs` – C#-Implementierung der Strategie unter Verwendung der StockSharp-Hochebene API.
