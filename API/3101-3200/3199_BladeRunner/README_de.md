# BladeRunner-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die BladeRunner-Strategie ist eine Übersetzung des MetaTrader Expert Advisors, der Fraktal-Ausbrüche mit Trend- und Momentum-Bestätigung kombiniert. Der StockSharp-Port behält die Multi-Timeframe-Struktur des ursprünglichen Skripts bei und analysiert drei verschiedene Kerzendaten-Feeds: eine primäre Serie für die Trade-Ausführung, eine höhere Timeframe-Serie für den Momentum-Filter und eine langsame Serie für den MACD-Trendfilter. Orders werden mit konfigurierbarer Skalierung, Stop-Loss- und Take-Profit-Abständen in Preisschritten eröffnet.

## Trading-Logik
1. **Fraktal-Ausbruchsfilter** – die Strategie scannt abgeschlossene Kerzen auf Bill-Williams-Fraktal-Muster. Ein bullisches (oberes) Fraktal wird akzeptiert, wenn die zwei Bars früher gebildete Kerze ein neues Swing-Hoch bildet und die Bestätigungskerze unterhalb des Fraktalpreises und des 20-Perioden-LWMA des typischen Preises eröffnet. Bearische Fraktale wenden die symmetrischen Regeln an.
2. **Trendbestätigung** – schnelle und langsame lineare gewichtete gleitende Durchschnitte (LWMA), die auf der primären Kerzenserie berechnet werden, definieren den zugrundeliegenden Trend. Longs erfordern, dass der schnelle LWMA über dem langsamen liegt, während Shorts die entgegengesetzte Ausrichtung erfordern.
3. **Momentum-Filter** – ein Momentum-Oszillator, der auf dem höheren Timeframe-Kerzenstream berechnet wird, muss in jeder der letzten drei Beobachtungen um mindestens den konfigurierten Schwellenwert von 100 abweichen. Dies reproduziert die Momentum-Spike-Prüfungen aus der MQL-Version.
4. **MACD-Filter** – ein MACD, der auf dem langsamen Timeframe berechnet wird, muss seine Hauptlinie über (Long) oder unter (Short) der Signallinie haben, was den monatlichen Filter des Expert Advisors widerspiegelt.
5. **Ausbruchsbestätigung** – der Schlusskurs der aktuellsten primären Kerze muss das gespeicherte Fraktalniveau überschreiten, bevor die Order gesendet wird.

Wenn alle Filter übereinstimmen, öffnet die Strategie eine Marktposition mit der konfigurierten Losgröße. Bestehende Exposition in der entgegengesetzten Richtung wird vor der Umkehr geschlossen. Zusätzliche Einträge sind erlaubt, bis die maximale Anzahl von Scale-In-Trades erreicht ist.

## Implementierungsdetails
- Drei Kerzenabonnements werden über die High-Level-API erstellt. Jeder Feed bindet direkt an die erforderlichen Indikatoren, ohne sie zur globalen Indikatorsammlung hinzuzufügen.
- LWMAs operieren auf dem typischen Preis (HLC/3), um der MQL-Implementierung zu entsprechen. Der MACD verbraucht ebenfalls typische Preise.
- Die Fraktaldetektion speichert ein gleitendes Fenster abgeschlossener Kerzen und zugehöriger Filterwerte. Nur die zuletzt validierte Fraktalrichtung wird gespeichert, was doppelte Signale auf derselben Struktur verhindert.
- Der Momentum-Verlauf wird als Array fester Größe gespeichert, was dynamische Zuweisungen vermeidet und den Lookback des ursprünglichen EA reproduziert.
- Die Order-Größenanpassung respektiert Börseneinschränkungen durch Volumen-Schritt-, Mindest- und Maximalvolumen-Anpassungen.
- Der eingebaute `StartProtection`-Helfer wendet Stop-Loss- und Take-Profit-Abstände in Preisschritten an, was den festen Pip-Werten aus MetaTrader entspricht.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Primäre Kerzenserie für die Signalgenerierung. | 15-Minuten-Kerzen |
| `MomentumCandleType` | Höhere Timeframe-Serie für den Momentum-Filter. | 1-Stunden-Kerzen |
| `MacdCandleType` | Kerzenserie für den MACD-Trendfilter. | Tageskerzen |
| `FastMaPeriod` | Länge des schnellen LWMA. | 6 |
| `SlowMaPeriod` | Länge des langsamen LWMA. | 85 |
| `FilterMaPeriod` | LWMA zur Validierung von Fraktal-Ausbrüchen. | 20 |
| `MomentumPeriod` | Averaging-Periode des Momentum-Indikators. | 14 |
| `MomentumThreshold` | Minimale absolute Abweichung des Momentums von 100. | 0.3 |
| `FractalLookback` | Anzahl der für die Fraktalanalyse gespeicherten Kerzen. | 200 |
| `MaxTrades` | Maximale Anzahl von Scale-In-Orders pro Richtung. | 3 |
| `OrderVolume` | Basisvolumen für jede Marktorder. | 1 Kontrakt |
| `TakeProfitSteps` | Take-Profit-Abstand in Preisschritten. | 50 |
| `StopLossSteps` | Stop-Loss-Abstand in Preisschritten. | 20 |

## Risikomanagement
- Stop-Loss- und Take-Profit-Niveaus werden automatisch über `StartProtection` an jede Position angehängt.
- Die Strategie schließt immer die entgegengesetzte Exposition, bevor Trades in der neuen Richtung eröffnet werden, um Hedge-Situationen zu vermeiden.
- Das Volumen wird vor der Orderplatzierung an die Instrumentbeschränkungen angepasst. Das `MaxTrades`-Limit begrenzt die gesamten Skalierungsschritte pro Richtung.

## Unterschiede zum ursprünglichen EA
- Die Equity-Stop-, Trailing-Stop- und Break-Even-Hilfsprogramme von MetaTrader sind nicht implementiert. Die StockSharp-Risikosteuerung kann bei Bedarf extern hinzugefügt werden.
- Geldbasierte Trailing-Logik und Push-Benachrichtigungen werden weggelassen, da StockSharp alternative Benachrichtigungsworkflows bietet.
- Der MACD-Filter verwendet standardmäßig Tageskerzen anstelle von Monatsbalken. Passen Sie `MacdCandleType` auf einen monatlichen Zeitrahmen an, wenn dies von der angeschlossenen Datenquelle unterstützt wird.
- Die Fraktalvalidierung basiert auf der letzten Bestätigungskerze im gleitenden Fenster. Dies erzeugt denselben praktischen Effekt wie die Schleife im MQL-Skript, während wiederholte Scans vermieden werden.

## Verwendungshinweise
1. Konfigurieren Sie die Kerzentypen so, dass sie zu den Instrumenten und Zeitrahmen passen, die von Ihrer Datenquelle unterstützt werden.
2. Richten Sie `OrderVolume`, `TakeProfitSteps` und `StopLossSteps` an der Tick-Größe und dem Volumenschritt des Instruments aus.
3. Optimieren Sie `MomentumThreshold` und die LWMA-Längen während Walk-Forward-Tests, um die Ausbruchsempfindlichkeit an verschiedene Märkte anzupassen.
4. Aktivieren Sie die Chart-Zeichnung, um die drei LWMAs zu visualisieren und sicherzustellen, dass Fraktal-Ausbrüche mit den Trendfiltern übereinstimmen, bevor Sie live gehen.
