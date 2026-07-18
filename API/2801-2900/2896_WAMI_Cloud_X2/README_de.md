# Strategie WAMI Cloud X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert das Dual-Zeitrahmen-Verhalten des originalen MetaTrader-Experten "Exp_WAMI_Cloud_X2". Sie verwendet den Warren Momentum Indicator (WAMI) auf einem höheren Zeitrahmen, um die dominierende Ausrichtung zu definieren, und eine zweite Instanz desselben Indikators auf einem niedrigeren Zeitrahmen für das Timing von Einstiegen und Ausstiegen. Die WAMI-Hauptlinie wird auf beiden Zeitrahmen gegen ihre interne Signallinie verglichen, was der Logik der ursprünglichen MQL-Implementierung entspricht.

## Konzept

- **WAMI-Konstruktion** – WAMI wird aus der ersten Differenz der Schlusskurse aufgebaut, die durch drei sequentielle gleitende Durchschnitte mit individuell auswählbaren Methoden (SMA, EMA, SMMA oder LWMA) geglättet wird. Ein vierter gleitender Durchschnitt produziert die Signallinie. Der benutzerdefinierte Indikator in der Strategie reproduziert diese Kette exakt, sodass sowohl Haupt- als auch Signallinie in einer Wert-Payload verfügbar sind.
- **Trendfilter (höherer Zeitrahmen)** – Die Standard-Sechs-Stunden-Kerzen treiben den Trend-WAMI an. Wenn die Hauptlinie über der Signallinie liegt, wird die Trendrichtung bullisch; darunter wird sie bärisch. Ein neutraler Zustand wird beibehalten, wenn beide Linien gleich sind oder der Indikator sich noch bildet.
- **Signal-Engine (niedrigerer Zeitrahmen)** – Die Standard-30-Minuten-Kerzen werden für die Suche nach Einstiegen verwendet. Für jede abgeschlossene Kerze speichert die Strategie aktuelle WAMI-Werte und wertet den durch `SignalBar` definierten letzten geschlossenen Balken aus. Kreuzungen werden erkannt, indem der neueste Wert (`SignalBar`) gegen den vorherigen (`SignalBar + 1`) verglichen wird.

## Handelsregeln

1. **Ausstiege**
   - Long-Positionen werden geschlossen, wenn der Signal-Zeitrahmen anhaltende Bärischheit zeigt (`previous.Main < previous.Signal`), falls `CloseLongOnSignal` aktiviert ist.
   - Short-Positionen werden analog geschlossen, wenn `CloseShortOnSignal` aktiviert ist.
   - Wenn der höhere Zeitrahmen die Richtung wechselt (`_trendDirection`), erzwingt das jeweilige Flag `CloseLongOnTrendFlip` oder `CloseShortOnTrendFlip` einen Ausstieg.
2. **Einstiege**
   - Short-Einstiege sind erlaubt, wenn der höhere Zeitrahmen bärisch ist und der Signal-WAMI aufwärts kreuzt (`current.Main >= current.Signal` mit `previous.Main < previous.Signal`). Dies entspricht dem ursprünglichen EA, der beim ersten Aufwärtsdurchbruch der Signallinie innerhalb eines Abwärtstrends verkauft.
   - Long-Einstiege sind die gespiegelte Bedingung, wenn der höhere Zeitrahmen bullisch ist und der Signal-WAMI abwärts kreuzt (`current.Main <= current.Signal` mit `previous.Main > previous.Signal`).
   - Einstiegs-Schalter (`EnableBuyEntries`, `EnableSellEntries`) können beide Seiten deaktivieren. Wenn eine entgegengesetzte Position offen ist, sendet die Strategie einen ausgleichenden Marktauftrag, um in einem einzigen Befehl abzuflachen und umzukehren, genau wie die MQL-Hilfsfunktionen.

## Parameter

- **Trend-WAMI** – `TrendPeriod1/2/3`, `TrendMethod1/2/3`, `TrendSignalPeriod`, `TrendSignalMethod`, `TrendCandleType`.
- **Signal-WAMI** – `SignalPeriod1/2/3`, `SignalMethod1/2/3`, `SignalSignalPeriod`, `SignalSignalMethod`, `SignalCandleType`.
- **Kontroll-Flags** – `SignalBar`, `EnableBuyEntries`, `EnableSellEntries`, `CloseLongOnTrendFlip`, `CloseShortOnTrendFlip`, `CloseLongOnSignal`, `CloseShortOnSignal`.
- **Handelsgröße** – `TradeVolume` definiert die Marktordergröße für neue Einstiege. Umkehrungen senden das entgegengesetzte Volumen plus die konfigurierte Größe.

Alle Parameter werden über `StrategyParam<T>`-Objekte bereitgestellt, sodass sie über die StockSharp-UI optimiert oder geändert werden können, genau wie es MetaTrader-Eingaben ermöglichten.

## Standardwerte

- **Trend-Zeitrahmen** – 6-Stunden-Kerzen.
- **Signal-Zeitrahmen** – 30-Minuten-Kerzen.
- **Alle gleitenden Durchschnittsmethoden** – Einfach (SMA).
- **Gleitende Durchschnittslängen** – 4 / 13 / 13 für die drei Stufen und 4 für die Signallinie auf beiden Zeitrahmen.
- **SignalBar** – 1 (letzte geschlossene Kerze verwenden).
- **TradeVolume** – 1 Kontrakt.
- **Alle Erlaubnis-Flags** – Aktiviert (true).

## Zusätzliche Hinweise

- Die Strategie setzt keine festen Stop-Loss- oder Take-Profit-Aufträge. Risikomanagement sollte bei Bedarf extern konfiguriert werden.
- Chart-Helfer zeichnen die Signal-Zeitrahmen-Kerzen, beide WAMI-Linien und die ausgeführten Trades. Der Trend-Zeitrahmen wird in einem separaten Bereich zur visuellen Bestätigung dargestellt.
- Die Implementierung vermeidet das Polling von Indikatorwerten (keine `GetValue`-Aufrufe) und bleibt bei der hochrangigen Kerzen-Abonnement-API, gemäß den Projektrichtlinien.
