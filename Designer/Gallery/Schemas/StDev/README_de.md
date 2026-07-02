# Beschreibung der StDevStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Strategieübersicht

Die „StDevStrategy" ist für den [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) konzipiert, um statistische Volatilitätsmuster mithilfe des Standard Deviation-Indikators zu nutzen. Die Strategie ist darauf ausgelegt, potenzielle Handelsmöglichkeiten auf Basis von Abweichungen vom Durchschnittspreis zu identifizieren, die auf überkaufte oder überverkaufte Bedingungen hinweisen.

![schema](schema.png)

## Strategiedetails

### Komponenten

- **Standard Deviation-Indikatoren**: Nutzt mehrere Längen, um kurzfristige und langfristige Volatilität zu erfassen.
  - **Std Dev 20**: Misst die Volatilität über [20 Perioden](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html).
  - **Lowest 15 und Highest 15**: Verfolgen die niedrigsten und höchsten Werte über 15 Perioden, um Ausbruchsbedingungen zu erkennen.
  - **Lowest 50**: Erfasst langfristige Kurstiefs zur Beurteilung erweiterter Marktbedingungen.

### Trade-Ausführung

- **Ordertyp**: Führt Trades mit [Marktorders](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) aus, um eine schnelle Reaktion auf Signaländerungen zu gewährleisten.
- **Ein- und Ausstieg**:
  - **Kauf**: Wird ausgelöst, wenn die Kursentwicklung eine Erholung von überverkauften Bedingungen nahelegt.
  - **Verkauf**: Wird eingeleitet, wenn die Kursentwicklung auf einen möglichen Rückgang von überkauften Bedingungen hindeutet.
- **Positionsmanagement**: Setzt eine dynamische Positionsgrößenstrategie ein, die sich an Marktvolatilität und Risikoparametern orientiert.

### Risikomanagement

- **Stop Loss und Take Profit**:
  - [Stop Loss](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) wird bei 1% unterhalb des Einstiegs gesetzt, um das Risiko zu minimieren.
  - [Take Profit](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) liegt bei 2% und erfasst potenzielle Kursanstiege, während Gewinne gesichert werden.

## Implementierungsdetails

- **Plattform**: Implementiert auf der StockSharp-Plattform unter Nutzung ihrer umfassenden Tools für Echtzeit-Datenanalyse und Ordermanagement.
- **Technische Indikatoren**: Integriert mehrere Instanzen von Standard Deviation zusammen mit der Verfolgung von Höchst- und Tiefstkursen, um die Handelsgenauigkeit zu verbessern.

## Fazit

Die „StDevStrategy" ist auf Trader zugeschnitten, die technische Analyse bevorzugen und sich auf die Erfassung volatilitätsgetriebener Kursbewegungen konzentrieren. Sie bietet einen strukturierten Handelsansatz durch den Einsatz fortgeschrittener Indikatoren zur effektiven Verwaltung von Ein- und Ausstiegspunkten.
