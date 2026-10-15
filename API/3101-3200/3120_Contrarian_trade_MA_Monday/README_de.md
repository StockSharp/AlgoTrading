# Konträre MA-Montags-Handelsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie recreiert den MetaTrader-Expertenberater **"Contrarian trade MA"** unter Verwendung der StockSharp High-Level-API. Sie kombiniert wöchentlichen Kontext mit einem Nur-Montags-Einstiegsfilter, um gegen Extreme zu handeln. Das System wartet auf eine neue Handelswoche, misst, wie weit die Vorwoche relativ zum höchsten Hoch und niedrigsten Tief über das Lookback-Fenster geschlossen hat, und prüft, ob der Preis die neue Woche auf der anderen Seite eines verschobenen gleitenden Durchschnitts eröffnet hat. Wenn der Markt die erste Tageskerze der Woche außerhalb dieser Schwellenwerte beendet, wird eine konträre Position eröffnet.

Die Logik beruht ausschließlich auf abgeschlossenen Kerzen. Eine Tagesserie (Standard) steuert Einstiege und Ausstiege, während eine Wochenserie die Extremwerte und das gleitende Durchschnitt-Signal liefert. Jedes Mal wenn eine Montags-Kerze abschließt, bewertet die Strategie, ob die Vorwoche über der jüngsten Hoch-Band oder unter der jüngsten Tief-Band endete, oder ob der vorherige MA-Wert auf der anderen Seite des aktuellen wöchentlichen Eröffnungspreises steht. Die Annahme ist, dass solche überdehnten Bewegungen dazu neigen, sich im Laufe der Woche zurückzumitteln.

## Funktionsweise

1. Wochenkerzen versorgen zwei Indikatoren:
   - `Highest`/`Lowest` finden das extreme Hoch und Tief über `CalcPeriod` Wochen.
   - Ein konfigurierbarer gleitender Durchschnitt (`MaPeriod`, `MaMethod`, `MaShift`, `AppliedPrice`) verarbeitet dieselben Wochenkerzen.
2. Tageskerzen (oder jeder ausgewählte `TradeCandleType`) lösen Handelsentscheidungen aus, sobald sie abgeschlossen sind.
3. Bei der ersten abgeschlossenen Kerze, deren `OpenTime.DayOfWeek == Monday` ist, bewertet die Strategie Einstiegsbedingungen:
   - **Long** wenn der vorherige wöchentliche Schlusskurs über dem höchsten Hoch des Lookbacks liegt, oder wenn der vorherige MA-Wert größer als der aktuelle wöchentliche Eröffnungspreis ist (d.h. Preis eröffnete unter dem MA).
   - **Short** wenn der vorherige wöchentliche Schlusskurs unter dem niedrigsten Tief des Lookbacks liegt, oder wenn der vorherige MA-Wert kleiner als der aktuelle wöchentliche Eröffnungspreis ist (Preis eröffnete über dem MA).
4. Orders werden mit `BuyMarket` oder `SellMarket` unter Verwendung des Strategie-Volumens ohne Mittelwertbildung gesendet. Es kann jeweils nur eine Position offen sein.

## Exit-Management

- Ein fester Stop-Loss-Abstand wird als `StopLossPips * Security.PriceStep` berechnet. Wenn aktiviert (> 0), überwacht die Strategie Tageskerzenhochs und -tiefs; wenn der Preis das Stop-Level innerhalb des Tages berührt, wird die Position zu Markt geschlossen.
- Ein zeitbasierter Exit schließt jede offene Position, sobald sieben Tage seit dem Einstieg vergangen sind (`604800` Sekunden im Original-EA). Die Prüfung erfolgt bei jeder abgeschlossenen Tageskerze.
- Die Strategie öffnet niemals einen neuen Trade, bis der vorherige vollständig geschlossen ist.

## Indikatoren und Daten

- **Wöchentliche Extrema:** `Highest`- und `Lowest`-Indikatoren, die an die `MaCandleType`-Serie (Standard: 1-Wochen-Kerzen) angehängt sind.
- **Wöchentlicher gleitender Durchschnitt:** `Simple`, `Exponential`, `Smoothed` oder `LinearWeighted` Methoden sind verfügbar. Der gleitende Durchschnitt kann um `MaShift` Bars nach vorne verschoben werden, um die MetaTrader-Einstellung zu imitieren, und kann verschiedene Preisquellen verarbeiten (`AppliedPrice`).
- **Primärer Zeitrahmen:** `TradeCandleType` definiert, welche Kerzen das Trade-Timing steuern; der Standard sind Tageskerzen, damit Einstiege nach dem ersten Tag der Handelswoche ausgewertet werden.

## Parameter

| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CalcPeriod` | `int` | `4` | Anzahl höherer Zeitrahmen-Kerzen zur Berechnung des höchsten Hochs und niedrigsten Tiefs. |
| `StopLossPips` | `int` | `300` | Stop-Loss-Abstand in Preisschritten. Auf `0` setzen zum Deaktivieren des Schutz-Stops. |
| `MaPeriod` | `int` | `7` | Länge des wöchentlichen gleitenden Durchschnitts. |
| `MaShift` | `int` | `0` | Vorwärtsverschiebung des gleitenden Durchschnitts in Bars. Spiegelt den MetaTrader MA-Verschiebungsparameter. |
| `MaMethod` | `MovingAverageMethod` | `LinearWeighted` | Berechnungsmethode des gleitenden Durchschnitts (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). |
| `AppliedPrice` | `AppliedPriceType` | `Weighted` | Preisquelle für den gleitenden Durchschnitt (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `TradeCandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Primärer Zeitrahmen, der Einstiege auslöst und Stops/Exits verwaltet. |
| `MaCandleType` | `DataType` | `TimeSpan.FromDays(7).TimeFrame()` | Höherer Zeitrahmen für den gleitenden Durchschnitt und die Berechnung der Extrema. |

## Hinweise

- Der Stop-Loss-Abstand passt sich dem Instrument an, indem der Pip-Zähler mit `Security.PriceStep` multipliziert wird. Instrumente ohne definierten Schritt werden den Stop effektiv deaktivieren.
- Da die Strategie abgeschlossene Kerzen auswertet, erfolgen Einstiege beim Schlusskurs der Montags-Bar und nicht beim ersten Tick der Woche. Dies hält das Verhalten über Backtests hinweg deterministisch.
- Die Logik geht von nur einer offenen Position aus; jeder offene Trade wird entweder durch den Stop-Loss oder durch das Sieben-Tage-Timeout geschlossen, bevor ein neues Signal in Betracht gezogen wird.
