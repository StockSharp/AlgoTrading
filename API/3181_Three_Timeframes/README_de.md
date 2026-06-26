# Three Timeframes-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Three Timeframes-Strategie** repliziert den MetaTrader Expert `Three timeframes.mq5` mit der StockSharp High-Level-API. Das System kombiniert Momentum- und Trendfilter aus verschiedenen Zeitrahmen:

- **MACD (M5)** erkennt aktuelle Momentum-Umkehrungen auf dem Handelszeitrahmen.
- **Alligator (H4)** verifiziert, dass die Struktur des höheren Zeitrahmens mit der beabsichtigten Handelsrichtung übereinstimmt.
- **RSI (H1)** bestätigt, dass das Momentum auf dem mittleren Zeitrahmen den Ausbruch unterstützt.
- Optionale **Sessionsfilterung** blockiert Trades außerhalb der konfigurierten Arbeitszeiten.

Die Strategie verwendet pip-basiertes Risikomanagement. Anfängliche Stop-Loss- und Take-Profit-Niveaus werden an jede neue Position angehängt. Wenn der Preis vorrückt, zieht ein optionaler Trailing-Stop den Schutz-Stop nach, sobald der Markt sowohl die Trailing-Distanz als auch den Trailing-Schritt abgedeckt hat.

## Signallogik
1. Preise werden auf drei verschiedenen Abonnements verarbeitet: Handelskerzen, höherzeitrahmen Kerzen für den Alligator und mittlere Kerzen für den RSI.
2. Ein Long-Setup erfordert:
   - MACD-Hauptlinie kreuzt **unterhalb** der Signallinie auf der vorherigen Bar, während die Bar davor über der Signallinie war, was die MetaTrader-Regel „blau kreuzt rot abwärts" reproduziert.
   - RSI auf dem H1-Feed über 50.
   - Alligator jaw > teeth > lips auf der vorherigen abgeschlossenen H4-Kerze, was eine Aufwärtsstruktur signalisiert.
3. Ein Short-Setup spiegelt die Regeln wider: die MACD-Hauptlinie kreuzt über die Signallinie, RSI liegt unter 50, und lips > teeth > jaw auf dem Alligator, um eine Abwärtsstruktur zu bestätigen.
4. Wenn eine Gegenposition existiert, schließt die Strategie sie durch Senden einer Marktorder für den Nettobetrag, genau wie der Original-EA vor dem Öffnen eines neuen Trades.
5. Nach dem Einstieg wendet die Strategie anfängliche Stop-Loss/Take-Profit-Abstände an und verfolgt den Stop weiter, sobald sich der Preis um `TrailingStopPips + TrailingStepPips` vom Einstieg entfernt.

Der Handelssessionsfilter spiegelt die MetaTrader-Implementierung wider. Wenn die Startstunde kleiner als die Endstunde ist, ist der Handel nur innerhalb des Intervalls erlaubt. Wenn die Startstunde größer als die Endstunde ist, spannt das aktive Fenster Mitternacht.

## Risikomanagement
- **Stop Loss / Take Profit** – Abstände werden in Pips ausgedrückt. Die Strategie konvertiert sie über den Preisschritt des Symbols in Preiseinheiten und passt für 3- oder 5-stellige FX-Notierungen an.
- **Trailing-Stop** – aktiviert sich, sobald der Trade sowohl die Trailing-Stop- als auch die Trailing-Schritt-Distanz abgedeckt hat. Der Stop wird dann auf `price - trailing distance` für Longs und `price + trailing distance` für Shorts bewegt.
- **Handelsvolumen** – gibt die Basis-Lotgröße für neue Marktorders an. Entgegengesetztes Exposure wird automatisch geflättet vor dem Umkehren.

## Unterschiede zur MetaTrader-Version
- Das asynchrone Ordermodell von StockSharp beseitigt die Notwendigkeit expliziter Transaktions-Tracking-Flags (`m_waiting_transaction`). Orders werden mit `BuyMarket`/`SellMarket` ausgeführt, die bereits intern auf Bestätigungen warten.
- Slippage-, Filling-Policy- und Margin-Mode-Einstellungen aus der MQL-Version werden von StockSharp abstrahiert. Diese plattformspezifischen Steuerungen sind für die .NET-Implementierung nicht erforderlich.
- Der Alligator-Indikator wird aus geglätteten gleitenden Durchschnitten rekonstruiert, wobei die ursprünglichen Perioden und Shifts erhalten bleiben. Indikatorwerte werden in gleitenden Puffern gespeichert, um das Offset-Verhalten des eingebauten MetaTrader-Alligators zu reproduzieren.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Marktordergröße in Lots/Kontrakten. | `1` |
| `StopLossPips` | Anfänglicher Stop-Loss-Abstand in Pips. | `50` |
| `TakeProfitPips` | Anfänglicher Take-Profit-Abstand in Pips. | `140` |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. | `5` |
| `TrailingStepPips` | Zusätzliche Pip-Bewegung bevor der Trailing-Stop bewegt wird. | `5` |
| `MacdFastPeriod` | Schnelle EMA-Länge für MACD. | `13` |
| `MacdSlowPeriod` | Langsame EMA-Länge für MACD. | `26` |
| `MacdSignalPeriod` | Signal-Glättungsperiode für MACD. | `10` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Alligator-SMMA-Perioden für jaw/teeth/lips. | `13`, `8`, `5` |
| `JawShift`, `TeethShift`, `LipsShift` | Vorwärts-Shifts für die Alligator-Linien. | `8`, `5`, `3` |
| `RsiPeriod` | RSI-Mittelungslänge auf dem mittleren Zeitrahmen. | `14` |
| `CandleType` | Handelszeitrahmen (Standard 5-Minuten-Kerzen). | `M5` |
| `AlligatorCandleType` | Höherer Zeitrahmen für Alligator-Berechnung (Standard 4-Stunden-Kerzen). | `H4` |
| `RsiCandleType` | Mittlerer Zeitrahmen für RSI-Bestätigung (Standard 1-Stunden-Kerzen). | `H1` |
| `UseTimeFilter` | Aktiviert den Sessionsfilter. | `true` |
| `StartHour` | Sessionsstartzeit (inklusiv). | `10` |
| `EndHour` | Sessionsendzeit (exklusiv). | `15` |

## Nutzungshinweise
- Stellen Sie sicher, dass das ausgewählte Wertpapier die drei konfigurierten Kerzenstreams bereitstellt (standardmäßig M5, H1, H4). StockSharp fordert automatisch alle erforderlichen Abonnements über `GetWorkingSecurities()` an.
- Die Pip-Konvertierung basiert auf `Security.PriceStep`. Instrumente mit ungewöhnlichen Tick-Größen benötigen möglicherweise manuelle Anpassung der Stop-/Take-Parameter.
- Trailing-Stops erfordern, dass sowohl `TrailingStopPips` als auch `TrailingStepPips` größer als null sind. Das Setzen eines Parameters auf null deaktiviert die Trailing-Logik, konsistent mit dem MQL-Expert.
