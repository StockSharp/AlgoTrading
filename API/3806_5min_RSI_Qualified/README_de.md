# 5 Minuten RSI Qualifizierte Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **5min RSI Qualified Strategy** ist eine direkte Umsetzung des MetaTrader Expert Advisors „5min_rsi_qual_01a“. Der ursprüngliche Roboter suchte anhand eines 28-Perioden-Relative-Stärke-Index (RSI) nach Erschöpfung in Fünf-Minuten-Kerzen. Sobald der Oszillator für eine vordefinierte Anzahl von Balken in einer extremen Zone blieb, eröffnete EA eine konträre Position und fügte einen Trailing Stop hinzu, der dem Schluss der vorherigen Kerze folgte. Der StockSharp-Port behält die genaue Bestätigungslogik, Preisversätze und Einzelpositionsbeschränkung bei und verlässt sich dabei auf das High-Level-Kerzenabonnement API.

Standardmäßig arbeitet die Strategie mit Fünf-Minuten-Kerzen, der Parameter `CandleType` akzeptiert jedoch jeden anderen vom Instrument unterstützten Zeitrahmen. Alle Indikatorschwellenwerte und Stoppentfernungen werden weiterhin in MetaTrader „Punkten“ ausgedrückt, sodass Benutzer ihre getesteten Konfigurationen ohne weitere Anpassungen erneut anwenden können.

## Handelslogik

1. **RSI-Berechnung** – Bei jeder fertigen Kerze wird ein 28-Perioden-RSI aktualisiert. Nur abgeschlossene Kerzen werden verarbeitet, um der Referenz MQL4 `Close[1]` zu entsprechen.
2. **Qualifikationszähler** – Zwei Zähler verfolgen, wie viele aufeinanderfolgende Kerzen der RSI über dem Überkauft-Schwellenwert (`UpperThreshold`) oder unter dem Überverkauft-Schwellenwert (`LowerThreshold`) geblieben ist. Dies spiegelt die MQL-Schleife wider, die die letzten 12 Balken überprüft hat.
3. **Eintrittsbedingungen** – Wenn keine Position offen ist und der Überkauft-Zähler `QualificationLength` erreicht, wird die Strategie zum Marktpreis verkauft. Wenn umgekehrt der überverkaufte Zähler den Bedarf erreicht, kauft er zum Marktpreis. Dies reproduziert das Verhalten von EA, bei dem höchstens ein Trade pro Symbol gehalten wird.
4. **Trailing Stop** – Während eine Position aktiv ist, wird das Stop-Level bei jeder abgeschlossenen Kerze neu berechnet, wobei der vorherige Schlusskurs minus/plus `StopLossPoints` in den absoluten Preis umgewandelt wird. Der Stop bewegt sich nur in Richtung des Handels, genau wie die `OrderModify`-Aufrufe im Originalcode.
5. **Anfangsstopp** – Nach jeder Füllung legt die Strategie den Anfangsstopp mit `InitialStopPoints` fest. Wenn der Anfangswert enger als die Nachlaufdistanz ist, wird er von der Nachlauflogik nicht gelockert, wodurch das MetaTrader-Verhalten erhalten bleibt, bei dem der Anfangsstopp näher als die Nachlaufdistanz liegen könnte.

## Risikomanagement

- Stoppentfernungen werden in MetaTrader Punkten definiert, um mit EA übereinzustimmen. Sie werden mithilfe des `PriceStep` des Instruments (oder `MinStep`, wenn der primäre Schritt nicht verfügbar ist) in absolute Preiserhöhungen umgewandelt.
- Bei der Strategie handelt es sich niemals um Pyramidengeschäfte. Eine neue Position wird erst eröffnet, wenn die vorherige vollständig geschlossen wurde.
- `StartProtection()` wird beim Start aufgerufen, damit die Schutzinfrastruktur von StockSharp mit den manuell verwalteten Stoppebenen synchron bleibt.

## Parameter

| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `RsiPeriod` | RSI Lookback-Länge. | `28` |
| `QualificationLength` | Anzahl aufeinanderfolgender Kerzen, bei denen RSI im Extrembereich bleiben muss, bevor ein Signal bestätigt wird. | `12` |
| `UpperThreshold` | RSI-Level, das ein rückläufiges Setup qualifiziert. | `55` |
| `LowerThreshold` | RSI-Level, das ein bullisches Setup qualifiziert. | `45` |
| `StopLossPoints` | Trailing-Stop-Distanz in MetaTrader Punkten. Für jede Kerze in den absoluten Preis umgerechnet. Auf `0` setzen, um das Nachstellen zu deaktivieren. | `21` |
| `InitialStopPoints` | Der anfängliche Schutzstoppabstand in MetaTrader Punkten wird unmittelbar nach der Einfahrt angewendet. Auf `0` einstellen, um den ersten Stopp zu überspringen. | `11` |
| `CandleType` | Kerzentyp, der zur Signalauswertung verwendet wird (standardmäßig 5 Minuten). | `5-minute time frame` |

## Nutzungsrichtlinien

- Stellen Sie sicher, dass der Preisschritt des Instruments mit der Punktgröße übereinstimmt, die während der MetaTrader-Optimierung verwendet wurde. Bei fünfstelligen FX-Symbolen entspricht ein Punkt 0,00010 (einem Pip), sodass die Standardabstände die 11/21-Punkt-Offsets von EA reproduzieren.
- Da die Methode konträr ist, sind Signale in Ranging-Märkten zuverlässiger. Erwägen Sie eine Erweiterung der Schwellenwerte oder eine Erhöhung von `QualificationLength` für Trend-Assets.
- Die Strategie verwendet die Eigenschaft der Basisklasse `Volume` für die Auftragsgröße. Konfigurieren Sie es in der Benutzeroberfläche oder per Code, bevor Sie mit der Strategie beginnen.
- Dank der Flags `SetCanOptimize()` kann eine Optimierung der RSI-Schwellenwerte, der Qualifikationslänge und der Stoppdistanzen durchgeführt werden.

## Konvertierungshinweise

- Die Kerzenbehandlung, die RSI-Berechnung und die Ein-Positions-Beschränkung spiegeln die MetaTrader-Implementierung wider. Es wurden keine zusätzlichen Filter eingeführt.
- Der Trailing Stop aktualisiert das Stop-Level mit dem Schlusskurs der vorherigen Kerze, genau wie die MQL4 `Close[1]`-Logik, und stellt sicher, dass beide Versionen bei einer Umkehr zum gleichen Preis aussteigen.
- Fehlerprüfungen aus dem MQL4-Skript (Balkenanzahl, freie Marge) werden absichtlich weggelassen, da StockSharp die Datenbereitschaft und Portfolioverfügbarkeit intern verwaltet.
