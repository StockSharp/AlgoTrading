# BADX ADX Bollinger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie reproduziert den MetaTrader-BADX-Expertenberater unter Verwendung der StockSharp High-Level-API. Sie kombiniert den **Average Directional Index (ADX)** mit **Bollinger Bands**, um Range-Bedingungen zu handeln: Wenn der ADX unter einen konfigurierbaren Schwellenwert fällt und der Preis das äußere Band berührt, faded die Strategie die Bewegung in Erwartung einer Mean Reversion. Alle Schutzorders, einschließlich Stop-Loss, Take-Profit und optionalem Trailing-Stop, werden automatisch durch `StartProtection` verwaltet.

## Funktionsweise

1. Abonniert die konfigurierte Kerzenserie und fügt sowohl einen `AverageDirectionalIndex` als auch einen `BollingerBands`-Indikator über High-Level-Bindings ein.
2. Für jede abgeschlossene Kerze erhält der Callback den ADX-Wert sowie die obere und untere Bollinger-Hülle.
3. Wenn der ADX unter `AdxLevel` liegt, gilt der Markt als trendlos:
   - Wenn der Schlusskurs unterhalb des unteren Bandes liegt und keine offene Position vorhanden ist, kauft die Strategie zum Marktpreis.
   - Wenn der Schlusskurs oberhalb des oberen Bandes liegt und keine offene Position vorhanden ist, verkauft die Strategie zum Marktpreis.
4. Das Risikomanagement wandelt Pip-Abstände in absolute Preis-Offsets um. Stop-Loss, Take-Profit und Trailing-Parameter (falls aktiviert) werden unmittelbar nach Einstiegen über den Schutzmanager angewendet.
5. Es kann immer nur eine Position aktiv sein. Ausstiege erfolgen durch Schutzorders oder Trailing-Stop-Anpassungen.

## Parameter

- **CandleType** (`DataType`): Zeitrahmen für Indikatorberechnungen. Standard: 15-Minuten-Kerzen.
- **AdxPeriod** (`int`): Mittelungsperiode für den ADX-Indikator. Standard: 30.
- **AdxLevel** (`decimal`): Maximaler ADX-Wert, der noch als Ranging-Markt gilt. Standard: 20.
- **BollingerPeriod** (`int`): Periode für den gleitenden Durchschnitt der Bollinger Bands. Standard: 10.
- **BollingerDeviation** (`decimal`): Standardabweichungs-Multiplikator für die Bollinger Bands. Standard: 1.5.
- **StopLossPips** (`decimal`): Stop-Loss-Abstand in Pips. Standard: 50.
- **TakeProfitPips** (`decimal`): Take-Profit-Abstand in Pips. Standard: 50.
- **TrailingStopPips** (`decimal`): Trailing-Stop-Abstand in Pips. Standard: 5.
- **TrailingStepPips** (`decimal`): Minimale Preisverbesserung in Pips, bevor der Trailing-Stop angepasst wird. Standard: 5.

## Verwendung

1. Die Strategie einem Wertpapier zuordnen und die Parameter nach Wunsch konfigurieren.
2. Strategie starten. Sie abonniert automatisch den benötigten Kerzenstrom, baut die Indikatoren auf und richtet Schutzorders ein.
3. Trades im Chart-Bereich überwachen: Kerzen, die Bollinger Bands und ausgeführte Orders werden visualisiert, wenn die Plattform Charting unterstützt.
4. Risikoparameter (Stop-Loss, Take-Profit, Trailing-Abstände) an die Instrument-Volatilität oder persönliche Präferenzen anpassen.

## Hinweise

- Nur abgeschlossene Kerzen werden verarbeitet, um vorzeitige Einstiege zu vermeiden.
- Die Pip-Größe wird aus dem `PriceStep` des Instruments abgeleitet; wenn das Instrument 3 oder 5 Dezimalstellen verwendet, wird der Pip um den Faktor zehn angepasst, was den ursprünglichen Expertenberater nachahmt.
- Die Strategie hält `Volume` standardmäßig auf `1`. Die `Volume`-Eigenschaft der Basisklasse anpassen, um die bevorzugte Handelsgröße zu erreichen.
- Alle Inline-Kommentare im Quellcode sind gemäß den Repository-Richtlinien auf Englisch verfasst.
