# Beschreibung der Bullish8020-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Strategieübersicht

Die „Bullish8020"-Strategie ist für den [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) entwickelt, um spezifische bullische Candlestick-Muster mit hoher Präzision zu nutzen. Diese Strategie zielt darauf ab, Marktgelegenheiten zu identifizieren, bei denen die bullische Stimmung stark ist, indem eine einzigartige Musteranalyse in Kombination mit Volumen und Kursaktion eingesetzt wird.

![schema](schema.png)

## Strategiedetails

### Mustererkennung: Bullish8020

- **Beschreibung**: Diese Strategie erkennt ein bullisches Szenario, bei dem der [Eröffnungskurs](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) unter dem Schlusskurs liegt und die Körpergröße das Vierfache der Summe beider Dochte beträgt, was auf starken Kaufdruck hinweist.
- **Candlestick-Muster**: 'Bullish8020' überprüft, ob `(O < C) && (B >= 4*(BS+TS))`, wobei `O` der Eröffnungskurs, `C` der Schlusskurs, `B` die Körpergröße, `BS` der untere Docht und `TS` der obere Docht ist.

### Handelsausführung

- **Ordertyp**: Market-[Order](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)
- **Einstieg**: Kauft, wenn das 'Bullish8020'-[Muster](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) bestätigt wird und eine potenzielle Aufwärtsbewegung signalisiert.
- **Ausstiegsstrategie**:
  - **Stop Loss**: Auf 0.5% unter dem Einstiegspunkt gesetzt, um potenzielle Verluste zu begrenzen.
  - **Marktbedingungen**: Trades werden zu aktuellen Marktpreisen ausgeführt, um eine schnelle Reaktion auf die Mustererkennung zu gewährleisten.

### Risikomanagement

- **Positionsgrößenbestimmung**: Die Strategie verwendet dynamische Größenbestimmung basierend auf aktuellen Marktbedingungen und dem Risikoprofil des Traders.
- **Stop-Loss-Strategie**: Ein strikter [Stop-Loss](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) wird implementiert, um sich gegen unvorhergesehene Marktumkehrungen zu schützen.

## Implementierungsdetails

- **Plattform**: Auf der StockSharp-Plattform implementiert, die ihre leistungsstarke API für Echtzeit-Datenverarbeitung und Orderausführung nutzt.
- **Verwendete Indikatoren**: Kombiniert Candlestick-Mustererkennung mit Volumenanalyse, um die Genauigkeit der Handelssignale zu verbessern.

## Fazit

Die „Bullish8020"-Strategie bietet Tradern ein robustes Werkzeug zur Ausnutzung spezifischer bullischer Muster im Markt. Sie ist darauf ausgelegt, Gewinne aus starken bullischen Setups zu maximieren, während strenge Risikomanagementprotokolle zum Schutz der Investitionen eingesetzt werden.
