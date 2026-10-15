# CH2010 Structure Multi-Zeitrahmen-Ausbruchsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das Verhalten des ursprünglichen **ch2010structure.mq5**-Experten, indem sie mehrere Forex-Paare auf zwei Zeitrahmen verfolgt. Jedes Instrument überwacht die Tageskerze, um einen Richtungsbias zu bestimmen, und beobachtet dann 30-Minuten-Kerzen auf der Suche nach Ausbrüchen jenseits des vorherigen Tagesbereichs. Marktpositionen werden eröffnet, wenn der Ausbruch mit dem Tagestrend übereinstimmt, und mit schützenden Stop-Loss- und Take-Profit-Niveaus geschlossen.

## Kernlogik

1. **Tägliche Bias-Erkennung**  
   * Die Strategie abonniert Tageskerzen für USDCHF, GBPUSD, AUDUSD, USDJPY und EURGBP.  
   * Wenn eine Tageskerze endet, definiert die Schluss/Eröffnungs-Beziehung den Bias: bullisch, bärisch oder neutral.  
   * Das tägliche Hoch, Tief und Schluss werden zusammen mit dem Sessionsdatum gespeichert, damit die Intraday-Logik bestätigen kann, dass die gleiche Session gehandelt wird.

2. **Intraday-Ausbruchsausführung**  
   * 30-Minuten-Kerzen werden ausgewertet, sobald sie schließen.  
   * Wenn der Schluss über dem vorherigen Tageshoch plus einem konfigurierbaren Puffer liegt und der Bias nicht bärisch ist, wird ein Long-Trade ausgelöst.  
   * Wenn der Schluss unter dem vorherigen Tagestief minus dem Puffer liegt und der Bias nicht bullisch ist, wird ein Short-Trade ausgelöst.  
   * Pro Instrument und Tag kann nur ein Long- und ein Short-Ausbruch aktiviert werden, um übermäßiges Trading zu vermeiden.

3. **Risikomanagement inspiriert von den ursprünglichen Helper-Funktionen**  
   * Volumina werden zwischen `MinTradeVolume` und `MaxTradeVolume` begrenzt und die aggregierte Position über alle Instrumente wird durch `MaxAggregateVolume` eingeschränkt.  
   * Jede gefüllte Position berechnet sofort absolute Stop-Loss- und Take-Profit-Niveaus unter Verwendung prozentualer Offsets vom Einstiegspreis.  
   * Positionen werden über Marktorders geschlossen, sobald der Stop oder das Ziel erreicht wird; wiederholte Exit-Orders werden durch den `ExitInProgress`-Flag verhindert.

4. **Zustandsverfolgung**  
   * Für jedes Instrument verfolgt die Strategie seine eigenen Tagesniveaus, letzte bekannte Position, Einstiegsseite, Exit-Orders und Ausbruchs-Flags in einem `InstrumentContext`.  
   * Dies ermöglicht den Multi-Symbol-Workflow, ohne benutzerdefinierte Sammlungen außerhalb der Kontextklasse pflegen zu müssen.

## Strategie-Parameter

| Parameter | Beschreibung |
| --- | --- |
| `TradeVolume` | Basis-Volumen für neue Einstiege, unterliegt den Volumengrenzen. |
| `MinTradeVolume` und `MaxTradeVolume` | Grenzen, die den ursprünglichen MQL-Risikofilter spiegeln. |
| `MaxAggregateVolume` | Maximale Summe absoluter Positionen über alle gehandelten Paare. |
| `StopLossPercent` | Schutzstopp-Offset in Prozent vom erkannten Einstiegspreis. |
| `TakeProfitPercent` | Take-Profit-Offset in Prozent vom erkannten Einstiegspreis. |
| `BreakoutBufferPercent` | Prozentsatz des vorherigen Tagesbereichs, der zu Ausbruchsauslösern hinzugefügt wird. |
| `DailyCandleType` | DataType zum Anfordern der höheren Zeitrahmen-Kerzen. |
| `IntradayCandleType` | DataType zum Anfordern der Ausführungszeitrahmen-Kerzen. |
| `UsdChfSecurity` .. `EurGbpSecurity` | Wertpapier-Objekte für die standardmäßig überwachten fünf Forex-Symbole. |

## Erforderliche Daten

* Tageskerzen für jedes konfigurierte Symbol (Standard: 1-Tages-Zeitrahmen).  
* Intraday-Kerzen (Standard: 30 Minuten) für dieselben Symbole.  
* Echtzeit-Order-Routing zum Einreichen von Marktorders für jedes Wertpapier.

## Verwendungshinweise

1. Die fünf Wertpapier-Parameter vor dem Start der Strategie konfigurieren. Sie können bei Bedarf durch andere Instrumente ersetzt werden.  
2. Portfolio und Connector wie in anderen StockSharp-Strategien einrichten.  
3. Optional den Ausbruchspuffer oder Risikoparameter anpassen, um die Kontraktspezifikationen des Ziel-Brokers zu berücksichtigen.  
4. Die Strategie starten. Sie wird automatisch beide Kerzenströme für jedes Instrument abonnieren, die Tagesstruktur protokollieren und auf gültige Intraday-Ausbrüche warten.  
5. Das Log auf Einträge wie `Daily candle captured` und `Enter Buy` überwachen, um den Entscheidungsfluss zu überprüfen.

## Unterschiede vs. dem ursprünglichen MQL-Experten

* Ausstehende Orders werden durch sofortige Marktorders ersetzt, sobald die Ausbruchsbedingung beobachtet wird. Dies hält die Logik kompatibel mit der StockSharp High-Level-API und bewahrt die Idee, die Exposition zu begrenzen und nur einmal pro Richtung täglich zu reagieren.  
* Volumenbeschränkungen aus dem `DebugOrderSend`-Helper wurden in Parameter angepasst, die einzelne Trade-Größen und die Gesamtexposition begrenzen.  
* Umfangreiche Protokollierung wurde hinzugefügt, um Tagesniveaus, Einstiegsgründe und Exit-Auslöser in englischen Kommentaren für einfacheres Debugging in StockSharp anzuzeigen.

## Haftungsausschluss

Dieses Beispiel dient zu Bildungszwecken. Parameter und Wertpapiere sollten überprüft und angepasst werden, bevor die Strategie im Produktionshandel eingesetzt wird.
