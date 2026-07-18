# MARE5.1 Shift Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die MARE5.1-Strategie ist eine C#-Portierung des MetaTrader 4 Expert Advisors `MARE5_1.mq4`. Der ursprüngliche Roboter handelte mit M1-Daten und stützte sich auf ein Paar einfacher gleitender Durchschnitte, die bei drei verschiedenen historischen Offsets ausgewertet wurden, um Regimeänderungen zu erkennen. Diese StockSharp-Implementierung reproduziert das Verhalten mit konfigurierbaren Parametern, fügt Schutzaufträge im MetaTrader-Stil hinzu und stellt einen detaillierten Handelsfensterfilter bereit.

## Handelslogik
1. **Marktdaten**
   - Ein einzelnes Kerzenabonnement, definiert durch `CandleType` (Standard: 1 Minute), speist die Berechnungen.
   - Jede Kerze wird erst nach dem Schließen verarbeitet, um die Verwendung halbgeformter Balken zu vermeiden.
2. **Indikatoren**
   - Zwei `SimpleMovingAverage`-Instanzen repräsentieren die schnelle (`FastPeriod`) und die langsame (`SlowPeriod`) Komponente.
   - Beide Durchschnittswerte werden um `MovingAverageShift` nach vorne verschoben, genau wie das Argument `ma_shift` in der Funktion MQL `iMA`.
   - Zusätzliche verzögerte Kopien jedes Durchschnitts werden mit Verschiebungen von `MovingAverageShift + 2` und `MovingAverageShift + 5` erhalten, um die ursprünglichen `iMA(..., shift=2/5)`-Aufrufe widerzuspiegeln.
3. **Signalerkennung**
   - Die Differenz zwischen den Durchschnittswerten muss mindestens eine Preisstufe (`Point` in MetaTrader ausgedrückt) überschreiten. Wenn das Instrument Null `PriceStep` hat, ist jede positive Differenz ausreichend.
   - **Setup verkaufen:**
     - Die vorherige Kerze muss bärisch sein (`Close < Open`).
     - Der aktuell verschobene langsame Durchschnitt ist größer als der schnelle Durchschnitt.
     - Zwei und fünf Kerzen zurück lag der schnelle Durchschnitt immer noch über dem langsamen Durchschnitt, was einen Momentumwechsel signalisierte.
   - **Setup kaufen:**
     - Die vorherige Kerze muss bullisch sein (`Close > Open`).
     - Der aktuell verschobene schnelle Durchschnitt ist größer als der langsame Durchschnitt.
     - Zwei und fünf Kerzen später war der langsame Durchschnitt immer noch führend, was einen Übergang von bärischen zu zinsbullischen Bedingungen bestätigte.
   - Es kann jeweils nur eine Position offen sein, wodurch die `OrdersTotal() < 1`-Bewachung der EA repliziert wird.
4. **Zeitfilter**
   - Der Handel ist nur zulässig, wenn die Schlussstunde der ausgewerteten Kerze in das `[TimeOpenHour, TimeCloseHour]`-Intervall fällt.
   - Wenn die Endstunde kürzer als die Startstunde ist, wird das Fenster als über Nacht behandelt (z. B. `22` bis `5`).

## Risikomanagement
- `StartProtection` ist mit einer Stop-Loss- und Take-Profit-Distanz konfiguriert, die mithilfe des Instruments `PriceStep` aus MetaTrader Punkten in absolute Preisversätze umgewandelt wird.
- Es ist kein Trailing Stop implementiert, da der ursprüngliche Code `TrailingStop` deklariert, ihn aber nie verwendet hat.
- Bestellungen werden mit dem durch `TradeVolume` definierten Volumen aufgegeben. Bei der Strategie handelt es sich nicht um eine Pyramiden- oder Scale-out-Positionierung.

## Parameter
| Name | Beschreibung | Standard | Notizen |
| --- | --- | --- | --- |
| `TradeVolume` | Losgröße für Markteintritte. | `7.8` | Gerundet gemäß den Austauschregeln durch den StockSharp-Konnektor. |
| `FastPeriod` | Periode des schnellen einfachen gleitenden Durchschnitts. | `13` | Steuert, wie schnell die Strategie auf Preisänderungen reagiert. |
| `SlowPeriod` | Periode des langsamen einfachen gleitenden Durchschnitts. | `55` | Bietet die längerfristige Trendreferenz. |
| `MovingAverageShift` | Auf beide gleitenden Durchschnitte angewendete Vorwärtsverschiebung. | `2` | Entspricht dem Parameter `ma_shift` der Funktion MQL `iMA`. |
| `StopLossPoints` | Schutzstoppabstand in MetaTrader Punkten. | `80` | Umgerechnet in einen absoluten Offset durch das Instrument `PriceStep`. |
| `TakeProfitPoints` | Gewinnzielentfernung in MetaTrader Punkten. | `110` | Auf `0` setzen, um den Take-Profit zu deaktivieren. |
| `TimeOpenHour` | Beginn des zulässigen Handelsfensters (Stunde, 0–23). | `8` | Bewertet anhand der Kerzenschlusszeit. |
| `TimeCloseHour` | Ende des zulässigen Handelsfensters (Stunde, 0–23). | `14` | Kann bis Mitternacht niedriger als `TimeOpenHour` sein. |
| `CandleType` | Für das Kerzenabonnement verwendeter Zeitrahmen. | `1 minute` | Jeder andere `TimeFrame()`-Wert kann angegeben werden. |

## Implementierungshinweise
- Der Indikator `Shift` wird intern verwendet, um die genauen historischen Offsets der MQL-Implementierung zu reproduzieren, ohne direkt auf Indikatorpuffer zuzugreifen.
- `IsDifferenceSatisfied` kapselt den Punkt-Schwellenwert-Vergleich und sorgt dafür, dass die Strategie mit Instrumenten mit unterschiedlichen Tick-Größen kompatibel bleibt.
- Bei der Prüfung des Handelsfensters werden Kerzenschlusszeiten verwendet. Dies ist die beste Annäherung an `Hour()` von MetaTrader, wenn nur fertige Kerzen verarbeitet werden.
- Alle Kommentare sind auf Englisch verfasst und der Code basiert ausschließlich auf dem High-Level API (`SubscribeCandles().Bind(...)`), wie in den Projektrichtlinien gefordert.

## Unterschiede im Vergleich zur MQL-Version
- Signale werden bei geschlossenen Kerzen ausgewertet, wodurch ein mögliches Neuzeichnen vermieden wird, das bei Intra-Bar-Ticks in MetaTrader auftreten könnte.
- Stop-Loss- und Take-Profit-Orders werden von `StartProtection` abgewickelt, anstatt manuell an jeden `OrderSend`-Aufruf angehängt zu werden.
- Die nicht verwendete `TrailingStop`-Eingabe wurde absichtlich weggelassen, um zu vermeiden, dass ein nicht funktionierender Parameter offengelegt wird.
- Der Zeitfilter unterstützt konstruktionsbedingt Sitzungen über Nacht, während der ursprüngliche EA implizit von `TimeOpen <= TimeClose` ausging.
