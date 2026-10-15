# Blau Ergodic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den Expert Advisor **Exp_BlauErgodic** von MQL5 auf StockSharp. Sie rekonstruiert den Blau Ergodic-Oszillator durch
dreifaches Glätten des Momentums und seines absoluten Wertes mit EMA-Filtern, erzeugt einen normalisierten Oszillator und eine Signallinie, und
bietet drei verschiedene Signalmodi, die den ursprünglichen EA widerspiegeln.

Die Standardkonfiguration wertet abgeschlossene 4-Stunden-Kerzen aus. Sie können den angewendeten Preis (Schluss, Eröffnung, hoch/tief-basierte
Durchschnitte), jede Glättungstiefe und den Balkenindex (`SignalBar`) ändern, der zum Lesen von Signalen verwendet wird. Trades werden über die
`Volume`-Eigenschaft der Strategie dimensioniert; Long/Short-Einstiege oder -Ausstiege können individuell durch boolesche Parameter deaktiviert werden. Schutz-Stop-Loss- und
Take-Profit-Niveaus werden in Punkten definiert und über `Security.PriceStep` in absolute Preise umgerechnet.

## Signalmodi

- **Breakdown** – reagiert auf das Kreuzen der Nulllinie durch den Oszillator. Longs öffnen bei negativen-zu-positiven Flips und Shorts bei
  positiven-zu-negativen Flips. Positionen werden geschlossen, wenn der Oszillator auf der gegenüberliegenden Seite von null verbleibt.
- **Twist** – sucht nach Steigungsumkehrungen. Ein Long-Setup erscheint, wenn der Oszillator auf dem vorherigen Balken fiel, aber auf dem
  neuesten Balken steigt; ein Short-Setup erfordert das umgekehrte Muster.
- **CloudTwist** – überwacht das Kreuzen der Signallinie durch den Oszillator. Longs werden ausgelöst, wenn der Oszillator durch die Signalwolke steigt,
  und Shorts wenn er wieder darunter fällt.

Alle Modi lesen Indikatorwerte vom Balken, der durch `SignalBar` angegeben wird (Standard `1`, also der letzte abgeschlossene Balken) und stützen sich auf
ältere Werte zur Bestätigung. Setzen Sie `SignalBar` auf mindestens `1`, da die Konvertierung nur abgeschlossene Kerzen verarbeitet.

## Einstiegs- und Ausstiegsregeln

- **Long-Einstiege:** aktiviert wenn `AllowBuyEntry` wahr ist, keine bestehende Long-Position vorhanden ist (`Position <= 0`), und der aktive Modus
  eine Kaufbedingung generiert. Die Strategie kehrt jede Short-Exposition um, indem sie `Volume + |Position|` kauft.
- **Short-Einstiege:** aktiviert wenn `AllowSellEntry` wahr ist, keine bestehende Short-Position vorhanden ist (`Position >= 0`), und der aktive
  Modus eine Verkaufsbedingung ausgibt. Sie deckt jede Long-Exposition ab, bevor der Short etabliert wird.
- **Long-Ausstiege:** ausgelöst durch die moduspezifische Bedingung, oder wenn `StopLossPoints` / `TakeProfitPoints` erreicht werden. Erzwungene
  Ausstiege umgehen den `AllowBuyExit`-Flag, damit Schutz-Stops immer eingehalten werden.
- **Short-Ausstiege:** analog zur Long-Ausstiegslogik mit `AllowSellExit` und Stop-Niveaus für Short-Trades.

## Parameter

- `CandleType` – Zeitrahmen für Kerzenabonnements (Standard 4-Stunden-Kerzen).
- `Mode` – eines von `Breakdown`, `Twist`, oder `CloudTwist`.
- `MomentumLength` – Rückblick für die rohe Momentumdifferenz.
- `First/Second/ThirdSmoothingLength` – EMA-Tiefen für die kaskadierten Momentum-Filter.
- `SignalSmoothingLength` – EMA-Tiefe für die Signallinie.
- `SignalBar` – Index des abgeschlossenen Balkens zum Lesen von Signalen (Minimum `1`).
- `AppliedPrices` – Preisquelle, die den Oszillator speist (Schluss, Eröffnung, Median, Typisch, Gewichtet, etc.).
- `AllowBuyEntry`, `AllowSellEntry`, `AllowBuyExit`, `AllowSellExit` – bestimmte Operationen aktivieren oder deaktivieren.
- `StopLossPoints`, `TakeProfitPoints` – Schutzabstände in Punkten (konvertiert über `Security.PriceStep`).

Die Konvertierung behält das Verhalten des MQL5-Experten bei und nutzt die StockSharp-High-Level-API (`SubscribeCandles`,
`Bind`) unter Einhaltung der StockSharp-Strategie-Konventionen mit Tabulatoreinrückung und englischen Kommentaren.
