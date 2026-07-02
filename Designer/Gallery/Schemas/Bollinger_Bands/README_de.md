# Beschreibung der Bollinger-Bands-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Strategieübersicht

Die „Bollinger Bands"-Strategie ist für den [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) konzipiert und konzentriert sich auf die Nutzung der Bollinger Bands, um von Volatilitätsmustern zu profitieren. Diese Strategie erkennt Preiskreuzungen der Bänder, um Ein- und Ausstiegspunkte im Markt zu bestimmen.

![schema](schema.png)

## Strategiedetails

### Komponenten

1. **Kerzenbildung**: Verwendet einen Fünf-Minuten-Zeitrahmen zur Erzeugung von [Kerzen](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) und löst die Analyse beim Schließen jeder Kerze aus.
2. **Bollinger-Bands-Indikator**: Berechnet die oberen und unteren Bänder der [Bollinger Bands](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) mit einer Periodenlänge von 32 und einem Standardabweichungsmultiplikator von 2.0.
3. **Handelssignale**:
   - **Kaufsignal**: Ein Kaufsignal wird generiert, wenn der [Tiefstkurs](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) der Kerze das untere Bollinger Band nach unten [kreuzt](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html), was auf einen überverkauften Zustand hindeutet.
   - **Verkaufssignal**: Ein Verkaufssignal wird ausgelöst, wenn der [Höchstkurs](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) der Kerze das obere Bollinger Band nach oben [kreuzt](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html), was einen überkauften Zustand anzeigt.

### Handelsausführung

- **Ordertyp**: [Market Orders](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) werden sowohl für den Ein- als auch für den Ausstieg verwendet, um schnelle Ausführung zu gewährleisten.
- **Positionsverwaltung**: Positionen werden auf Basis der Kreuzungssignale eröffnet und entweder bei einer Kreuzung in entgegengesetzter Richtung oder auf Basis vordefinierter Stop-Loss- oder Take-Profit-Bedingungen geschlossen.

### Risikomanagement

- **Stop-Loss und Take-Profit**: Konfigurierbare Einstellungen ermöglichen feste oder prozentbasierte [Stop-Loss- und Take-Profit](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)-Niveaus für effektives Risikomanagement.
- **Geldmanagement**: Die Strategie umfasst Parameter zur Anpassung der Handelsgröße auf Basis des verfügbaren Kontosaldos und der Risikoniveaus.

## Fazit

Die „Bollinger Bands"-Strategie bietet einen systematischen Ansatz für den Handel auf Basis von Volatilität und Marktbedingungen, was sie für Trader geeignet macht, die ein robustes, automatisiertes Handelssystem innerhalb der StockSharp-Plattform suchen. Sie kombiniert technische Indikatoren mit präzisen Handelsausführungsregeln, um die Handelsleistung in verschiedenen Marktumgebungen zu verbessern.
