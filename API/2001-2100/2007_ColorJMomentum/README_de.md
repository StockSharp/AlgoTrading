# ColorJMomentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **ColorJMomentum-Strategie** handelt basierend auf Richtungsänderungen eines mit Jurik geglätteten Momentum-Indikators. Der Ansatz leitet sich vom ursprünglichen MQL5-Expertenberater `Exp_ColorJMomentum` ab und wird mithilfe der StockSharp-High-Level-API reproduziert.

## Konzept

1. Den Standard-*Momentum* der ausgewählten Preisserie berechnen.
2. Die Momentum-Werte mit dem **Jurik Moving Average (JMA)** glätten.
3. Die letzten zwei Werte des geglätteten Momentums überwachen:
   - Wenn der Indikator rückläufig war und sich nach oben dreht, wird eine **Long**-Position eröffnet.
   - Wenn der Indikator steigend war und sich nach unten dreht, wird eine **Short**-Position eröffnet.
4. Der Positionsschutz erfolgt durch optionalen Stop Loss und Take Profit in Prozentangaben.

Die Strategie liest historische Indikatorwerte nicht direkt. Stattdessen reagiert sie nur auf neue Kerzenabschlüsse und speichert vorherige Werte intern.

## Parameter

- **Momentum Length** – Periode für die Momentum-Berechnung.
- **JMA Length** – Glättungsperiode des auf Momentum angewandten Jurik Moving Average.
- **Candle Type** – Zeitrahmen für Kerzenabonnements.
- **Stop Loss %** – Prozentsatz für optionalen Stop Loss.
- **Enable Stop Loss** – ob Stop Loss aktiviert wird.
- **Take Profit %** – Prozentsatz für Take Profit.
- **Enable Long** – Long-Positionen eröffnen erlauben.
- **Enable Short** – Short-Positionen eröffnen erlauben.

Alle Parameter werden mit `StrategyParam` erstellt, damit sie in Designer optimiert werden können.

## Verwendung

1. Strategie an das gewünschte Wertpapier anhängen.
2. Parameter konfigurieren oder Standardwerte belassen (8-Perioden-Momentum und 8-Perioden-JMA auf 8‑Stunden-Kerzen).
3. Strategie ausführen. Orders werden über `BuyMarket` und `SellMarket` ausgegeben, wenn sich die Momentum-Richtung umkehrt.

## Hinweise

- Die Strategie verarbeitet nur abgeschlossene Kerzen.
- Für Indikatoren werden keine expliziten Farben gesetzt – Designer wählt sie automatisch aus.
- Der Algorithmus vermeidet jegliches LINQ oder benutzerdefinierte Sammlungen gemäß den Projektrichtlinien.
