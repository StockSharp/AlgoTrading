# Day Trading Trend Pullback-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Day Trading-Strategie ist ein Trendfolge-System, das bei Rücksetzern in einer etablierten Richtung einsteigt. Der ursprüngliche Expertenberater (MQL-Eintrag `MQL/24298/Day Trading.mq4`) kombiniert einen 100-Perioden-EMA-Trendfilter mit Momentum und einer höheren Zeitrahmen-MACD-Bestätigung. Der StockSharp-Port behält dieselbe Idee bei und exponiert jeden wichtigen Input als Strategieparameter.

Die Strategie operiert auf einem einzelnen Instrument und einem konfigurierbaren Kerzentyp. Sie platziert keine ausstehenden Orders – alle Trades werden zu Marktpreisen ausgeführt, sobald die Bedingungen der zuletzt abgeschlossenen Kerze erfüllt sind. Schützende Stop-Loss- und Take-Profit-Niveaus werden sofort nach dem Einstieg angehängt.

## Handelslogik
1. **Trendqualifikation** – Das Tief jeder der letzten `TrendConfirmationCount` Kerzen muss über dem 100-Perioden-EMA schließen, um Long-Setups zu erlauben. Für Shorts müssen die Hochs des Lookback-Fensters unter dem EMA bleiben. Dies reproduziert den `candles()`-Helper des ursprünglichen EA.
2. **Rücksetzer-Check** – Ein Trade darf nur stattfinden, wenn mindestens eine der vorherigen drei Kerzen zum 20-Perioden-EMA zurückgesetzt hat. Für Long-Trades muss das Tief unter den EMA fallen, während Short-Trades erfordern, dass das Tief über dem EMA bleibt (der MQL-Code verwendete `Low > EMA20` für Short-Filter und derselbe Vergleich wird hier beibehalten).
3. **Momentum-Filter** – Momentum (Periode `MomentumPeriod`) muss bei einer der drei zuletzt abgeschlossenen Kerzen um mehr als `MomentumThreshold` vom neutralen Wert 100 abweichen. Die Abweichung wird als `abs(momentum - 100)` gemessen.
4. **Monatliche MACD-Bestätigung** – Der Port öffnet Positionen nur wenn die monatliche MACD-Hauptlinie für Longs über der Signallinie oder für Shorts darunter liegt. Der MACD wird auf dem `MacdCandleType`-Abonnement (monatlich standardmäßig) ausgewertet und verwendet die klassische 12/26/9-Konfiguration.
5. **Positionsgrößenbestimmung** – Jede neue Order verwendet `Volume` Lots. Die Netto-Positionsgröße überschreitet niemals `Volume * MaxPositions`. Wenn das Signal umkehrt während eine Position offen ist, dreht die Strategie die Position um, indem sie Schließ- und Eröffnungsvolumen in einer einzelnen Marktorder kombiniert.
6. **Risikomanagement** – Direkt nach einer Ausführung speichert die Strategie feste Stop-Loss- und Take-Profit-Preise aus `StopLossPips` und `TakeProfitPips`. Jede abgeschlossene Kerze prüft ob ein Niveau erreicht wurde und schließt die Position falls nötig.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Volume` | Basis-Ordergröße. Der Wert wird auf den Volumen-Schritt des Instruments normalisiert. | `1` |
| `CandleType` | Arbeitszeitrahmen. | `TimeSpan.FromMinutes(15).TimeFrame()` |
| `MacdCandleType` | Zeitrahmen für die MACD-Bestätigung. | `TimeSpan.FromDays(30).TimeFrame()` |
| `TrendConfirmationCount` | Anzahl der Kerzen, die auf der richtigen Seite des 100 EMA bleiben müssen. Spiegelt den `Count`-Input des EA. | `10` |
| `MomentumPeriod` | Momentum-Indikator-Periode. | `14` |
| `MomentumThreshold` | Mindestabstand des Momentum von 100 für Einstiege. | `0.3` |
| `StopLossPips` | Stop-Loss-Abstand in Pips. | `20` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. | `50` |
| `MaxPositions` | Maximale Anzahl der Basis-Lots, die in einer Richtung angesammelt werden können. | `10` |

## Implementierungshinweise
- Indikator-Bindings werden mit der High-Level-API durchgeführt. Das Hauptkerzen-Abonnement liefert EMA20/60/100 und Momentum-Werte, während das monatliche Abonnement den MACD-Filter via `BindEx` speist.
- Alle Sammlungen, die die MQL-Lookbacks replizieren (Rücksetzer-Flags, EMA-Trend-Flags, Momentum-Abweichungen), werden als Rolling Queues implementiert, sodass kein roher Indikatorverlauf direkt zugegriffen wird.
- Stops und Ziele werden bei jeder abgeschlossenen Kerze geprüft. Der Helper, der Pips in Preise umrechnet, passt die Pip-Größe aus dem Instrument `PriceStep` an und reproduziert die `pips`-Berechnung des EA.
- Die Strategie verwendet `StartProtection()` in `OnStarted`, damit der integrierte Schutzblock aktiviert ist bevor Orders gesendet werden.

## Konvertierungsunterschiede
- Der ursprüngliche Experte führte zahlreiche Balance-Management-Aufgaben durch (Equity-Stop, Break-Even-Schalter, benutzerdefiniertes Trailing). Nur die deterministischen Teile der Einstiegs-/Ausstiegslogik wurden portiert. StockSharp-Benutzer können die Klasse erweitern wenn diese Geldmanagement-Regeln benötigt werden.
- Mail-, Push-Benachrichtigungen und Chart-Annotationen aus der MQL-Datei werden bewusst weggelassen.
- Da StockSharp mit aggregierten Positionen arbeitet, begrenzt `MaxPositions` das absolute Netto-Exposure anstatt der rohen Order-Anzahl.

## Verwendung
1. Die Strategie an einen Connector anhängen, der das gewünschte Instrument und Kerzen-Daten für den Trading-Zeitrahmen und den MACD-Bestätigungs-Zeitrahmen liefert.
2. Parameter entsprechend der Volatilität des Assets und der Risikotoleranz anpassen. Das Erhöhen von `TrendConfirmationCount` oder `MomentumThreshold` macht Einstiege selektiver.
3. Strategie starten. Orders werden automatisch generiert sobald alle Filter auf einer abgeschlossenen Kerze übereinstimmen.

## Dateien
- `CS/DayTradingStrategy.cs` – StockSharp-Implementierung.
- `README_ru.md` – Russische Beschreibung.
- `README_zh.md` – Chinesische Beschreibung.
