# Surfen 3.0-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese C#-Strategie ist eine originalgetreue Portierung des MetaTrader 4-Experten **Surfing 3.0**. Es stellt die Ausbruchslogik wieder her, die einen exponentiellen gleitenden Durchschnitt (EMA)-Umschlag beobachtet, der aus Kerzenhochs und -tiefs besteht. Immer wenn der vorherige Balken innerhalb des Bandes schließt und der zuletzt geschlossene Balken dieses durchbricht, reagiert das System mit einem Richtungshandel. Die Übersetzung basiert auf der hohen Ebene API von StockSharp, Kerzenabonnements und integrierten Indikatoren anstelle von handgeschriebenen Puffern.

Der Algorithmus arbeitet ausschließlich mit fertigen Kerzen aus einer konfigurierbaren Aggregation. Es behält nur die minimale Menge an Status bei, die zum Emulieren der vom Originalcode verwendeten `iMA`- und `iClose`-Lookbacks erforderlich ist. Jede Entscheidung wird einmal pro geschlossenem Balken getroffen, was dem Bewertungsstil „geschlossener Balken“ der MQL-Implementierung entspricht.

## Indikatoren

- **Hoch EMA / Tief EMA** – Zwei exponentielle gleitende Durchschnitte, berechnet auf Kerzenhochs und -tiefs. Sie bilden einen dynamischen Umschlag, der die Ausbruchsniveaus für Long- und Short-Einstiege definiert.
- **Relative Strength Index (RSI)** – Fungiert als Trendfilter. Für Long-Positionen muss der RSI über `LongRsiThreshold` liegen, während Short-Positionen nur zulässig sind, wenn er unter `ShortRsiThreshold` liegt.

## Handelslogik

1. Abonnieren Sie Kerzen vom Typ `CandleType` und aktualisieren Sie die Indikatoren EMA und RSI für jeden fertigen Balken.
2. Speichern Sie die vorherigen Schlussbalkenwerte des Schlusskurses und der EMA Hochs/Tiefs. Diese repräsentieren `PriceClose_2`, `PriceHigh_2` und `PriceLow_2` vom ursprünglichen Experten.
3. Wenn der zuletzt geschlossene Balken (`PriceClose_1`) **über** das Hoch EMA kreuzt, während der vorherige Schlusskurs darunter oder gleich diesem Wert lag und der RSI-Filter Folgendes bestätigt:
   - Schließen Sie alle offenen Short-Positionen.
   - Eröffnen Sie eine Long-Market-Order mit einem Volumen von `OrderVolume`.
   - Berechnen Sie Stop-Loss und Take-Profit-Offsets in Instrumentenpunkten.
4. Wenn der zuletzt geschlossene Balken **unter** den Tiefstwert EMA kreuzt, während der vorherige Schlusskurs darüber oder gleich diesem Wert lag und der RSI unter dem Short-Schwellenwert liegt:
   - Schließen Sie alle offenen Long-Positionen.
   - Eröffnen Sie eine Short-Market-Order mit einem Volumen von `OrderVolume`.
   - Tragen Sie die Schutzebenen in gleichen punktuellen Abständen auf.
5. Es kann nur eine Nettoposition aktiv sein. Umkehrsignale glätten immer das bestehende Risiko, bevor sie in die entgegengesetzte Richtung eintreten.
6. Außerhalb des Handelsfensters `[TradeStartHour, TradeEndHour)` werden keine neuen Geschäfte initiiert. Sobald die Uhr `TradeEndHour` erreicht, schließt die Strategie alle verbleibenden Positionen und setzt ihren internen Verlauf zurück, wobei sie den Aufruf `closeAllPos()` in der Version MQL nachahmt.

## Risikomanagement

- **Stop-Loss / Take-Profit** – Wird in Instrumentenpunkten ausgedrückt und anhand der Wertpapierpreisstufe umgerechnet. Beide sind optional; Wenn Sie einen Abstand von `0` festlegen, wird die entsprechende Ebene deaktiviert.
- **Session Flat** – Am Ende des zulässigen Handelsfensters wird jede offene Position zum Marktwert geschlossen und die Stop/Take-Profit-Verfolgung wird gelöscht. Dies verhindert, dass Positionen über Nacht driften, genau wie es der ursprüngliche Experte mit `startHour` / `endHour` erzwungen hat.

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|---------|
| `OrderVolume` | Handelsvolumen, das für jede Marktorder verwendet wird. | `1` |
| `TakeProfitPoints` | Nehmen Sie die Gewinndistanz, ausgedrückt in Instrumentenpunkten. | `80` |
| `StopLossPoints` | Stop-Loss-Distanz, ausgedrückt in Instrumentenpunkten. | `50` |
| `MaPeriod` | Länge des EMA, angewendet auf Hochs und Tiefs. | `50` |
| `RsiPeriod` | Zeitraum des Filters RSI. | `10` |
| `LongRsiThreshold` | Mindestwert RSI erforderlich, um lange Einträge zuzulassen. | `40` |
| `ShortRsiThreshold` | Maximal zulässiger RSI-Wert für die Eingabe von Short-Positionen. | `65` |
| `TradeStartHour` | Stunde (Börsenzeit), ab der neue Geschäfte erlaubt sind. | `8` |
| `TradeEndHour` | Stunde (exklusiv), nach der die Positionen geschlossen werden und keine neuen Trades beginnen. | `18` |
| `CandleType` | Für alle Berechnungen verwendete Kerzenaggregation (Standard: 15-Minuten-Kerzen). | `15m` |

## Notizen

- Signale werden ausschließlich an fertigen Kerzen ausgewertet; Intrabar-Schwankungen werden wie in MetaTrader ignoriert.
- Die Strategie setzt ihren EMA-Verlauf zurück, wenn die Handelssitzung endet, um eine Vermischung von Daten verschiedener Tage zu vermeiden.
- Auf eine Python-Übersetzung wird gemäß den Projektrichtlinien bewusst verzichtet.
