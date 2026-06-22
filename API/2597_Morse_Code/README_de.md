# Morse Code Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Morse Code Strategie repliziert den ursprünglichen MetaTrader 5-Experten, der jede abgeschlossene Kerze als "Strich" oder "Punkt" behandelt. Eine bullische Kerze (Schlusskurs größer oder gleich der Eröffnung) wird als `1` kodiert, während eine bärische Kerze (Schlusskurs kleiner oder gleich der Eröffnung) als `0` kodiert wird. Die Strategie durchsucht die neueste Sequenz abgeschlossener Kerzen und vergleicht sie mit einer vom Benutzer ausgewählten binären Maske. Wenn die letzten Kerzen genau der konfigurierten Sequenz entsprechen, öffnet die Strategie eine Position in der gewählten Richtung und hängt sofort sowohl eine Take-Profit- als auch eine Stop-Loss-Order in Pips an.

Die Implementierung basiert ausschließlich auf High-Level-StockSharp-APIs: Kerzenabonnements liefern Daten, Binding übernimmt die Ereignisübermittlung, und das eingebaute Schutzmodul verwaltet Exits. Keine benutzerdefinierten Sammlungen oder direkter Indikatorwertzugriff erforderlich, was die Logik präzise und robust hält.

## Musterlogik
- Kerzen werden nur nach vollständigem Abschluss ausgewertet (`CandleStates.Finished`).
- Jede Kerze wird zu einer binären Ziffer:
  - `1` – die Kerze ist bullisch oder neutral (`Close >= Open`).
  - `0` – die Kerze ist bärisch oder neutral (`Close <= Open`). Doji-Kerzen entsprechen beiden Ziffern, genau wie im ursprünglichen Experten.
- Die Maske wird aus der `MorsePatternMasks`-Enumeration ausgewählt. Sie enthält jede binäre Sequenz der Länge 1 bis 5, die in der MT5-Version erschien (zum Beispiel `000`, `1011`, `11111`).
- Die Strategie hält ein gleitendes Fenster der neuesten Kerzen. Wenn das neueste Fenster der Maske entspricht, wird das Eintrittssignal ausgelöst.

Dieses Verhalten spiegelt die MT5-Implementierung wider, die `CopyRates` aufrief und jede Bar zeichenweise mit dem Muster-String verglich.

## Handelsworkflow
1. Den konfigurierten Kerzentyp abonnieren und warten, bis genügend Bars akkumuliert wurden, um die Maskenlänge abzudecken.
2. Für jede abgeschlossene Kerze:
   - Die internen Masken aktualisieren, die die Kerze als bullisch, bärisch oder neutral klassifizieren.
   - Weitere Prüfungen ignorieren, bis mindestens so viele Kerzen verarbeitet wurden, wie die Maske erfordert.
   - Wenn die neuesten Kerzen genau der ausgewählten Maske entsprechen, die gewünschte Richtung prüfen.
   - Eine Marktorder in Richtung des Signals senden (`BuyMarket` oder `SellMarket`). Wenn eine entgegengesetzte Position existiert, schließt die Strategie sie zuerst durch Erhöhung des Ordervolumens und reproduziert damit das Verhalten des ursprünglichen Expertenberaters.
3. `StartProtection` hängt sofort einen Stop-Loss und Take-Profit in Preiseinheiten an. Schutzorders werden von der StockSharp-Engine mit Marktausstiegen gehandhabt, um verpasste Fills zu vermeiden.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 5-Minuten-Kerzen (`TimeSpan.FromMinutes(5).TimeFrame()`) | Datentyp für den Aufbau der Morse-Sequenz. |
| `Pattern` | `_0` (`"0"`) | Binäre Maske zum Abgleich mit den neuesten Kerzen. Werte kommen direkt aus `MorsePatternMasks`. |
| `Direction` | `Sides.Buy` | Ob eine Long- (`Buy`) oder Short- (`Sell`) Position geöffnet werden soll, wenn das Muster erscheint. |
| `TakeProfitPips` | `50` | Abstand vom Einstieg zum Take-Profit in Pips. Die Strategie passt sich automatisch an 3- und 5-Dezimal-Forexkurse an, indem sie den Preisschritt mit zehn multipliziert. |
| `StopLossPips` | `50` | Abstand vom Einstieg zum Stop-Loss in Pips, mit derselben Pip-Berechnung wie oben. |
| `Volume` (Strategieeigenschaft) | benutzerdefiniert | Ordergröße in Lots/Kontrakten, entspricht `InpLot` im MT5-Experten. |

Alle Parameter unterstützen das StockSharp-Parameterfenster, können optimiert und vor dem Start der Strategie geändert werden.

## Risikomanagement
- `StartProtection` hängt beide Ziele mit preisbasierten Offsets aus den Pip-Einstellungen an. Exits werden mit Marktorders ausgeführt, sodass das Verhalten der MT5-Trade-Klasse entspricht, die Stop-Loss- und Take-Profit-Werte beim Positionseinstieg setzte.
- Da die Strategie nicht pyramidiert, wird ein neuer Trade ignoriert, solange eine bestehende Position in dieselbe Richtung offen ist. Wenn das Muster erscheint, während die entgegengesetzte Richtung gehalten wird, wird das Volumen automatisch erhöht, um die Position umzudrehen.
- Standard-StockSharp-Logging meldet jeden Einstieg im Strategiejournal.

## Verwendungshinweise
- Die binären Masken sind absichtlich kurz (bis zu fünf Kerzen), um die Logik der ursprünglichen Idee treu zu halten. Erwägen Sie, mehrere Mustermappen durch Portfolio-Orchestrierung zu kombinieren, wenn ein reicheres Vokabular benötigt wird.
- Die Pip-Konvertierung basiert auf dem Instrumentpreisschritt. Für exotische Symbole mit nicht standardmäßigen Inkrementen können Sie `TakeProfitPips` und `StopLossPips` manuell anpassen.
- Die Strategie filtert nicht nach Tageszeit oder Volatilität. Sie können sie in eine übergeordnete Strategie einbetten, die Sitzungen oder zusätzliche Indikatoren handhabt, wenn erforderlich.
- Beim Testen stellen Sie sicher, dass die `Volume`-Eigenschaft der erwarteten Lotgröße entspricht. Der StockSharp-Tester verwendet dieselben Schutzfunktionen und den Orderfluss wie der Live-Modus.

## Musterreferenz
Beispiele für Enumerationswerte:
- `_0` → `"0"` (einzelne bärische Kerze)
- `_5` → `"11"` (zwei bullische Kerzen in Folge)
- `_20` → `"0110"` (bärisch-bullische Sequenz, die ein Zig-Zag bildet)
- `_33` → `"00011"` (drei bärische Kerzen gefolgt von zwei bullischen)
- `_61` → `"11111"` (fünf aufeinanderfolgende bullische Kerzen)

Jede der 62 Masken kann aus dem Parameterfenster ausgewählt werden, um die genaue Morse-Code-Signatur zu reproduzieren, die der Handelsplan erfordert.
