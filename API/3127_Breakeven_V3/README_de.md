# Breakeven V3 Manager
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Der Breakeven V3 Manager ist eine Konvertierung des MetaTrader 5 Expertenberaters `Breakeven v3 (barabashkakvn's edition)`.
Das ursprüngliche Skript eröffnet keine Trades. Stattdessen berechnet es kontinuierlich das Portfolio-Break-Even-Level für das
ausgewählte Symbol und verschiebt Schutzorders (Stop-Loss oder Take-Profit) für jede offene Long- und Short-Position,
sodass das gesamte Buch rund um diesen Break-Even-Preis mit einem optionalen Puffer geschlossen wird.

## Strategielogik
* **Break-Even-Rekonstruktion** – jedes Mal, wenn ein Trade ausgeführt wird oder neue Kurse eintreffen, rekonstruiert die Strategie den gewichteten
  durchschnittlichen Eröffnungspreis für Long- und Short-Exposition separat. Sie berücksichtigt die positions-bezogenen Provisionen, die StockSharp
  in den `MyTrade`-Objekten meldet, um die MQL-Implementierung widerzuspiegeln.
* **Zielpreisberechnung** – der Break-Even-Preis wird um `Delta (points)` MetaTrader-Punkte verschoben. Die Verschiebung wird
  addiert, wenn die Nettoposition long ist, und subtrahiert, wenn sie short ist, und repliziert damit den ursprünglichen "Delta"-Parameter.
* **Schutzorder-Platzierung** –
  * Wenn die Nettoposition long ist, wird ein **Sell-Limit** Take-Profit für das gesamte Long-Volumen und ein **Buy-Stop**
    Stop-Loss an das aggregierte Short-Volumen am selben Preis angehängt.
  * Wenn die Nettoposition short ist, wird ein **Buy-Limit** Take-Profit für das gesamte Short-Volumen und ein **Sell-Stop**
    Stop-Loss für etwaige Long-Absicherungen platziert.
  * Wenn beide Seiten flat sind, werden alle Schutzorders storniert.
* **Kursüberwachung und Diagnose** – die Strategie abonniert Level1-Updates. Das neueste Bid/Ask wird verwendet, um
  Abstands-zum-Ziel-Statistiken und einen geschätzten schwebenden Gewinn zu berechnen. Wenn `Enable Logging` true ist, werden diese Werte
  in das Strategie-Log geschrieben, um die On-Chart-Kommentare der MQL-Version zu emulieren.

## Parameter
* **Delta (points)** – Offset, der auf den berechneten Break-Even-Preis angewendet wird. Der Wert wird in MetaTrader-Punkten ausgedrückt,
  d.h. ein Zehntel Pip bei fünfstelligen FX-Symbolen. Standard: `100`.
* **Enable Logging** – schaltet die detaillierte Log-Ausgabe um, die das aktuelle Break-Even-Level, den Abstand zum Ziel und
  den schwebenden PnL beschreibt. Standard: `true`.

## Verwendungshinweise
* Die Strategie ist ein Trade-Manager. Sie sollte auf einer bestehenden Strategie oder manuellen Position aufgesetzt werden. Sie wird
  selbst keine Market-Orders öffnen.
* Beim Start inspiziert der Code das Portfolio und rekonstruiert ein einzelnes synthetisches Lot für jede Seite der Position unter Verwendung
  des von StockSharp gemeldeten Durchschnittspreises. Für beste Genauigkeit die Strategie immer laufen lassen, wenn neue Trades eröffnet werden.
* Swap-Gebühren sind von StockSharp nicht verfügbar, daher wird beim Neuaufbau des Break-Even-Preises nur die Provisionsinfo berücksichtigt.
  Falls der Broker Overnight-Swaps anwendet, müssen diese manuell behandelt werden.
* Das Skript setzt voraus, dass das Konto Hedging (gleichzeitige Long- und Short-Positionen) erlaubt. Wenn der Broker Positionen nettet,
  reduzieren sich die Long- und Short-Aggregate genau wie in MetaTrader auf eine einzelne Nettoexposition.
* Es gibt keine Python-Version dieses Ports. Nur die C#-Implementierung wird bereitgestellt.
