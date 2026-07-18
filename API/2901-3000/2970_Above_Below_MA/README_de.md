# Above Below MA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Above Below MA Strategie bildet den MetaTrader-Expert-Advisor *Above Below MA (barabashkakvn's edition)* nach. Sie überwacht, wie weit aktuelle Preise im Verhältnis zu einem konfigurierbaren gleitenden Durchschnitt handeln, und erlaubt Trades nur, wenn der Preis auf der „falschen" Seite des Durchschnitts liegt — mindestens um eine definierte Distanz — während der Durchschnitt selbst in die erwartete Richtung tendiert. Die Logik wurde auf die StockSharp High-Level-API portiert und wird ausschließlich auf abgeschlossenen Kerzen ausgeführt.

## Übersicht

- **Marktregime**: Funktioniert am besten bei Instrumenten, die einen gleitenden Durchschnitt häufig retesten, bevor sie den Trend fortsetzen.
- **Instrumente**: Jedes von Ihrer StockSharp-Verbindung unterstützte Instrument. Forex-Paare profitieren am meisten, da das Originalskript die Distanz in Pips maß.
- **Zeitrahmen**: Über den Parameter *Candle Type* einstellbar (Standard: 1-Minuten-Zeitrahmen).
- **Positionsrichtung**: Sowohl Long- als auch Short-Trades werden unterstützt, aber es kann zu jedem Zeitpunkt nur eine Nettoposition bestehen.

## Strategie-Logik

1. Es wird ein gleitender Durchschnitt auf der ausgewählten Kerzenserie berechnet. Die Mittelungsmethode (SMA, EMA, SMMA, WMA), der angewandte Preis (close, open, high, low, median, typical, weighted) und die Vorwärtsverschiebung replizieren die MetaTrader-Eingaben.
2. Die in Pips ausgedrückte Mindestdistanz wird mit dem `PriceStep` des Instruments in einen tatsächlichen Preisversatz umgerechnet. Wenn der Broker keinen Preisschritt veröffentlicht, wird der Distanzfilter automatisch übersprungen.
3. Bei jeder abgeschlossenen Kerze:
   - **Long-Setup**:
     - Eröffnung und Schluss der Kerze müssen mindestens die konfigurierte Distanz unterhalb des verschobenen gleitenden Durchschnitts liegen.
     - Der gleitende Durchschnitt muss gegenüber der vorherigen Kerze steigen.
   - **Short-Setup**:
     - Eröffnung und Schluss der Kerze müssen mindestens die konfigurierte Distanz oberhalb des verschobenen gleitenden Durchschnitts liegen.
     - Der gleitende Durchschnitt muss gegenüber der vorherigen Kerze fallen.
4. Die Strategie schließt jede entgegengesetzte Position, bevor eine neue Marktorder in der Signalrichtung gesendet wird. Gleichzeitige Long/Short-Exposition ist nicht erlaubt.

Alle Handelsentscheidungen werden auf abgeschlossenen Kerzen getroffen, um wiederholte Einstiege innerhalb eines sich formierenden Balkens zu vermeiden. Orders werden über `BuyMarket` oder `SellMarket` mit dem konfigurierten Volumen ausgeführt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `MaPeriod` | Länge des gleitenden Durchschnitts. Standard 6.
| `MaShift` | Anzahl der Kerzen, um die der gleitende Durchschnitt vorwärts verschoben wird. Ein Wert von 0 verwendet den aktuellen Balken, `n` verwendet den Wert von vor `n` Balken. Standard 0.
| `MaMethod` | Typ des gleitenden Durchschnitts: `Simple`, `Exponential`, `Smoothed` oder `Weighted`. Standard `Exponential`.
| `AppliedPrice` | Preisquelle: close, open, high, low, median, typical oder weighted. Standard `Typical`.
| `MinimumDistancePips` | Erforderliche Distanz in Pips zwischen den Kerzenpreisen und dem gleitenden Durchschnitt. Wird mit `PriceStep` umgerechnet. Standard 5.
| `CandleType` | Kerzentyp, der Indikatoraktualisierungen antreibt. Standard: 1-Minuten-Zeitrahmen.
| `TradeVolume` | Ordervolumen für neue Einstiege. Standard 1.

## Zusätzliche Hinweise

- Keine Stop-Loss- oder Take-Profit-Logik ist enthalten. Das Risikomanagement muss über Portfolio-Einstellungen oder externe Module implementiert werden.
- Der Verschiebungs-Puffer des gleitenden Durchschnitts wird minimal gehalten und respektiert die „keine Sammlungen"-Richtlinie, indem nur die für die angegebene Verschiebung erforderlichen Werte gespeichert werden.
- Wenn `PriceStep` nicht verfügbar ist, kann der Mindestdistanzfilter nicht ausgewertet werden, sodass Einstiege ausschließlich von den Bedingungen des gleitenden Durchschnitts abhängen.
- Die Strategie zeichnet die Kerzenserie, den gleitenden Durchschnittsindikator und Ihre Trades im Standard-Chartbereich, wenn ein Chart-Container verfügbar ist.
