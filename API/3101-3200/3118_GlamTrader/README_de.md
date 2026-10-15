# GlamTrader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **GlamTrader-Strategie** ist eine StockSharp High-Level-API-Konvertierung des MetaTrader-Expertenberaters `GlamTrader.mq5`. Der ursprüngliche Roboter verbindet einen verschobenen gleitenden Durchschnitt mit dem Laguerre-RSI-Oszillator und dem Awesome Oscillator, um den Momentum zu filtern, bevor eine einzige Marktposition eröffnet wird. Der Port bewahrt den exakten Entscheidungsbaum und die Geldmanagement-Regeln, während Orderausführung, Charting und Risikokontrollen an StockSharp-Konventionen angepasst werden.

## Funktionsweise der Strategie

1. Die durch `CandleType` definierte Kerzenserie abonnieren (standardmäßig M15). Der ausgewählte Zeitrahmen versorgt jeden Indikator.
2. Einen konfigurierbaren gleitenden Durchschnitt auf der ausgewählten `AppliedPrice`-Quelle erstellen und ihn um `MaShift` Bars verschieben, um den in MetaTrader verwendeten verschobenen Puffer zu reproduzieren.
3. Den Laguerre-RSI-Filter intern mithilfe des vierstufigen rekursiven Filters nachbilden (`LaguerreGamma` steuert den Glättungsfaktor). Der Wert bleibt im Bereich `[0;1]` wie der ursprüngliche benutzerdefinierte Indikator.
4. Den Awesome Oscillator mit Standard-5/34-einfachen Durchschnitten des Median-Preises berechnen und die aktuellen und vorherigen Messwerte für die Steilheitserkennung speichern.
5. Nur wenn keine Position offen ist:
   - **Long-Einstieg** – gleitender Durchschnitt über dem aktuellen Schlusskurs, Laguerre RSI über `0.15`, und Awesome Oscillator steigend im Vergleich zur vorherigen Bar.
   - **Short-Einstieg** – gleitender Durchschnitt unter dem aktuellen Schlusskurs, Laguerre RSI unter `0.75`, und Awesome Oscillator fallend im Vergleich zur vorherigen Bar.
6. Beim Einstieg wandelt die Strategie Stop-Loss/Take-Profit-Abstände von Pips in Preisabstände unter Verwendung der Instrument-Tick-Größe um. Abstände werden für 3- oder 5-stellige Kurse genau wie `Point * 10` in MQL angepasst.
7. Während eine Position aktiv ist, spiegelt der Algorithmus die ursprüngliche Trailing-Routine: sobald der Preis mehr als `TrailingStopPips + TrailingStepPips` voranzieht, wird der Stop auf `TrailingStopPips` hinter (oder über) dem Markt nachgezogen. Exits werden ausgeführt, wenn der Kerzenrange den Trailing-Stop- oder Take-Profit-Preis berührt.

## Einstiegs- und Ausstiegslogik

- Immer höchstens eine Position halten. Entgegengesetzte Signale werden ignoriert, bis der aktuelle Trade geschlossen ist.
- Long-Trades erfordern einen bearischen verschobenen gleitenden Durchschnitt (Preis kreuzt über die Linie), Laguerre RSI verlässt die überverkaufte Zone (`> 0.15`), und zunehmendes Awesome Oscillator-Momentum.
- Short-Trades erfordern einen bullischen verschobenen gleitenden Durchschnitt (Preis kreuzt unter die Linie), Laguerre RSI fällt aus der überkauften Zone (`< 0.75`), und abnehmendes Awesome Oscillator-Momentum.
- Stops und Ziele werden durch Preisvergleiche gegen Kerzenhochs/-tiefs durchgesetzt, so dass Intrabar-Treffer respektiert werden, obwohl die Logik auf abgeschlossenen Kerzen läuft.
- Trailing folgt der MetaTrader-Regel: der Stop bewegt sich nur, nachdem der Preis um den Stop-Abstand plus den Trailing-Schritt voranzieht, und kehrt nie zurück.

## Parameter

| Parameter | Typ | Standard | Beschreibung |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(15).TimeFrame()` | Zeitrahmen für Indikatorberechnungen und Entscheidungsfindung. |
| `TradeVolume` | `decimal` | `1` | Volumen für Marktorders. |
| `StopLossBuyPips` | `decimal` | `50` | Stop-Loss-Abstand in Pips für Long-Einstiege. |
| `TakeProfitBuyPips` | `decimal` | `50` | Take-Profit-Abstand in Pips für Long-Einstiege. |
| `StopLossSellPips` | `decimal` | `50` | Stop-Loss-Abstand in Pips für Short-Einstiege. |
| `TakeProfitSellPips` | `decimal` | `50` | Take-Profit-Abstand in Pips für Short-Einstiege. |
| `TrailingStopPips` | `decimal` | `5` | Trailing-Stop-Abstand in Pips. Auf null setzen um Trailing zu deaktivieren. |
| `TrailingStepPips` | `decimal` | `15` | Zusätzlicher Gewinn (in Pips) erforderlich bevor der Trailing-Stop sich bewegen kann. |
| `MaPeriod` | `int` | `14` | Lookback-Länge des gleitenden Durchschnitts. |
| `MaShift` | `int` | `1` | Positive Verschiebung auf den gleitenden Durchschnitt angewendet. |
| `MaMethod` | `MaMethod` | `LinearWeighted` | Typ des gleitenden Durchschnitts (einfach, exponentiell, geglättet oder linear gewichtet). |
| `AppliedPrice` | `AppliedPrice` | `Weighted` | Preisquelle für gleitenden Durchschnitt und Laguerre-Filter. |
| `LaguerreGamma` | `decimal` | `0.7` | Laguerre-Glättungskoeffizient (Bereich 0–1). |

## Verwendungshinweise

1. Die Strategie an das gewünschte Wertpapier anhängen, sicherstellen, dass das Broker-Modell Tick-Größen-/Schrittinformationen liefert, und `CandleType` auf den gewünschten Zeitrahmen einstellen.
2. Pip-basierte Risikoparameter an die Instrumentvolatilität anpassen. Die Konvertierung normalisiert Abstände automatisch mit `PriceStep`; fünfstellige FX-Symbole erhalten den erwarteten 10×-Multiplikator.
3. Optionale Chart-Helfer zeichnen den gleitenden Durchschnitt im Preisbereich und tragen den Awesome Oscillator in einem separaten Panel zusammen mit den Trades ein.
4. Die Strategie starten. Sie verwaltet Stops und Trailing intern und spiegelt die `OpenBuy`-, `OpenSell`- und Trailing-Routinen aus dem ursprünglichen MQL-Code.

## Hinweise

- Die Laguerre-RSI-Implementierung spiegelt den `laguerre.mq5`-Indikator, einschließlich der `CU/(CU+CD)`-Normalisierung.
- Awesome Oscillator-Werte kommen aus StockSharp's eingebautem Indikator, daher ist kein manuelles Puffer-Kopieren erforderlich.
- Da die Logik auf abgeschlossenen Kerzen ausgewertet wird, bleiben Backtests und Live-Trading deterministisch und frei von Tick-Level-Repainting.
