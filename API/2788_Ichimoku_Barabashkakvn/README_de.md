# Ichimoku Barabashkakvn Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie recreiert Vladimir Karputovs Ichimoku Expert Advisor (barabashkakvn-Edition) auf der StockSharp High-Level-API. Sie kombiniert den klassischen Tenkan/Kijun-Crossover mit der Bestätigung aus der Kumo-Wolke und fügt ein detailliertes Risikomanagement hinzu, das mit dem MetaTrader-Original identisch ist.

## Funktionsweise

- **Indikatorstapel** – ein einzelner Ichimoku Kinko Hyo-Indikator liefert Tenkan-sen-, Kijun-sen-, Senkou Span A- und Senkou Span B-Werte. Die Standardperioden bleiben 9/26/52.
- **Long-Einstiege** – werden ausgelöst, wenn Tenkan nach oben durch Kijun kreuzt und der Schlusskurs über Senkou Span B liegt. Die Crossover-Erkennung verwendet den vorherigen Tenkan-Wert und spiegelt die Balken-für-Balken-Logik des EA wider.
- **Short-Einstiege** – erscheinen, wenn Tenkan nach unten durch Kijun kreuzt, während der Schluss unter Senkou Span A liegt.
- **Positionsmanagement** – nur eine Nettoposition wird aufrechterhalten. Entgegengesetzte Signale schließen zuerst bestehende Trades, was den zweistufigen Umkehrablauf des Skripts reproduziert.
- **Handelsfenster** – ein optionaler Stundenfilter lässt das System nur zwischen konfigurierten Start/End-Stunden (inklusive) handeln, mit demselben Vergleich wie die MQL-Version.

## Risikomanagement

- **Direktionale Stops und Ziele** – Long- und Short-Positionen verwenden unabhängige Stop-Loss/Take-Profit-Abstände in Pips. Pips werden in Preiseinheiten unter Verwendung der Instrumentenschrittgröße mit einer 10×-Anpassung für 3- und 5-Dezimalstellen-Notierungen konvertiert, was der Punktbehandlung des EA entspricht.
- **Trailing-Stop** – jede Richtung hat ihren eigenen Trailing-Abstand plus einem gemeinsamen Trailing-Schritt. Der Stop rückt nur vor, nachdem die Bewegung `(Trailing-Abstand + Trailing-Schritt)` überschritten hat, genau wie im ursprünglichen Code.
- **Schutzausführung** – Stop-Loss- und Take-Profit-Prüfungen erfolgen bei jeder abgeschlossenen Kerze, damit sich virtuelle Schutzniveaus wie broker-verwaltete Orders aus MetaTrader verhalten.

## Parameter

- `TenkanPeriod` *(Standard 9)* – Tenkan-sen-Länge.
- `KijunPeriod` *(Standard 26)* – Kijun-sen-Länge.
- `SenkouSpanBPeriod` *(Standard 52)* – Senkou Span B-Länge.
- `CandleType` *(Standard 1-Stunden-Kerzen)* – Datenquelle für Berechnungen.
- `OrderVolume` *(Standard 1 Lot)* – Handelsgröße.
- `BuyStopLossPips` / `SellStopLossPips` *(Standard 100)* – Stop-Loss-Abstände in Pips.
- `BuyTakeProfitPips` / `SellTakeProfitPips` *(Standard 300)* – Take-Profit-Abstände in Pips.
- `BuyTrailingStopPips` / `SellTrailingStopPips` *(Standard 50)* – Trailing-Abstände in Pips.
- `TrailingStepPips` *(Standard 5)* – minimaler Gewinnzuwachs, der zum Verschieben des Trailing-Stops erforderlich ist.
- `UseTradeHours` *(Standard false)* – Sitzungsfilter aktivieren.
- `StartHour` / `EndHour` *(Standards 0/23)* – inklusive Handelsfenstergrenzen (0–23).

Diese Standardwerte entsprechen dem veröffentlichten EA. Alle Parameter werden durch `StrategyParam<T>`-Objekte bereitgestellt, sodass sie innerhalb des StockSharp Designers optimiert oder angepasst werden können, ohne den Quellcode zu berühren.
