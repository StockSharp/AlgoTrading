# Ichimoku-Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Ichimoku Oscillator**-Strategie verwendet einen benutzerdefinierten Oszillator, der vom Ichimoku-Indikator abgeleitet wird. Der Oszillator ist definiert als die Differenz zwischen der Lagging Line und Senkou Span B minus der Differenz zwischen Tenkan-sen und Kijun-sen. Der resultierende Wert wird mit einem Jurik Moving Average geglättet.

Die Strategie eröffnet Positionen, wenn dieser geglättete Oszillator die Richtung wechselt und seinen vorherigen Wert kreuzt, um aufkommende Trends zu erfassen.

## Funktionsweise
- **Long-Einstieg**: Der Oszillator steigt und der aktuelle Wert kreuzt über den vorherigen Wert. Jede Short-Position wird vor dem Long-Einstieg geschlossen.
- **Short-Einstieg**: Der Oszillator fällt und der aktuelle Wert kreuzt unter den vorherigen Wert. Jede Long-Position wird vor dem Short-Einstieg geschlossen.
- Optionaler Stop-Loss und Take-Profit in Prozent werden für das Risikomanagement angewendet.

## Parameter
- **Tenkan Period** – Tenkan-sen-Periode des Ichimoku-Indikators.
- **Kijun Period** – Kijun-sen-Periode des Ichimoku-Indikators.
- **Senkou Span B Period** – Senkou-Span-B-Periode des Ichimoku-Indikators.
- **Smoothing Period** – Periode für die Jurik-Moving-Average-Glättung des Oszillators.
- **Candle Type** – Zeitrahmen für die Berechnungen.
- **Stop Loss %** – Stop-Loss in Prozent ausgedrückt.
- **Enable Stop Loss** – Aktiviert oder deaktiviert den Stop-Loss-Schutz.
- **Take Profit %** – Take-Profit in Prozent ausgedrückt.

## Indikatoren
- Ichimoku
- Jurik Moving Average

## Hinweise
Diese Strategie ist für Bildungszwecke gedacht und sollte vor dem echten Handel an historischen Daten getestet werden.
