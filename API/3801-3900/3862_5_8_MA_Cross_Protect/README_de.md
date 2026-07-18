# 5/8 EMA Cross-Protect-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **5/8 EMA Cross Protect-Strategie** repliziert den MetaTraderExpert Advisor `5_8macrossv2.mq4`, indem sie zwei konfigurierbare gleitende Durchschnitte auf demselben Symbol vergleicht. Ein zinsbullischer Crossover des schnellen gleitenden Durchschnitts über den langsamen eröffnet Long-Positionen, während ein bärischer Crossover Short-Positionen eröffnet. Die portierte Version folgt StockSharp-Mustern auf hoher Ebene und fügt optionales Take-Profit-, Stop-Loss- und Trailing-Stop-Management hinzu.

## Handelslogik
- Für das ausgewählte Kerzenabonnement werden zwei gleitende Durchschnitte berechnet. Standardmäßig wird ein exponentieller MA mit 5 Perioden für Schlusskurse mit einem exponentiellen MA mit 8 Perioden für Eröffnungskurse verglichen.
- Wenn der schnelle MA den langsamen MA der zuletzt beendeten Kerze überschreitet, öffnet sich die Strategie oder kehrt sich in eine Long-Position um. Wenn eine Short-Position aktiv ist, wird ihr Volumen in die neue Marktkauforder zur Umkehrung der Richtung einbezogen.
- Wenn der schnelle MA den langsamen MA unterschreitet, öffnet sich die Strategie oder kehrt sich in eine Short-Position um, wobei dieselbe Volumennormalisierungslogik verwendet wird.
- MA-Verschiebungsparameter emulieren den ursprünglichen horizontalen Versatz. Positive Werte verzögern das Signal um so viele geschlossene Kerzen; Negative Werte werden auf Null gerundet, da vorwärts verschobene Werte in Echtzeitdaten nicht verfügbar sind.

## Risikomanagement
- **Take-Profit**- und **Stop-Loss-Abstände werden in Pips (Preisschritten) ausgedrückt. Wenn eine Long-Position eröffnet wird, werden Schutzniveaus über bzw. unter dem Einstiegspreis platziert; die Logik spiegelt sich für Shorts wider.
- **Trailing Stop** (auch in Pips) verschärft ständig das Schutzniveau, wenn sich der Preis zugunsten der Position bewegt. Bei Long-Positionen bewegt sich der Trailing Stop nur nach oben; Bei Shorts bewegt es sich nur nach unten.
- Wenn bei einer fertigen Kerze eine Schutzbedingung erfüllt ist (hohe Treffer: Take-Profit, niedrige Treffer Stop-Loss oder Trailing-Level), verlässt die Strategie die Position mit einer Marktorder und setzt ihren internen Zustand zurück.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `0.1` | Auftragsvolumen für Neuzugänge. Die Strategie addiert beim Umkehren die absolute Positionsgröße. |
| `TakeProfitPips` | `decimal` | `40` | Abstand vom Einstieg in Pips zum Schließen der Position mit Gewinn. Zum Deaktivieren auf `0` setzen. |
| `StopLossPips` | `decimal` | `0` | Entfernung vom Einstieg in Pips für schützenden Stop-Loss. Zum Deaktivieren auf `0` setzen. |
| `TrailingStopPips` | `decimal` | `0` | Trailing-Stop-Distanz in Pips. Zum Deaktivieren auf `0` setzen. |
| `FastPeriod` | `int` | `5` | Periode des schnellen gleitenden Durchschnitts. |
| `FastShift` | `int` | `-1` | Horizontale Verschiebung für den schnellen MA. Negative Werte werden in diesem Port als Null behandelt. |
| `FastMethod` | `MovingAverageMethod` | `Exponential` | Glättungsalgorithmus für den schnellen MA (einfach, exponentiell, geglättet, linear gewichtet). |
| `FastPrice` | `AppliedPrice` | `Close` | Für den schnellen MA verwendeter Kerzenpreis. |
| `SlowPeriod` | `int` | `8` | Periode des langsamen gleitenden Durchschnitts. |
| `SlowShift` | `int` | `0` | Horizontale Verschiebung für den langsamen MA. |
| `SlowMethod` | `MovingAverageMethod` | `Exponential` | Glättungsalgorithmus für den langsamen MA. |
| `SlowPrice` | `AppliedPrice` | `Open` | Für den langsamen MA verwendeter Kerzenpreis. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(30).TimeFrame()` | Für Berechnungen verwendete Kerzenreihe. |

## Notizen
- Durch die Konvertierung konzentriert sich die Logik auf fertige Kerzen, um vorzeitige Signale zu vermeiden.
- Trailing Stops und Gewinnziele werden mit `Security.PriceStep` berechnet; wenn ein Symbol es nicht definiert, bleiben die Risikoparameter inaktiv.
- Die Python-Version wird aufgrund der Aufgabenanforderungen absichtlich weggelassen.
