# Trendfolger-Regenbogenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Trend Follower Rainbow Strategy ist eine C#-Portierung des MetaTrader 4-Expertenberaters „TrendFollowerRainbowMethodkyast773“. Die Strategie kombiniert mehrere Bestätigungsebenen, um in Richtung starker Trends zu handeln und gleichzeitig bereichsgebundene Zeiträume herauszufiltern. Es basiert auf der Ausrichtung eines Regenbogens exponentieller gleitender Durchschnitte, MACD-Momentum, Laguerre-Oszillator-Schwellenwerten, Geldflussindexwerten und einem schnellen/langsamen EMA-Crossover, um Positionen auszulösen.

## Handelslogik
1. **Handelsfenster** – Signale werden nur ausgewertet, wenn die aktuelle Schlusszeit der Kerze genau zwischen den konfigurierbaren Start- und Endstunden liegt. Dies ahmt den ursprünglichen Zeitfilter von EA nach, der die ersten und letzten Handelsstunden der Sitzung vermied.
2. **EMA Crossover-Trigger** – Ein langer Aufbau erfordert, dass der schnelle EMA (Standardlänge 4) den langsamen EMA (Standardlänge 8) überschreitet. Ein kurzer Aufbau erfordert die umgekehrte Frequenzweiche.
3. **MACD-Bestätigung** – Die MACD-Linie und die Signallinie (Standard 5/35/5) müssen beide über Null für Long-Trades oder unter Null für Short-Trades liegen, um die Momentumausrichtung zu bestätigen.
4. **Laguerre-Filter** – Der Wert des Laguerre-Filters muss für Long-Trades über 0,15 und für Short-Trades unter 0,75 liegen und damit die ursprünglichen Schwellenwertprüfungen reproduzieren, die für den benutzerdefinierten Indikator durchgeführt wurden.
5. **Regenbogenausrichtung** – Fünf Bündel exponentieller gleitender Durchschnitte (vier EMAs pro Bündel) müssen monoton sortiert werden, um die Regenbogenstruktur zu bestätigen. Bundles werden in bullischen Szenarien auf nicht steigende Ordnung und in bärischen Szenarien auf nicht abnehmende Ordnung bewertet.
6. **Money-Flow-Index-Filter** – Der Money-Flow-Index (Standardzeitraum 14) muss für Long-Einträge unter 40 und für Short-Einträge über 60 liegen, um einen Handel gegen den volumengesteuerten Fluss zu vermeiden.
7. **Positionsverwaltung** – Marktaufträge werden verwendet. Wenn ein entgegengesetztes Signal auftritt, wird das bestehende Engagement geschlossen und eine neue Position in die entgegengesetzte Richtung eröffnet.

## Risikomanagement
Die Strategie unterstützt integrierte Schutzmaßnahmen durch den `StartProtection`-Helfer von StockSharp:
- **Take-Profit**- und **Stop-Loss-Abstände** werden in Preisschritten ausgedrückt, um die punktbasierte Konfiguration von EA widerzuspiegeln.
- Die **Trailing Stop**-Distanz verwendet ebenfalls Preisschritte und wird aktiviert, sobald der Schutzblock gestartet wird.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `OrderVolume` | Basis-Market-Order-Volumen. | 1 |
| `TakeProfitPoints` | Nehmen Sie die Gewinndistanz in Preisschritten. | 17 |
| `StopLossPoints` | Stop-Loss-Distanz in Preisschritten. | 30 |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Preisschritten. | 45 |
| `TradingStartHour` | Erste Stunde (einschließlich), die übersprungen wird, bevor Signale ausgewertet werden. | 1 |
| `TradingEndHour` | Letzte Stunde (einschließlich), die nach der Auswertung der Signale übersprungen wird. | 23 |
| `FastEmaLength` | Länge des schnellen EMA, der im Crossover-Trigger verwendet wird. | 4 |
| `SlowEmaLength` | Länge des langsamen EMA, der im Crossover-Trigger verwendet wird. | 8 |
| `MacdFastLength` | MACD schneller EMA Länge. | 5 |
| `MacdSlowLength` | MACD langsamer EMA Länge. | 35 |
| `MacdSignalLength` | MACD Signal EMA Länge. | 5 |
| `LaguerreGamma` | Glättungsfaktor des Laguerre-Filters. | 0,7 |
| `LaguerreBuyThreshold` | Die Laguerre-Schwelle wurde für Long-Trades nach oben überschritten. | 0,15 |
| `LaguerreSellThreshold` | Die Laguerre-Schwelle wurde für Short-Trades nach unten überschritten. | 0,75 |
| `MfiPeriod` | Berechnungszeitraum des Geldflussindex. | 14 |
| `MfiBuyLevel` | Maximaler MFI-Level, der noch lange Einträge zulässt. | 40 |
| `MfiSellLevel` | Mindest-MFI-Level, das noch kurze Einstiege zulässt. | 60 |
| `RainbowGroup{1..5}Base` | Basislänge EMA für jedes Regenbogenbündel. Aus jedem Basiswert werden durch Addition von Offsets (0, 2, 4, 6) vier aufeinanderfolgende EMAs erstellt. | 5 / 13 / 21 / 34 / 55 |
| `CandleType` | Von der Strategie verwendete primäre Kerzenserie. Standardmäßig werden 5-Minuten-Kerzen verwendet. | Zeitrahmen von 5 Minuten |

## Diagramme
Die Strategie zeichnet automatisch:
- Preiskerzen für die abonnierte Serie.
- Schnelle und langsame EMAs zur visuellen Bestätigung von Überkreuzungen.
- Laguerre-Filterwerte zur Beobachtung von Schwellenwertüberschreitungen.
- Eigene Trades werden im Diagrammbereich dargestellt.

## Notizen
- Die Rainbow-Logik nähert sich den ursprünglichen benutzerdefinierten RainbowMMA-Indikatoren an, indem sie konfigurierbare EMA-Bundles erstellt. Passen Sie die Basislängen bei Bedarf an eine bestimmte Regenbogenvorlage an.
- Alle Codekommentare, Protokolle und Dokumentationen werden bei Bedarf auf Englisch bereitgestellt.
- Die Strategie konzentriert sich ausschließlich auf die C#-Implementierung. In dieser Aufgabe wird kein Python-Port generiert.
