# DynamicRS_C-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader-Expert-Advisor **Exp_DynamicRS_C** unter Verwendung der hochrangigen StockSharp-API. Sie wertet die Farbübergänge des benutzerdefinierten DynamicRS_C-Indikators aus, um dynamische Unterstützung und Resistance zu erkennen. Wenn die Linie magenta wird (Farbindex `0`), bevorzugt sie bullische Setups; wenn sie blau-violett wird (Farbindex `2`), bevorzugt sie bärische Setups. Der StockSharp-Port behält das gleiche Signal-Timing, Erlaubnisflags und Stop/Take-Struktur wie der Quell-Robot bei.

## Details

- **Einstiegskriterien**:
  - **Long**: Die von `SignalBar` ausgewählte abgeschlossene Kerze ändert die Indikatorfarbe von irgendetwas außer `0` auf `0`. Die Strategie schließt optional einen vorhandenen Short vor dem Einstieg, repliziert das ursprüngliche `SellPosClose`-Gate, und öffnet dann einen Long, wenn `AllowBuyEntry` aktiviert ist.
  - **Short**: Die ausgewertete Kerze ändert die Indikatorfarbe von irgendetwas außer `2` auf `2`. Die Strategie schließt optional einen vorhandenen Long (`AllowBuyExit`) und öffnet dann einen Short, wenn `AllowSellEntry` aktiviert ist.
- **Long/Short**: Handelt beide Richtungen mit unabhängigen Schaltern für Einstiege und Ausstiege.
- **Ausstiegskriterien**:
  - Long-Positionen schließen, wenn ein Short-Signal erscheint und `AllowBuyExit` wahr ist, oder wenn Stop-Loss-/Take-Profit-Limits getroffen werden.
  - Short-Positionen schließen, wenn ein Long-Signal erscheint und `AllowSellExit` wahr ist, oder wenn die Risiko-Limits auslösen.
- **Stops**: `StopLossPoints` und `TakeProfitPoints` sind absolute Preisoffsets vom Einstiegspreis. Einen Wert auf null setzen deaktiviert diesen Schutz.
- **Filter**:
  - `SignalBar` bestimmt, wie viele vollständig geschlossene Kerzen zurück auf einen Farbwechsel untersucht werden, entsprechend der ursprünglichen Buffer-Suche (`CopyBuffer(..., SignalBar, 2)`).
  - `CandleType` wählt den Zeitrahmen für Indikator und Handelslogik (Standard: 4-Stunden-Kerzen, entsprechend dem EA).

## Parameter

- `CandleType` – Von der Strategie verarbeitete Kerzenserie.
- `Length` – Rückblicktiefe des DynamicRS_C-Indikators zum Vergleich von Hochs/Tiefs (`Length` in MQL).
- `SignalBar` – Anzahl vollständig geschlossener Kerzen zurück für die Signalauswertung (äquivalent zum EA-Eingang `SignalBar`).
- `AllowBuyEntry` / `AllowSellEntry` – Erlaubt das Öffnen von Long-/Short-Positionen bei ihren jeweiligen Signalen.
- `AllowBuyExit` / `AllowSellExit` – Erlaubt das Schließen bestehender Long-/Short-Positionen wenn das entgegengesetzte Signal erscheint.
- `StopLossPoints` – Absoluter Verlustabstand vom Einstiegspreis. Wenn positiv, schließt es Longs unterhalb und Shorts oberhalb des Einstiegs.
- `TakeProfitPoints` – Absoluter Gewinnabstand vom Einstiegspreis. Wenn positiv, schließt es Longs oberhalb und Shorts unterhalb des Einstiegs.
- `Volume` – Basis-Ordergröße aus `Strategy.Volume`. Zusätzliche Menge wird automatisch hinzugefügt, um entgegengesetzte Positionen zu schließen, wenn das Signal eine Umkehr anfordert.

## Indikatorlogik

Der enthaltene `DynamicRsCIndicator` reproduziert das Farb-Buffer-Verhalten des MetaTrader-Skripts:

- Er verfolgt die neuesten Hochs und Tiefs über das konfigurierte `Length`-Fenster und den unmittelbar vorherigen Bar.
- Wenn ein lokales Hoch niedriger als das vorherige Hoch und das Hoch vor `Length` Bars ist, und auch unterhalb des vorherigen Indikatorwerts liegt, schaltet der Buffer auf Farbe `0` (magenta) und der Wert springt auf dieses Hoch.
- Wenn ein lokales Tief höher als das vorherige Tief und das Tief vor `Length` Bars ist, und über dem vorherigen Indikatorwert liegt, schaltet der Buffer auf Farbe `2` (blau-violett) und der Wert springt auf dieses Tief.
- Andernfalls behält der Indikator seinen vorherigen Wert. Neutrale Farbe `1` fungiert als Brücke zwischen Trending-Zuständen genau wie im ursprünglichen Algorithmus.

Durch die Bindung dieses Indikators über `BindEx` erhält die Strategie sowohl den numerischen Wert als auch den diskreten Farbindex, was sicherstellt, dass die Signalauswertung und das Trade-Timing dem Verhalten des Quell-Experten entsprechen.
