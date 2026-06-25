# Russian20 Momentum MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Russian20 Momentum MA-Strategie** ist eine direkte Konvertierung des MetaTrader 5-Expert-Advisors `Russian20-hp1.mq5`. Das Originalskript wurde von Gordago Software Corp. veröffentlicht und basiert auf einem Zwei-Stunden-Chart, einem 20-Perioden-Simple-Moving-Average (SMA) und einem 5-Perioden-Momentum-Indikator, um kurzfristige Trendfortsetzungen zu identifizieren. Die StockSharp-Implementierung behält denselben analytischen Kern bei und passt die Auftragsabwicklung und das Geldmanagement an die High-Level-Strategie-API an.

## Handelslogik
- **Datenfrequenz:** Arbeitet mit dem benutzerdefinierten Kerzentyp (Standard sind 2-Stunden-Kerzen, entsprechend dem MQL5-Zeitrahmen `PERIOD_H2`). Die Logik wird nur ausgeführt, wenn eine Kerze geschlossen ist.
- **Indikatoren:**
  - Einfacher gleitender Durchschnitt mit konfigurierbarer Periode (Standard 20).
  - Momentum-Indikator mit konfigurierbarer Periode (Standard 5). Der neutrale Momentum-Level ist 100, entsprechend der MQL5-Standardausgabe.
- **Long-Einstieg:** Wird ausgelöst, wenn alle folgenden Bedingungen auf der zuletzt geschlossenen Kerze erfüllt sind:
  1. Schlusskurs liegt über dem SMA.
  2. Momentum-Wert ist größer als 100 (positive Beschleunigung).
  3. Der Schlusskurs liegt höher als der Schlusskurs der vorherigen Kerze, was aufwärtigen Momentum in der Kursbewegung sicherstellt.
- **Short-Einstieg:** Wird ausgelöst, wenn alle folgenden Bedingungen erfüllt sind:
  1. Schlusskurs liegt unter dem SMA.
  2. Momentum-Wert ist kleiner als 100 (negative Beschleunigung).
  3. Der Schlusskurs liegt niedriger als der Schlusskurs der vorherigen Kerze.
- **Long-Ausstieg:** Die Strategie liquidiert Long-Positionen, wenn Momentum unter 100 fällt oder wenn ein schützender Stop-Loss- oder Take-Profit-Schwellenwert überschritten wird.
- **Short-Ausstieg:** Die Strategie liquidiert Short-Positionen, wenn Momentum über 100 steigt oder wenn die konfigurierten Schutzschwellenwerte erreicht werden.

## Risikomanagement
Der ursprüngliche MQL5-Expert-Advisor platziert feste Stop-Loss- und Take-Profit-Orders in "Pips", die für 4- und 5-stellige Forex-Preise angepasst sind. Die C#-Konvertierung reproduziert dieses Verhalten durch:
- Berechnung einer angepassten Pip-Größe aus dem `PriceStep` des Wertpapiers. Für Symbole mit drei oder fünf Dezimalstellen entspricht die Pip-Größe `PriceStep * 10`, andernfalls `PriceStep`.
- Übersetzung der Benutzereingaben für Stop-Loss und Take-Profit in absolute Preisabstände.
- Überwachung der Kursbewegung auf jeder geschlossenen Kerze und Schließung der Position, wenn der Kurs die berechneten Schwellenwerte kreuzt.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | 2-Stunden-Kerzen | Datentyp für die Signalerzeugung. |
| `MovingAverageLength` | 20 | Rückblick für den SMA-Filter. |
| `MomentumPeriod` | 5 | Rückblick für den Momentum-Indikator. |
| `StopLossBuyPips` | 50 | Long-Stop-Loss-Abstand in Pips. 0 zum Deaktivieren. |
| `TakeProfitBuyPips` | 50 | Long-Take-Profit-Abstand in Pips. 0 zum Deaktivieren. |
| `StopLossSellPips` | 50 | Short-Stop-Loss-Abstand in Pips. 0 zum Deaktivieren. |
| `TakeProfitSellPips` | 50 | Short-Take-Profit-Abstand in Pips. 0 zum Deaktivieren. |

Alle numerischen Parameter werden über `StrategyParam<T>` bereitgestellt und gegebenenfalls als optimierbar markiert, was Backtesting und Optimierung mit StockSharp-Tools ermöglicht.

## Implementierungshinweise
- Die Strategie verwendet die High-Level-API `SubscribeCandles().Bind(...)` zum Streamen von Kerzendaten und gleichzeitigen Abrufen von SMA- und Momentum-Werten ohne manuelle Indikator-Buchhaltung.
- Momentum-Level werden genau wie im MQL5-Skript ausgewertet (100 als neutrales Level). Jeder Verstoß jenseits der Stop-Loss/Take-Profit-Offsets löst einen Marktausstieg aus und ahmt originalgetreu die ursprüngliche Order-Platzierungslogik nach.
- Der vorherige Schlusskurs wird zwischengespeichert, um Preis-Momentum zu überprüfen, ohne auf historische Sammlungssuchen zurückzugreifen, in Übereinstimmung mit den Leistungsrichtlinien des Projekts.
- Visualisierungs-Hooks (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) sind zur Vereinfachung verdrahtet, wenn die Host-Umgebung Charting unterstützt.

## Verwendungshinweise
- Der Standard-Zeitrahmen und die Parameter entsprechen der ursprünglichen Konfiguration des Autors. Passen Sie den Kerzentyp an, wenn Sie mit Instrumenten arbeiten, die keine 2-Stunden-Bars erzeugen.
- Beim Handel mit Vermögenswerten, die mit unkonventionellen Tick-Größen notiert sind, überprüfen Sie die berechnete Pip-Größe, um sicherzustellen, dass die Stop-Loss- und Take-Profit-Abstände realistisch bleiben.
- Die Strategie ist für eine einzelne offene Position gleichzeitig ausgelegt. Externe manuelle Trades oder gleichzeitige Positionen im selben Wertpapier können die integrierte Ausstiegslogik beeinträchtigen.
