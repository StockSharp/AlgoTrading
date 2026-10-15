# XAng Zad C TM MM Rec Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein C#-Port des MetaTrader-Expert-Advisors **Exp_XAng_Zad_C_Tm_MMRec**. Sie handelt adaptive Preisenvelopes, die vom benutzerdefinierten *XAng Zad C*-Indikator berechnet werden, und fügt ein zeitbasiertes Handelsfenster zusammen mit einem einfachen Money-Management-Zähler hinzu. Das Ziel ist, Ausbrüche zu erfassen, wenn die adaptiven oberen und unteren Linien sich kreuzen, während die Positionsgröße nach einer konfigurierbaren Anzahl verlierender Trades dynamisch skaliert wird.

### Kernlogik
- **Indikator** – der XAng Zad C-Indikator erzeugt einen adaptiven oberen und unteren Kanal. Die C#-Version reproduziert die Envelope-Berechnung und unterstützt mehrere Moving-Average-Glätter (SMA, EMA, SMMA, LWMA). Exotische Glätter aus dem ursprünglichen Skript fallen auf EMA zurück.
- **Einstiegssignale** – wenn die vorherige Kerze zeigt, dass die obere Linie über der unteren liegt, und die aktuelle Kerze mit der oberen Linie unter der unteren schließt, wird ein bullischer Ausbruch erkannt. Die entgegengesetzte Konfiguration erzeugt einen bärischen Ausbruch. Der Parameter `SignalShift` definiert, wie viele geschlossene Kerzen zurück verglichen werden sollen.
- **Ausstiegssignale** – optionale Flags ermöglichen das Schließen von Long-Positionen, wenn die obere Linie wieder unter die untere fällt, und das Schließen von Short-Positionen bei umgekehrtem Ereignis. Positionen werden auch sofort geschlossen, wenn das konfigurierte Handelsfenster endet.
- **Money-Management** – die Strategie führt eine Liste historischer Trade-Ergebnisse. Wenn die jüngsten `BuyLossTrigger` (oder `SellLossTrigger`) Verlust-Trades innerhalb der letzten `BuyTotalTrigger` (oder `SellTotalTrigger`) Trades erscheinen, verwendet die nächste Position das reduzierte Volumen. Andernfalls wird das normale Volumen wiederhergestellt.
- **Risikokontrolle** – statische Stop-Loss- und Take-Profit-Ziele werden in Vielfachen des Instrument-Preis-Schritts angewendet. Wenn ein Niveau während der Kerze erreicht wird, wird die Position zum entsprechenden Preis geglättet.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `NormalVolume` | Standard-Ordergröße, die verwendet wird, wenn keine jüngste Verlustserie vorliegt. |
| `ReducedVolume` | Ordergröße, die nach einer Folge von Verlust-Trades angewendet wird. |
| `BuyTotalTrigger` / `SellTotalTrigger` | Anzahl der historischen Trades, die bei der Bewertung des Verlustzählers inspiziert werden. |
| `BuyLossTrigger` / `SellLossTrigger` | Erforderliche Verlust-Trades (innerhalb des obigen Fensters), um auf das reduzierte Volumen umzuschalten. |
| `EnableBuyEntries` / `EnableSellEntries` | Long- oder Short-Einstiege erlauben. |
| `EnableBuyExit` / `EnableSellExit` | Automatische Ausstiegssignale auf Basis von Kanalkreuzungen erlauben. |
| `UseTradingWindow` | Zeitfilter aktivieren. Außerhalb des Fensters werden alle Positionen geschlossen und keine neuen Orders abgeschickt. |
| `WindowStart` / `WindowEnd` | Start- und Endzeiten des täglichen Handelsfensters (UTC). Das Fenster kann Mitternacht überspannen. |
| `StopLoss` | Stop-Loss-Abstand ausgedrückt in Vielfachen von `Security.PriceStep`. Auf `0` setzen zum Deaktivieren. |
| `TakeProfit` | Gewinnziel-Abstand ausgedrückt in Vielfachen von `Security.PriceStep`. Auf `0` setzen zum Deaktivieren. |
| `SignalShift` | Anzahl der bereits geschlossenen Kerzen für den Kreuzungsvergleich. |
| `CandleType` | Kerzendatentyp für den Indikator (Standard: 4-Stunden-Kerzen). |
| `SmoothMethods` | Moving-Average-Glätter innerhalb des Indikators. Nicht unterstützte Werte verwenden automatisch EMA. |
| `MaLength` | Glättungslänge für den Indikator. |
| `MaPhase` | Zusätzlicher Phasenparameter aus dem ursprünglichen Indikator übernommen (derzeit informativ). |
| `Ki` | Verhältnis, das steuert, wie schnell die adaptiven Envelopes auf Preisänderungen reagieren. |
| `AppliedPrices` | Preisquelle für den Indikator (Schluss, Eröffnung, Mittelwert usw.). |

## Hinweise im Vergleich zur MQL5-Version
- MetaTrader-Money-Management-Helfer stützten sich auf die globale Trade-Historie. Die C#-Version verfolgt abgeschlossene Trades lokal und wendet dieselbe Triggerlogik an.
- Die Lot-Größe wird direkt als Strategievolumen ausgedrückt. Passen Sie `NormalVolume`/`ReducedVolume` an die Zielmenge für Ihre Plattform an.
- Zeitfenster werden mit `TimeSpan`-Werten konfiguriert. Wenn `WindowStart` gleich `WindowEnd` ist, wird der Handel deaktiviert (entspricht dem Null-Breite-Fenster-Verhalten des ursprünglichen Skripts).
- Die Strategie geht von vollständigen Positionsumkehrungen aus und behält keine Teilpositionen aus früheren Signalen.
- Nicht unterstützte Glättungstypen (JJMA, JurX, ParMA, T3, VIDYA, AMA) verwenden standardmäßig EMA. Erwägen Sie, `CreateMovingAverage` zu erweitern, wenn Sie eine bestimmte Alternative benötigen.

## Verwendungstipps
1. Wählen Sie einen Kerzentyp, der dem Indikator-Zeitrahmen in MetaTrader entspricht (Standard: H4).
2. Passen Sie Stop-Loss- und Take-Profit-Abstände basierend auf der Instrument-Tick-Größe an, um die punktbasierten Werte des ursprünglichen EA anzunähern.
3. Optimieren Sie die Money-Management-Trigger, um die Asset-Volatilität und Ihre Risikotoleranz widerzuspiegeln.
4. Überwachen Sie das Indikatorverhalten auf einem Chart (obere/untere Kanallinien), um zu bestätigen, dass der rekonstruierte Indikator die Erwartungen vor dem Live-Trading erfüllt.
