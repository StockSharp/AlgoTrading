# SUPERMACBOT von The Guardian Forex TV Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **SUPERMACBOT by The Guardian Forex TV Strategy** repliziert das Konzept des ursprünglichen MetaTrader-Expertenberaters, indem sie den MACD-Oszillator mit einem dualen einfachen Trendfilter für gleitende Durchschnitte und einem Ausstiegsfilter für nachlaufende Durchschnitte kombiniert. Die konvertierte StockSharp-Implementierung funktioniert bei abgeschlossenen Kerzen und sendet Marktaufträge, wenn sich ein bullischer oder bärischer Zusammenfluss bildet. Die Strategie vermeidet den Tick-für-Tick-Handel und folgt den übergeordneten API-Richtlinien, indem sie auf Kerzenabonnements und Indikatorbindungen setzt.

Die Handelsmaschine bewertet die Dynamik anhand des MACD-Histogramms und der Trendausrichtung zwischen zwei einfachen gleitenden Durchschnitten. Ein nachlaufender gleitender Durchschnitt fungiert sowohl als Referenz für das Handelsmanagement als auch als verzögerter Bestätigungsfilter und spiegelt das im Experten MQL konfigurierte Nachlaufmodul wider. Die StockSharp-Version konzentriert sich auf Klarheit und Portabilität über Instrumente und Zeitrahmen hinweg, indem jeder Schlüsselwert als konfigurierbarer Parameter verfügbar gemacht wird.

## Handelslogik
1. **Datenquelle** – die Strategie abonniert einen konfigurierbaren Kerzentyp (Zeitrahmen). Jede abgeschlossene Kerze löst den Entscheidungsfluss aus.
2. **Indikatorvorbereitung** – MACD (mit einstellbaren schnellen, langsamen und Signalperioden) und zwei SMAs werden bei jeder Kerze neu berechnet. Ein zusätzlicher SMA repliziert den nachgestellten Filter des MQL-Experten.
3. **Eintrittsregeln**
   - **Langer Eintrag**
     - MACD Histogramm überschreitet den konfigurierbaren Schwellenwert.
     - Der schnelle SMA liegt über dem langsamen SMA und zeigt einen etablierten Aufwärtstrend.
     - Der Schlusskurs bleibt über dem letzten SMA, um die Preisstärke sicherzustellen.
     - Die Strategie hat keine bestehende Long-Position (es wird nur eine Nettoposition beibehalten).
   - **Kurzer Eintrag**
     - Das Histogramm von MACD unterschreitet den negativen Schwellenwert.
     - Der schnelle SMA liegt unter dem langsamen SMA, was auf ein rückläufiges Umfeld hinweist.
     - Der Schlusskurs bleibt unter dem nachfolgenden SMA.
     - Die Strategie beinhaltet kein Short-Engagement.
4. **Ausgangsregeln**
   - Long-Positionen werden geschlossen, wenn eines der folgenden Ereignisse eintritt: Das Histogramm wird negativ, der schnelle SMA fällt unter den langsamen SMA oder der Preis schließt unter dem nachlaufenden SMA.
   - Short-Positionen werden geschlossen, wenn das Histogramm positiv wird, der schnelle SMA über den langsamen SMA steigt oder der Preis über dem nachlaufenden SMA schließt.
5. **Risikomanagement** – der Algorithmus handelt eine einzelne Nettoposition und niemals Pyramiden. Schutzstopps können bei Bedarf extern mithilfe von StockSharp-Risikoregeln hinzugefügt werden.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Von der Strategie verarbeitete Kerzenserie. | Zeitrahmen von 1 Minute |
| `FastMaPeriod` | Periode des schnellen einfachen gleitenden Durchschnittsfilters. | 12 |
| `SlowMaPeriod` | Periode des langsamen einfachen gleitenden Durchschnittsfilters. | 26 |
| `MacdFastPeriod` | Schneller Zeitraum von EMA für den Indikator MACD. | 12 |
| `MacdSlowPeriod` | Langsamer Zeitraum von EMA für den Indikator MACD. | 24 |
| `MacdSignalPeriod` | Signalzeitraum von EMA für den Indikator MACD. | 9 |
| `HistogramThreshold` | Minimaler absoluter Wert, der aus dem MACD-Histogramm benötigt wird, bevor eine Position eröffnet wird. | 0,0 |
| `TrailingPeriod` | Zeitraum des nachlaufenden einfachen gleitenden Durchschnitts, der für Bestätigungen und Ausstiege verwendet wird. | 12 |

Alle Parameter werden über `StrategyParam<T>` verfügbar gemacht und können im StockSharp Designer optimiert werden.

## Nutzungshinweise
- Hängen Sie die Strategie an jede Sicherheit und jeden Zeitrahmen an, der zu Ihrer Testumgebung passt.
- Stellen Sie sicher, dass ein ausreichender Verlaufspuffer zur Verfügung steht, damit alle Indikatoren vollständig ausgebildet sind, bevor der Handel beginnt.
- Da die Strategie mit fertigen Kerzen und Nettopositionen arbeitet, ist es sicher, Portfolios mit mehreren Instrumenten ohne widersprüchliche Aufträge auszuführen.
- Durch die Zusammenstellung der Strategie mit anderen StockSharp-Modulen kann zusätzliches Geldmanagement (Lotgröße, Stop-Losses, Teilausstiege) hinzugefügt werden.

## Unterschiede zum Original-Experten
- Die StockSharp-Konvertierung konzentriert sich auf die Candle-Close-Logik und nicht auf die ereignisgesteuerte Engine des MetaTrader Expert Advisors. Dadurch bleibt das Verhalten bei Backtests und Live-Handel deterministisch.
- Lot-Sizing- und Trailing-Stop-Orders des ursprünglichen Expert Advisors werden durch einen vereinfachten, positionbasierten Ausstieg ersetzt, der durch den Trailing Average bedingt ist.
- Signalschwellenwerte werden über den Histogramm-Schwellenwertparameter MACD verwaltet, sodass Benutzer das Bewertungssystem des MQL-Experten nachahmen können, indem sie den Wert anpassen.

## Haftungsausschluss
Handelsalgorithmen beinhalten finanzielle Risiken. Führen Sie einen gründlichen Back- und Forward-Test der Strategie durch, bevor Sie sie mit echtem Kapital einsetzen.
