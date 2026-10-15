# EMA WMA-Risikostrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des Expertenberaters MetaTrader 4 „EMA WMA“ durch Vladimir Hlystov.
- Trades-Trendumkehrungen werden anhand der Beziehung zwischen einem exponentiellen gleitenden Durchschnitt (EMA) und einem gewichteten gleitenden Durchschnitt (WMA) ermittelt, der anhand der **Eröffnungspreise** der Kerze berechnet wird.
- Fügt automatisch Stop-Loss- und Take-Profit-Orders hinzu, die mit dem MT4-Robot identisch sind, indem der Schutzhelfer von StockSharp verwendet wird.
- Unterstützt eine risikobasierte Positionsgrößenbestimmung, die die ursprüngliche „Risiko“-Eingabe widerspiegelt und gleichzeitig eine Option für den Handel mit festem Volumen beibehält.

## Ursprüngliche Expert Advisor-Logik
- Die MT4-Version funktioniert mit jedem Symbol und Zeitrahmen und wertet Signale einmal auf einem neuen Balken aus (geschützt durch `TimeBar`).
- Indikatoren verwenden `PRICE_OPEN`, sodass die Durchschnittswerte auf den Eröffnungstick des Balkens reagieren.
- Wenn EMA unter den WMA fällt, während er zuvor darüber lag, werden alle Short-Positionen geschlossen und ein Long-Trade mit vordefinierten Stop-Loss- und Take-Profit-Abständen eröffnet.
- Wenn EMA über den WMA steigt, nachdem er darunter lag, werden alle Long-Positionen geschlossen und eine neue Short-Position eröffnet.
- Die Eingabe `risk` berechnet die Lotgröße aus der verfügbaren Marge und der Stop-Loss-Distanz.

## Handelsregeln in StockSharp
1. Abonnieren Sie die konfigurierte Kerzenserie (`CandleType`, standardmäßig 30 Minuten). Es werden nur fertige Kerzen verarbeitet, um ein Nachlackieren zu vermeiden.
2. Geben Sie die offenen Kerzenpreise in die Indikatoren EMA und WMA ein. Warten Sie, bis sich beide Indikatoren gebildet haben.
3. Erkennen Sie einen bullischen Crossover, wenn vorheriger EMA > vorheriger WMA und aktueller EMA < aktueller WMA.
   - Schließen Sie alle Short-Positionen und gehen Sie eine Long-Position ein, deren Größe den Risikoregeln entspricht.
4. Erkennen Sie einen bearischen Crossover, wenn vorheriger EMA < vorheriger WMA und aktueller EMA > aktueller WMA.
   - Schließen Sie alle Long-Positionen und gehen Sie eine Short-Position ein, deren Größe den Risikoregeln entspricht.
5. `StartProtection` erstellt Marktschutzaufträge, sodass jeder neue Trade sofort Stop-Loss- und Take-Profit-Werte erhält, ausgedrückt in Preisschritten.

## Positionsgrößenbestimmung und Risikokontrolle
- **RiskPercent** emuliert den MT4-Parameter `risk`. Das Volumen wird aus Portfolio-Eigenkapital, Stop-Loss-Distanz und Wertpapierschritt-/Schrittpreiswerten berechnet.
- Wenn Börsenmetadaten fehlen (kein Preisschritt oder Schrittpreis), greift der Algorithmus auf die Verwendung der absoluten Stoppdistanz zurück.
- Wenn `RiskPercent` auf Null gesetzt ist, erfordert die Strategie ein positives **OrderVolume** (Überschreibung des festen Volumens).
- Bestehendes Gegenrisiko wird geschlossen, bevor neue Aufträge gesendet werden, was dem MT4-Verhalten von `CLOSEORDER` und dann `OPENORDER` entspricht.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `EmaPeriod` | Periode des exponentiellen gleitenden Durchschnitts (Standard 28). |
| `WmaPeriod` | Periode des gewichteten gleitenden Durchschnitts (Standard 8). |
| `StopLossPoints` | Stop-Loss-Distanz in Instrumentenschritten (Standard 50). |
| `TakeProfitPoints` | Take-Profit-Distanz in Instrumentenschritten (Standard 50). |
| `RiskPercent` | Prozentsatz des Eigenkapitals zum Risiko pro Trade (Standard 10 %). |
| `OrderVolume` | Festes Auftragsvolumen; Verwenden Sie 0, um die risikobasierte Größenbestimmung zu aktivieren. |
| `CandleType` | Kerzendatentyp/Zeitrahmen, der für Berechnungen verwendet wird. |

## Implementierungshinweise
- EMA- und WMA-Werte werden manuell über `DecimalIndicatorValue` übertragen, um sicherzustellen, dass der Eröffnungspreis genau wie die MT4-Indikatorkonfiguration verwendet wird.
- Die Strategie basiert auf geschlossenen Kerzen zur Signalbestätigung; Dies kann die Einstiege im Vergleich zu MT4 um einen Balken verzögern, verhindert jedoch einen Look-Ahead-Bias.
- Schutzaufträge werden in Preisschritten ausgedrückt, um dem `Point`-Multiplikator von MetaTrader zu entsprechen.
- Diagramme zeichnen automatisch Kerzen, gleitende Durchschnitte und Handelsmarkierungen ein, wenn ein Diagrammbereich verfügbar ist.
