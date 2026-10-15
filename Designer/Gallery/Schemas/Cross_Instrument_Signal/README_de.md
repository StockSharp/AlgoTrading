# Strategiediagramm mit instrumentübergreifendem Signal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm synchronisiert abgeschlossene Vier-Stunden-Kerzen von TONUSDT@BNBFT und BTCUSDT@BNBFT. TONUSDT liefert ein Rate-of-Change-Signal mit Periode 20, während BTCUSDT seinen eigenen Filter aus einem einfachen gleitenden Durchschnitt mit Periode 20 liefert. Das Diagramm ist für Strategy Security BTCUSDT@BNBFT vorgesehen; ein numerischer `0/1`-Flat/Long-Zustandsmerker, ein gemeinsamer Einstiegserlaubnisstatus und ausführungsabhängiger Schutz verwalten dieses reine Long-Exposure.

![schema](schema.svg)

## Strategieübersicht

- Zwei unabhängige Instrumentvariablen konfigurieren ausschließlich die Vier-Stunden-Kerzenabonnements für TONUSDT und BTCUSDT. Deren abgeschlossene Kerzen werden vor der Entscheidung ausgerichtet, sodass TON-Momentum und BTC-Trendfilter stets zum selben synchronisierten Intervall gehören.
- TON ROC(20) ist oberhalb von null positiv und wird bei null oder darunter zur Ausstiegsbedingung. Ein BTCUSDT-Schlusskurs auf oder über SMA(20) erlaubt den Einstieg; ein Schlusskurs unter SMA(20) ist eine Ausstiegsbedingung.
- Ein Long-Einstieg erfordert gleichzeitig vier Bedingungen: `TON ROC(20) > 0`, `BTC Close >= BTC SMA(20)`, der interne Zustandsmerker meldet Flat und der gemeinsame Status `Cooldown is ready` erlaubt den Einstieg. Das extern zusammengesetzte AND löst dann einen NoCondition-Marktkauf mit Volumen 1 aus.
- Die Einstiegsaktion löscht die gemeinsame Erlaubnis und startet Entry Cooldown N, der diskretionäre Signalverkauf löscht denselben Status und startet Signal-exit Cooldown N. Der zugehörige Timer stellt die Erlaubnis nach acht synchronisierten Kerzenpaaren wieder her; eine Generationsprüfung unterdrückt den Abschluss eines älteren Timers nach einer neueren Rücksetzung. Ausstiege warten nicht auf diesen Status, und Schutzausstiege setzen ihn nicht zurück.
- Die BTCUSDT-Marktkaufausführung setzt den Zustandsmerker auf Long und aktiviert 2% Take-Profit sowie festen, nicht nachlaufenden 2.5% Stop-Loss. Eine diskretionäre Schließungsausführung oder Take/Stop-Aktivierung und -Ausführung setzt ihn wieder auf Flat. Die Strategie eröffnet niemals einen Short.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Bei einem synchronisierten Paar abgeschlossener Vier-Stunden-Kerzen löst das externe Einstiegs-AND einen NoCondition-Marktkauf mit Volumen 1 aus, wenn TON ROC(20) über null liegt, BTC Close auf oder über BTC SMA(20) liegt, der Zustandsmerker Flat meldet und der gemeinsame Status `Cooldown is ready` den Einstieg erlaubt. Die Aktion handelt das gewählte Strategy Security; es muss BTCUSDT@BNBFT sein und dem Kerzenparameter Traded Security entsprechen.
- **Short-Einstieg**: Es gibt keinen Short-Einstieg. Der diskretionäre Verkauf verwendet ReduceOnly, MarketOrder und Volumen 1 und kann daher nur das Exposure des gewählten Strategy Security reduzieren; Take- und Stop-Schutz schließen ebenfalls das Long-Exposure.
- **Ausstieg**: Wenn der Zustandsmerker Long meldet, löst `TON ROC(20) <= 0` oder `BTC Close < BTC SMA(20)` den ReduceOnly-Marktverkauf aus, ohne auf den gemeinsamen Cooldown-Status zu warten. Auch 2% Take-Profit oder fester 2.5% Stop-Loss können das Exposure per Marktorder schließen; der Stop läuft nicht nach.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Traded Security | BTCUSDT@BNBFT | Instrument ausschließlich für das Kerzenabonnement des gehandelten Werts. Das gewählte Strategy Security muss ebenfalls BTCUSDT@BNBFT sein, da Aktionen und Strategieausführungen Strategy Security verwenden. |
| Signal Security | TONUSDT@BNBFT | Instrument ausschließlich für das Signal-Kerzenabonnement; sein Momentum trägt zum Signal bei, aber keine Aktion ist an diese Variable adressiert. |
| BTC Candles Series | 04:00:00 | Serie abgeschlossener Vier-Stunden-Kerzen von BTCUSDT für Close, SMA(20), Zustandsentscheidungen und Chart. |
| TON Candles Series | 04:00:00 | Serie abgeschlossener Vier-Stunden-Kerzen von TONUSDT für ROC(20) und synchronisierte Entscheidungen. |
| BTC SMA Length | 20 | Periode des aus abgeschlossenen BTCUSDT-Kerzen berechneten SimpleMovingAverage. |
| TON ROC Length | 20 | Periode der aus abgeschlossenen TONUSDT-Kerzen berechneten RateOfChange. |
| ROC Threshold | 0 | Nulllinie zur Trennung des positiven Momentumzustands für den Einstieg vom nicht positiven Signalausstieg. |
| Entry Cooldown N | 8 | Anzahl synchronisierter Kerzenpaare, die der Timer der Einstiegsaktion zählt, bevor er die gemeinsame Erlaubnis wiederherstellen kann. |
| Signal-exit Cooldown N | 8 | Anzahl synchronisierter Kerzenpaare, die der Timer des diskretionären Signalausstiegs zählt, bevor er die gemeinsame Erlaubnis wiederherstellen kann. |
| Order Volume | 1 | Feste Menge für NoCondition-Marktkauf und ReduceOnly-Marktverkauf des gewählten Strategy Security. |
| Take Profit | 2% | Prozentualer Gewinn gegenüber dem ausgeführten Einstiegspreis, der den Take-Profit aktiviert. |
| Stop Loss | 2.5% | Prozentualer Verlust gegenüber dem ausgeführten Einstiegspreis, der den Stop-Loss aktiviert. |
| Trailing Stop Loss | false | Deaktiviert; dadurch bleibt der 2.5% Stop-Loss fest und folgt keiner günstigen Kursbewegung. |
| Use Market Orders | true | Aktiviert; dadurch schließen Take-Profit und Stop-Loss die Position mit Marktorders. |

## Diagrammdetails

- Getrennte instrumentbezogene [Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)-Bausteine versorgen ausschließlich die unabhängigen Kerzenabonnements TONUSDT@BNBFT und BTCUSDT@BNBFT. Orderaktionen und Strategieausführungen verwenden Strategy Security; wählen Sie dort ebenfalls BTCUSDT@BNBFT, damit es dem Kerzenparameter Traded Security entspricht.
- Zwei [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Bausteine geben nur abgeschlossene Vier-Stunden-Kerzen aus. Ein [Sync](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/sync.html)-Baustein paart beide Ströme im Intervall `04:00:00`, bevor eines der Instrumente in die Entscheidungskette gelangt.
- Ein [Indikator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Baustein berechnet RateOfChange 20 für TONUSDT, ein weiterer SimpleMovingAverage 20 für BTCUSDT. Vergleichsbausteine bilden die positiven und nicht positiven ROC-Zustände sowie die Lage von BTC Close über oder unter seinem Durchschnitt ab.
- Eine numerische Unit-[Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) dient als Zustandsmerker: `0` bedeutet Flat und `1` Long. Buy MyTrade schreibt `1`; MyTrade des diskretionären Verkaufs sowie Take/Stop-Aktivierungs- und MyTrade-Ereignisse schreiben `0`. Logikbausteine kombinieren diesen Zustand mit den synchronisierten Signalen.
- Zwei aktionsspezifische N-Werte-Timer halten Entry Cooldown N und Signal-exit Cooldown N auf 8. Jede der Aktionen löscht dieselbe gemeinsame Einstiegserlaubnis; ihr Timer kann `Cooldown is ready` nach acht synchronisierten Paaren wiederherstellen, während eine Generationsprüfung einen veralteten Abschluss eines früheren Timers verwirft. Das externe Flat-Einstiegs-AND hat genau einen Cooldown-Eingang. Es löst eine als NoCondition, MarketOrder, Volumen 1 konfigurierte Kaufaktion [Position ändern](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) aus; der Signalausstieg löst ohne diesen Eingang einen getrennten ReduceOnly-Marktverkauf mit Volumen 1 aus.
- Der [Positionsschutz](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) erhält Kauf- und diskretionäre Schließungsausführungen: Der Kauf aktiviert Take Profit `2%` und festen Stop Loss `2.5%`, die Schließung löscht veralteten Schutzstatus. Trailing Stop Loss ist `false`, Use Market Orders ist `true`. Der Chart erhält beide synchronisierten Kerzenströme, BTC SMA(20), TON ROC(20), Take- und Stop-Orderströme sowie alle BTC-Ausführungen aus Strategieausführungen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
