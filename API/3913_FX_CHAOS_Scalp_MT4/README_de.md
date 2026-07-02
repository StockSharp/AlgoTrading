# FX-CHAOS Scalp MT4-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die FX-CHAOS Scalp MT4-Strategie ist eine direkte Portierung des Expert Advisors MetaTrader 4, der einen Awesome Oscillator-Filter mit auf Fraktalen basierenden ZigZag-Levels kombiniert. Die StockSharp-Version behält das Multi-Timeframe-Design des ursprünglichen Systems bei: Stündliche Kerzen erzeugen Handelssignale, während tägliche Kerzen eine stärkere Zeitrahmen-Tendenz bieten. Zwei eingebettete Tracker rekonstruieren den Indikator „ZigZag on Fractals“, indem sie Fünf-Kerzen-Muster scannen und abwechselnde Swing-Hochs und -Tiefs aufzeichnen.

## Handelsablauf
1. **Datenerfassung**
   - Stündliche Kerzen versorgen die primäre Ausführungslogik und Risikokontrolle.
   - Tägliche Kerzen aktualisieren den als Trendfilter verwendeten langfristigen ZigZag-Swing.
   - Der Awesome Oscillator (5, 34) wird in der stündlichen Reihe durch den High-Level-Indikator API bewertet.
2. **Zick-Zack-Rekonstruktion**
   - Jede fertige Kerze wird in einem verschiebbaren Fenster mit fünf Elementen aufbewahrt.
   - Wenn die mittlere Kerze ein Aufwärts-Fraktal bildet, speichert der Tracker das Hoch der Kerze als letzten Schwung und ändert die Richtung auf „Aufwärts“. Ein Down-Fraktal bewirkt dasselbe für Tiefs.
   - Aufeinanderfolgende Schwankungen in die gleiche Richtung werden nur dann ersetzt, wenn das neue Extrem ausgeprägter ist, was die Pufferlogik des MT4-Indikators nachahmt.
3. **Signalerkennung**
   - Der Breakout-Puffer fügt zwei Preisschritt-Offsets zum Hoch/Tief der vorherigen Stunde hinzu und spiegelt die `2*Point`-Auffüllung im Originalcode wider.
   - Bei Long-Einstiegen muss die Kerze unterhalb des gepufferten Hochs öffnen, darüber schließen, unter dem letzten stündlichen ZigZag-Schwung bleiben, über dem letzten Tagesschwung schließen und den Awesome Oscillator negativ halten.
   - Kurze Einträge spiegeln die Bedingungen unter Verwendung der gepufferten niedrigen, oberen ZigZag-Ebene und positiven Oszillatorwerte wider.
4. **Auftragsausführung und Konfliktlösung**
   - Gegensätzliche Positionen werden geschlossen, bevor ein neuer Auftrag gesendet wird, sodass die Strategie niemals gleichzeitige Long- und Short-Trades vorsieht.
   - Der ausgeführte Schlusskurs wird gespeichert, um Stop-Loss- und Take-Profit-Abstände bei nachfolgenden Kerzen abzuleiten.

## Risikomanagement
- Stop-Loss- und Take-Profit-Schwellenwerte sind optional; ein Wert von `0` deaktiviert die entsprechende Regel.
- Am Ende jeder fertigen Kerze prüft die Strategie, ob die Kerzenspanne den konfigurierten Stopp oder das konfigurierte Ziel berührt hat und schließt die Position, wenn das Niveau durchbrochen wurde.
- Wenn ein entgegengesetzter Ausbruch auftritt, wird die Position zuerst liquidiert, dann wird der neue Handel auf derselben Kerze gesendet, um die Einzelpositionsregel beizubehalten.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Volume` | Handelsvolumen in Lots, angewendet auf jede Marktorder. |
| `Stop Loss (pts)` | Abstand in Punkten für den Schutzstopp. Multipliziert mit der Wertpapierpreisstufe. Zum Deaktivieren auf `0` setzen. |
| `Take Profit (pts)` | Abstand in Punkten für das Gewinnziel. Multipliziert mit der Preisstufe. Zum Deaktivieren auf `0` setzen. |
| `Breakout Buffer` | Zusätzliche Punkte, die zum vorherigen Kerzenextremum hinzugefügt wurden, bevor Ausbrüche getestet wurden. Der Standardwert reproduziert das in MT4 verwendete Polster `2*Point`. |
| `Spread (pts)` | Durchschnittlicher Spread in Punkten, der bei Kaufsignalen zur Ausbruchsschwelle addiert wird, sodass der Einstieg `2*Point + spread` von MT4 widerspiegelt. |
| `Trading Candle` | Primärer Zeitrahmen für Einträge (standardmäßig eine Stunde). |
| `Daily Candle` | Für den ZigZag-Filter wird ein höherer Zeitrahmen verwendet (standardmäßig ein Tag). |

## Implementierungshinweise
- Die Strategie basiert auf den übergeordneten `SubscribeCandles` API und `BindEx`, um die direkte Arbeit mit Indikatorpuffern zu vermeiden und dabei die Repository-Richtlinien zu respektieren.
- Der von `Security.PriceStep` abgerufene Preisschritt wird verwendet, um in Punkten ausgedrückte Parameterwerte in absolute Preisabstände umzuwandeln. Wenn dem Instrument ein Schritt fehlt, fällt der Code auf `1` zurück.
- Beide ZigZag-Tracker werden am `OnReseted` zurückgesetzt und unterbrechen den Handel, bis sie genügend Kerzen angesammelt haben, um den ersten Schwung zu bestimmen. Dies verhindert vorzeitige Einträge, wenn historischer Kontext fehlt.
- Bei der Diagrammdarstellung werden die stündlichen Kerzen, der Awesome Oscillator und die Strategie-Trades gezeichnet, um den Vergleich der StockSharp-Implementierung mit der MT4-Vorlage zu erleichtern.
