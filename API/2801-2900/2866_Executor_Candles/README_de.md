# Executor Candles-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine direkte Konvertierung des MetaTrader-Experten "Executor Candles". Sie reagiert auf eine umfangreiche Sammlung bullischer und bärischer Kerzenumkehrmuster und kann optional Trades mit einer Trendkerze aus einem höheren Zeitrahmen bestätigen. Die gesamte Trade-Management-Logik – Stops, Take-Profits und Trailing-Stops – spiegelt das Verhalten des ursprünglichen Experten, gemessen in Pips (Preisschritte), wider.

## Funktionsweise

- **Trendfilter**: Wenn `UseTrendFilter` aktiviert ist, beobachtet die Strategie die zuletzt abgeschlossene Kerze von `TrendCandleType`. Long-Setups sind nur erlaubt, wenn diese Kerze bullisch schloss, während Short-Setups einen bärischen Schluss erfordern. Mit deaktiviertem Filter (Standard) wird nur Musterlogik verwendet.
- **Long-Muster**: Hammer, bullische Umarmung, Durchdringungslinie, Morgenstern und Morgenstern-Doji-Strukturen aus den letzten drei abgeschlossenen Handelskerzen.
- **Short-Muster**: Erhängter Mann, bärische Umarmung, dunkle Wolkendecke, Abendstern und Abendstern-Doji-Bestätigungen.
- **Trade-Management**:
  - Separate Stop-Loss- und Take-Profit-Abstände für Long- und Short-Positionen ausgedrückt in Pips (`StopLossBuyPips`, `TakeProfitBuyPips`, `StopLossSellPips`, `TakeProfitSellPips`).
  - Optionale Trailing-Stops für beide Richtungen kontrolliert durch `TrailingStopBuyPips`, `TrailingStopSellPips` und die minimale Verschiebung `TrailingStepPips`. Eine Trailing-Aktualisierung wird erst vorgenommen, nachdem der Preis um die Stop-Distanz plus den Trailing-Schritt vorgerückt ist, was die MetaTrader-Logik repliziert.
  - Aufträge werden mit `OrderVolume` Lots platziert und die aktuelle Position wird vollständig mit Marktaufträgen umgekehrt, wenn eine Ausstiegsbedingung ausgelöst wird.

Die Strategie abonniert den konfigurierten `CandleType` für Handelssignale und bei Bedarf `TrendCandleType` für die Bestätigungskerze. Sie hält einen internen Puffer der letzten drei abgeschlossenen Handelskerzen, um die Mehr-Balken-Muster auszuwerten, ohne lange Historien zu speichern.

## Parameter

- `CandleType` – Zeitrahmen zur Erkennung der Kerzenmuster.
- `TrendCandleType` – Kerze aus höherem Zeitrahmen, wenn der Trendfilter aktiv ist.
- `OrderVolume` – Auftragsgröße für Marktein- und -ausstiege.
- `StopLossBuyPips`, `TakeProfitBuyPips`, `TrailingStopBuyPips` – Risikokontrollen für Long-Positionen.
- `StopLossSellPips`, `TakeProfitSellPips`, `TrailingStopSellPips` – Risikokontrollen für Short-Positionen.
- `TrailingStepPips` – minimale günstige Bewegung, bevor der Trailing-Stop angepasst wird.
- `UseTrendFilter` – aktiviert oder deaktiviert die Bestätigung aus dem höheren Zeitrahmen.

## Hinweise

- Alle Pip-basierten Abstände werden mit dem `PriceStep` des Instruments multipliziert. Stellen Sie sicher, dass er für genaue Risikoniveaus korrekt konfiguriert ist.
- Die Einstiegsprüfungen werden bei jeder abgeschlossenen Kerze ausgeführt; Live-Ticks aktualisieren nur den aktuellsten Balken, ohne den Entscheidungsfluss zu ändern.
- Die Strategie gibt nur Marktaufträge aus und erwartet, dass die Ausführung sofort erfolgt, wie in der MetaTrader-Version.
