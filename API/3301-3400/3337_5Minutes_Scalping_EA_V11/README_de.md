# FiveMinutesScalpingEA v1.1 (StockSharp-Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **FiveMinutesScalpingEaV11Strategy** ist eine Konvertierung des MetaTrader 4 Expertenberaters *5MinutesScalpingEA v1.1*. Die Strategie behält das ursprüngliche Konzept der Kombination von gleitenden Hull-Durchschnitten über mehrere Perioden, einer Momentum-Fisher-Transformation, einem ATR-Breakout-Detektor und einem Trendfilter bei, um kurzlebige Bewegungen auf einem Fünf-Minuten-Chart abzubilden. Die Implementierung folgt dem StockSharp-High-Level-API und verwendet Kerzenabonnements mit Indikatorbindungen, um das Expert Advisor-Verhalten zu reproduzieren.

Die Strategie ist für den Einzelsymbolhandel konzipiert. Es wird immer nur eine Nettoposition gehalten und alle Signale werden auf abgeschlossene Kerzen ausgewertet. Schutzaufträge werden innerhalb der Strategie durch die Überwachung von Kerzenhochs und -tiefs simuliert.

## Indikatorstapel
| Komponente | StockSharp-Implementierung | Zweck |
|-----------|--------------------------|---------|
| `i1` benutzerdefinierter Rumpf MA | `HullMovingAverage` mit Punkt `Period1` (Standard 30) | Erkennt eine schnelle Trendrichtung anhand der Steigung des gleitenden Hull-Durchschnitts. |
| `i2` benutzerdefinierter Rumpf MA | `HullMovingAverage` mit Punkt `Period2` (Standard 50) | Bestätigt die allgemeinere Trendrichtung; Für Einträge im Normalmodus müssen beide Hull-Filter übereinstimmen. |
| `i3` Fisher-Momentum | `FisherTransform` with period `Period3` | Fungiert als Impulsoszillator. Positive Werte begünstigen Long-Setups, negative Werte begünstigen Short-Setups. |
| `i4` ATR Ausbruchspfeile | `AverageTrueRange` mit Zeitraum `Period4` kombiniert mit Kerzenvergleichen | Sucht nach starken Ausbrüchen, bei denen das aktuelle Hoch/Tief die beiden vorherigen Hochs/Tiefs um mindestens ein ATR überschreitet. |
| `i5` Fisher-Trendfilter | `FisherTransform` with period `Period5` | Bietet eine geglättete Trendbestätigung ähnlich dem ursprünglichen EA-Trendhistogramm. |

Für jeden Indikator speichert die Strategie historische Werte, sodass sie den Wert `IndicatorShift` Kerzen zurücklesen kann, passend zum Parameter MQL4 `IndicatorsShift`. Alle Filter können einzeln über ihre jeweiligen Parameter deaktiviert werden.

## Handelslogik
1. Die Strategie abonniert die durch `CandleType` definierte Kerzenserie (Standard: 5-Minuten-Kerzen).
2. Bei jeder fertigen Kerze werden die Indikatoren Hull, Fisher und ATR aktualisiert. Wenn genügend Verlauf verfügbar ist, wertet die Strategie die Kerze aus, die `IndicatorShift` Balken zurückliegt.
3. **Normaler Modus** (`SignalMode = Normal`):
   - Für einen **Long**-Eintrag müssen alle aktivierten Filter bullische Bedingungen melden (positive Hull-Steigung, Fisher-Momentum über Null, ATR-Ausbruch nach oben, Trend Fisher über Null).
   - Für einen **Short**-Eintrag müssen alle aktivierten Filter bärische Bedingungen melden (negative Hull-Steigung, Fisher-Momentum unter Null, ATR-Ausbruch nach unten, Trend-Fisher unter Null).
4. **Umgekehrter Modus** (`SignalMode = Reverse`) vertauscht einfach die Interpretation von bullischen und bärischen Bedingungen.
5. Ein neues Signal schaltet das interne Flag `_lastSignal` um. Wenn `CloseOnSignal` aktiviert ist, wird die Gegenposition sofort geschlossen, bevor ein neuer Eintrag gesendet wird.
6. Der Parameter `UseTimeFilter` beschränkt Einträge auf den Bereich `[StartHour, EndHour)` (mit identischem Umlaufverhalten wie MQL4 EA).

## Risikomanagement
Der Port StockSharp implementiert die folgenden Schutzfunktionen:
- **Stop-Loss / Take-Profit** – Wenn aktiviert, werden Stop- und Zielpreise in einem festen Abstand (`StopLossPips`, `TakeProfitPips`) vom Einstiegspreis platziert und bei jeder Kerze überwacht.
- **Trailing Stop** – Wenn `UseTrailingStop` aktiviert ist, wird ein Trailing-Anker beibehalten. Sobald der Preis um `TrailingStepPips` steigt, wird der Stop so verschoben, dass er weiterhin `TrailingStopPips` vom aktuellen Extremwert entfernt bleibt.
- **Break-Even** – Wenn `UseBreakEven` aktiviert ist und sich der Preis um `BreakEvenPips + BreakEvenAfterPips` bewegt, wird der Stop auf `BreakEvenPips` vom Einstieg entfernt verschärft.
- **Einzelposition** – Alle Exits werden über Marktaufträge (`SellMarket` / `BuyMarket`) ausgeführt, die die gesamte Nettoposition schließen.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `CandleType` | M5 | Primärer Zeitrahmen. |
| `IndicatorShift` | 1 | Anzahl der geschlossenen Kerzen, auf die bei der Filterauswertung zurückgegriffen werden soll. |
| `SignalMode` | Normal | Verwenden Sie normale oder umgekehrte Signale. |
| `UseIndicator1`..`UseIndicator5` | wahr | Schaltet jeden Filter um. |
| `Period1`, `Period2`, `Period3`, `Period4`, `Period5` | 30, 50, 10, 14, 18 | Zeiträume für Hull-, Fisher- und ATR-Berechnungen. |
| `PriceMode3` | HochNiedrig | Kompatibilitätsparameter für die ursprüngliche Fisher-Preisauswahl. Die StockSharp-Implementierung speist immer den Standardkerzenpreis in den Fisher-Indikator ein. |
| `CloseOnSignal` | falsch | Schließen Sie die gegenüberliegende Position, wenn ein neues Einfahrtssignal erscheint. |
| `UseTimeFilter`, `StartHour`, `EndHour` | falsch, 0, 0 | Optionales Intraday-Handelsfenster. |
| `UseTakeProfit`, `TakeProfitPips` | stimmt, 10 | Take-Profit-Management. |
| `UseStopLoss`, `StopLossPips` | stimmt, 10 | Stop-Loss-Management. |
| `UseTrailingStop`, `TrailingStopPips`, `TrailingStepPips` | falsch, 1, 1 | Trailing-Stop-Management. |
| `UseBreakEven`, `BreakEvenPips`, `BreakEvenAfterPips` | falsch, 4, 2 | Break-Even-Stopp-Logik. |
| `TradeVolume` | 0,01 | Volumen für Markteintritte. |

## Unterschiede zum Original EA
- Die Korbschließlogik (`UseBasketClose`, `CloseInProfit`, `CloseInLoss`) ist nicht implementiert, da die Strategie StockSharp mit einer einzelnen Nettoposition arbeitet.
- Automatische Losgrößenbestimmung (`AutoLotSize` / `RiskFactor`) und Spread-Prüfungen sind nicht Teil dieses Ports. Verwenden Sie die Hosting-Umgebung, um Lautstärke und Slippage zu kontrollieren.
- Der Fisher-Preismodusparameter ist aus Kompatibilitätsgründen verfügbar, aber StockSharp `FisherTransform` verwendet derzeit den Standardkerzenpreis. Andere Preismodi können bei Bedarf durch Erweiterung des Indikators nachgebildet werden.
- Das Handelsmanagement wird für abgeschlossene Kerzen durchgeführt, was das Verhalten von EA widerspiegelt, wenn `IndicatorsShift >= 1`.

## Anwendungstipps
1. Verbinden Sie die Strategie mit einem liquiden Instrument mit engen Spreads (der EA wurde ursprünglich für EUR/USD M5 entwickelt).
2. Konfigurieren Sie `TradeVolume` gemäß Ihren Kontogrößenregeln.
3. Passen Sie die Indikatorperioden an oder deaktivieren Sie Filter, um sie an Ihre Risikotoleranz anzupassen.
4. Kombinieren Sie es mit dem integrierten Zeitfilter, um Sitzungen mit geringer Liquidität zu vermeiden.
5. Überprüfen Sie die Einstellungen immer im StockSharp-Tester, bevor Sie ihn mit Live-Daten ausführen.
