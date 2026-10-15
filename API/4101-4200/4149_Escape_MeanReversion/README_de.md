# Escape-Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Escape-Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `escape.mq4`. Der ursprüngliche Roboter handelt auf einem Fünf-Minuten-Chart und reagiert auf Gelegenheiten zur Mean-Reversion: Er kauft, wenn der Preis unter einen kurzen gleitenden Durchschnitt fällt, und verkauft, wenn der Preis über einen anderen schnellen Durchschnitt steigt. Jede Position ist durch einen Take-Profit und Stop-Loss mit fester Distanz geschützt, ausgedrückt in MetaTrader Punkten. Die C#-Implementierung behält die gleiche minimalistische Logik bei und stellt alle einstellbaren Abstände als Strategieparameter bereit.

## Handelslogik
1. **Initialisierung**
   - Abonnieren Sie die konfigurierbare `CandleType`-Serie (standardmäßig Fünf-Minuten-Kerzen).
   - Erstellen Sie zwei `SimpleMovingAverage`-Indikatoren mit den Längen 5 und 4, die mit Kerzeneröffnungspreisen gefüttert werden.
   - Berechnen Sie das MetaTrader `Point`-Äquivalent aus `Security.PriceStep`; Dieser Wert wird wiederverwendet, um Pip-Abstände in absolute Preise umzuwandeln.

2. **Verarbeitung pro Kerze**
   - Über `SubscribeCandles(...).WhenCandlesFinished(ProcessCandle)` werden nur fertige Kerzen verarbeitet.
   - Die Strategie prüft zunächst, ob eine bestehende Position ihren Stop-Loss oder Take-Profit erreicht, indem sie das Kerzenhoch/-tief mit den aufgezeichneten Ausstiegsniveaus vergleicht. Beim Durchbrechen eines Levels wird die Position mit einer Marktorder geschlossen und doppelte Exit-Orders werden durch interne Flags verhindert.
   - Wenn das Konto flach ist, vorherige Werte der beiden SMAs verfügbar sind, der Handel erlaubt ist und das Portfolio über genügend Kapital (`Portfolio.CurrentValue >= MinimumMarginPerLot * TradeVolume`) verfügt, wertet die Strategie Einträge aus:
     * **Langer Einstieg** – Der aktuelle Schlusskurs liegt unter den vorherigen 5-Perioden SMA der Eröffnungen.
     * **Kurzer Einstieg** – Der aktuelle Schlusskurs liegt über den vorherigen 4-Perioden-Öffnungen SMA.
   - Wenn ein Signal ausgelöst wird, werden die Stop-Loss- und Take-Profit-Werte ausgehend vom Kerzenschluss anhand der konfigurierten Punktabstände berechnet und zur späteren Überwachung gespeichert.

3. **Risikomanagement**
   - `TradeVolume` definiert die Losgröße jeder Marktorder.
   - `MinimumMarginPerLot` entspricht in etwa der `AccountFreeMargin`-Prüfung von MetaTrader. Wenn der verfügbare Portfoliowert zu klein ist, wird die Eingabe übersprungen und eine Diagnosemeldung protokolliert.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `LongTakeProfitPoints` | `10` | Take-Profit-Distanz für Long-Positionen in MetaTrader Punkten. Auf `0` setzen, um das Ziel zu deaktivieren. |
| `ShortTakeProfitPoints` | `10` | Take-Profit-Distanz für Short-Positionen in MetaTrader Punkten. Auf `0` setzen, um das Ziel zu deaktivieren. |
| `LongStopLossPoints` | `1000` | Stop-Loss-Distanz für Long-Positionen in MetaTrader Punkten. Auf `0` einstellen, um den Schutzstopp zu deaktivieren. |
| `ShortStopLossPoints` | `1000` | Stop-Loss-Distanz für Short-Positionen in MetaTrader Punkten. Auf `0` einstellen, um den Schutzstopp zu deaktivieren. |
| `TradeVolume` | `0.2` | Lotgröße, die beim Senden von Marktaufträgen verwendet wird. |
| `MinimumMarginPerLot` | `500` | Ungefährer Kapitalbedarf pro Lot vor der Eröffnung eines neuen Handels. |
| `CandleType` | Zeitrahmen von fünf Minuten | Kerzenserie, die die Aktualisierung von Indikatoren und die Signalgenerierung vorantreibt. |

## Implementierungshinweise
- Indikatoren werden in `ProcessCandle` manuell mit offenen Kerzenpreisen aktualisiert, sodass die gespeicherten Werte immer den vorherigen Balken darstellen (was die in `iMA` verwendeten `shift=1`-Argumente widerspiegelt).
- Exit-Ebenen werden in Dezimalfeldern verfolgt, anstatt zusätzliche Sammlungen zu erstellen, wodurch die allgemeinen API-Richtlinien erfüllt werden.
- Stopps und Ziele werden anhand der Kerzenextreme bewertet. Da nur OHLC-Daten verfügbar sind, wird die Stoppprüfung vor dem Take-Profit durchgeführt, um die Orderpriorität von MetaTrader so genau wie möglich nachzubilden.
- Die Strategie zeichnet Kerzen sowohl mit gleitenden Durchschnitten als auch mit eigenen Trades zusammen, wenn ein Chartbereich verfügbar ist, was die visuelle Validierung vereinfacht.

## Unterschiede zur MetaTrader-Version
- MetaTrader fügt Stop-Loss- und Take-Profit-Orders direkt an Tickets an. Der StockSharp-Port reproduziert sie, indem er Kerzenhochs und -tiefs überwacht und Marktausgänge sendet; Die Ausführungsreihenfolge innerhalb eines Balkens kann nicht garantiert werden, wenn beide Ebenen innerhalb desselben Balkens berührt werden.
- Die Einstiegspreise werden vom Kerzenschluss abgeleitet, der das Signal ausgelöst hat, und nicht vom genauen Geld-/Briefkurs, der von MetaTrader verwendet wird. Daher müssen Slippage und Spread-Handhabung auf Connector-Ebene konfiguriert werden.
- Der Schutz `AccountFreeMargin()` wird durch `Portfolio.CurrentValue` angenähert. Benutzer mit detaillierteren Margin-Modellen können `HasSufficientMargin` bei Bedarf erweitern.
- Kosmetische MQL-Einstellungen wie Farben, Töne und Slippage werden weggelassen; Die StockSharp-Version konzentriert sich auf die Kernhandelslogik.
