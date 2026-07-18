# JFatl Candle MMRec-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie recreiert das Verhalten des ursprünglichen **Exp_JFatlCandle_MMRec.mq5** Expert Advisors innerhalb des StockSharp-Frameworks.
Sie analysiert die Farbwechsel des JFatl-Kerzenfilters und kombiniert diese mit einem adaptiven Geldmanagement-Block,
der die Handelsgröße nach einer konfigurierbaren Anzahl kürzlicher Verluste reduziert.

## Handelsidee

* Synthetische Kerzen werden durch Filtern der klassischen OHLC-Werte mit dem Fast Adaptive Trend Line (FATL)-Kernel erstellt.
  Die Implementierung verwendet die originale 39-Tap-Koeffiziententabelle gefolgt von einer exponentiellen Glättungsstufe, um
  den in MetaTrader verwendeten Jurik Moving Average zu approximieren.
* Erkennt Farbübergänge des synthetischen Kerzenkörpers:
  * Farbe **2** (bullisch) bedeutet, dass der gefilterte Schlusskurs über dem gefilterten Eröffnungskurs liegt;
  * Farbe **0** (bärisch) bedeutet, dass der gefilterte Schlusskurs unter dem gefilterten Eröffnungskurs liegt;
  * Farbe **1** markiert einen neutralen Kerzenkörper.
* Eine bullische Farbe auf der Kerze, die `SignalBar + 1` Perioden alt ist, zwingt die Strategie, alle Shorts zu schließen und sich auf
  einen neuen Long-Einstieg vorzubereiten, wenn die Kerze `SignalBar` Perioden alt nicht mehr bullisch ist.
* Eine bärische Farbe, die auf dieselbe Weise beobachtet wird, schließt Longs und ermöglicht einen Short-Einstieg, wenn die neuere Kerze nicht mehr bärisch ist.
* Long- und Short-Positionen werden durch die MMRecounter-Logik dimensioniert. Wenn die letzten `TotalTrigger` Trades der
  entsprechenden Richtung mindestens `LossTrigger` negative Ergebnisse enthalten, wechselt die Strategie zur reduzierten Positionsgröße.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `CandleType` | Zeitrahmen der Kerzen, die in den FATL-Filter eingespeist werden (Standard: 12 Stunden).
| `SignalBar` | Anzahl der abgeschlossenen Balken, die beim Lesen des Farbpuffers zurückgeblickt werden. `0` bedeutet die aktuelle abgeschlossene Kerze zu verwenden, `1` reproduziert die MT5-Standardwerte.
| `SmoothingLength` | Exponentielle Glättungslänge, die nach dem FATL-Kernel angewendet wird, um Jurik-Glättung zu emulieren.
| `NormalVolume` | Standard-Positionsgröße, wenn die jüngste Erfolgsbilanz gesund ist.
| `ReducedVolume` | Positionsgröße, die angewendet wird, nachdem der MMRecounter zu viele Verluste erkennt.
| `BuyTotalTrigger` / `SellTotalTrigger` | Anzahl der historischen Trades (pro Richtung), die vom MMRecounter untersucht werden.
| `BuyLossTrigger` / `SellLossTrigger` | Mindestanzahl von Verlusten innerhalb des untersuchten Fensters, die die reduzierte Positionsgröße erzwingen.
| `EnableBuyEntries` / `EnableSellEntries` | Öffnen von Long/Short-Positionen erlauben.
| `EnableBuyExits` / `EnableSellExits` | Schließen von Long/Short-Positionen erlauben, wenn das entgegengesetzte Signal erscheint.
| `StopLossPoints` | Optionaler Schutz-Stop für beide Richtungen in Kursschritten des Wertpapiers. Auf `0` setzen zum Deaktivieren.
| `TakeProfitPoints` | Optionales Gewinnziel in Kursschritten. Auf `0` setzen zum Deaktivieren.

## Handelsregeln

1. Gefilterte OHLC-Werte berechnen und die Kerzenfarbe bei jeder abgeschlossenen Kerze bestimmen.
2. `C1` sei die Farbe der Kerze `SignalBar + 1` Perioden zuvor und `C0` die Farbe der Kerze `SignalBar` Perioden zuvor
   (für `SignalBar = 0` wird die aktuelle Kerze als `C0` und die vorherige als `C1` verwendet).
3. Wenn `C1 == 2` (bullisch):
   * jede Short-Position schließen, wenn `EnableSellExits` `true` ist;
   * eine Long-Position mit der berechneten Positionsgröße öffnen, wenn `EnableBuyEntries` `true` ist **und** `C0 != 2`.
4. Wenn `C1 == 0` (bärisch):
   * jede Long-Position schließen, wenn `EnableBuyExits` `true` ist;
   * eine Short-Position öffnen, wenn `EnableSellEntries` `true` ist **und** `C0 != 0`.
5. Positionen können auch durch Stop-Loss- oder Take-Profit-Grenzen geschlossen werden, wenn die Kerzenspanne das konfigurierte Niveau berührt.

## Geldmanagement

Die Strategie speichert den Gewinn jedes abgeschlossenen Long- und Short-Trades separat. Wenn ein neuer Einstieg erwogen wird, scannt sie
bis zu `TotalTrigger` frühere Trades dieser Richtung. Wenn mindestens `LossTrigger` Trades innerhalb dieses Fensters mit einem negativen
Ergebnis endeten, wird das reduzierte Volumen verwendet; andernfalls wird das normale Volumen gehandelt.

## Hinweise

* Die preisschrittbasierte Stop-Loss- und Take-Profit-Logik basiert auf dem `Security.PriceStep`-Wert. Wenn das Instrument diesen nicht bereitstellt,
  wird ein Schritt von `1` angenommen.
* Der FATL-Filter benötigt mindestens 39 historische Kerzen, bevor er betriebsbereit ist. Es werden keine Trades generiert, bis
  genügend Daten akkumuliert sind.
* Die Strategie hält eine kompakte Handelshistorie für den MMRecounter-Block; sobald die Historie 100 Einträge überschreitet, werden die ältesten Datensätze
  automatisch verworfen.
