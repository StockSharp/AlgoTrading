# Brandy-Strategie (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Brandy-Strategie ist eine direkte Portierung des MetaTrader 5-Expert Advisors *Brandy (barabashkakvn's edition)*. Sie kombiniert zwei konfigurierbare gleitende Durchschnitte und bewertet deren relative Positionen auf abgeschlossenen Kerzen, um zu entscheiden, ob eine Long- oder Short-Position eröffnet werden soll. Die ursprüngliche Logik erzwingt auch optionale Stop-Loss-, Take-Profit- und Trailing-Stop-Kontrollen in Pips. Diese C#-Version reproduziert diese Verhaltensweisen originalgetreu auf der StockSharp-High-Level-Strategie-API.

Die Strategie berechnet einen "schnellen" gleitenden Durchschnitt auf dem Eröffnungspreisstream und einen "langsamen" gleitenden Durchschnitt auf dem Schlusskursstream. Beide Indikatoren haben unabhängige Parameter für Periode, Glättungsmethode, Preisquelle, Signalbalken-Referenz und Verschiebung. Signale werden generiert, wenn die MA-Werte des vorherigen Balkens auf derselben Seite der jeweiligen Signalwerte liegen. Die Schutzlogik prüft den eröffnungsbasierten gleitenden Durchschnitt bei jeder Kerze und verlässt den Trade sofort, wenn die Trendbedingung nicht mehr erfüllt ist. Zusätzliches Risikomanagement wird mit optionalen Stop-Loss-, Take-Profit- und Trailing-Stop-Abständen implementiert, alle in Pips gemessen und in absolute Preise umgerechnet durch die Verwendung der Tick-Größe des Instruments mit einer Fünf-Dezimalstellen-Pip-Anpassung.

## Handelslogik
1. Bei jeder abgeschlossenen Kerze aktualisiert die Strategie die eröffnungs- und schlusspreisbasierten gleitenden Durchschnitte unter Verwendung der konfigurierten Glättungsmethode und des angewendeten Preises. Historische MA-Werte werden gepuffert, damit der Code das `iMA`-Verschiebungsverhalten des ursprünglichen Expert Advisors emulieren kann.
2. Wenn keine aktive Position vorhanden ist, wird ein Long-Trade eröffnet, wenn:
   - Der vorherige eröffnungsbasierte MA-Wert größer als der konfigurierte Signalwert (möglicherweise verschoben) ist;
   - Der vorherige schlussbasierte MA-Wert ebenfalls größer als seine Signalreferenz ist (zu beachten ist, dass der ursprüngliche EA für diese Prüfung gegen den eröffnungsbasierten Indikator vergleicht, und die Portierung behält diese Eigenart für die Kompatibilität).
3. Ein Short-Trade wird eröffnet, wenn beide gleitenden Durchschnitte unter ihren jeweiligen Signalreferenzen liegen.
4. Während eine Position aktiv ist, bewertet die Strategie Ausstiege bei jeder abgeschlossenen Kerze in folgender Reihenfolge:
   - Trendumkehr: wenn der eröffnungsbasierte MA unter den Signalwert fällt (für Longs) oder darüber steigt (für Shorts), wird die Position sofort zum Marktpreis geschlossen.
   - Trailing-Stop-Aktualisierung: wenn aktiviert und die Bewegung zugunsten des Trades *Trailing Stop + Trailing Step* überschreitet (in absolute Preise umgerechnet), wird das Stop-Niveau eng gezogen, um eine Distanz von *Trailing Stop* vom letzten Schluss beizubehalten.
   - Take-Profit: wenn der Kerzenbereich das Gewinnziel berührt, wird der Trade zum Marktpreis geschlossen.
   - Stop-Loss: wenn der Kerzenbereich das Schutz-Stop-Niveau verletzt, wird der Trade geschlossen.
5. Das gesamte Volumen ist fest und wird durch den Parameter `TradeVolume` bestimmt. Der Standardwert repliziert die 0,1-Lot-Einstellung der MT5-Version.

## Parameterreferenz
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Marktordergröße in Lots.
| `StopLossPips` | Abstand des Schutz-Stops, gemessen in Pips (0 deaktiviert ihn).
| `TakeProfitPips` | Abstand des Gewinnziels in Pips (0 deaktiviert es).
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. Erfordert, dass `TrailingStepPips` positiv ist.
| `TrailingStepPips` | Zusätzliche Pip-Bewegung erforderlich, bevor der Trailing Stop vorgerückt wird. Muss ungleich null sein, wenn der Trailing Stop aktiv ist.
| `MaClosePeriod`, `MaOpenPeriod` | Gleitende Durchschnittslängen für die Schluss- und Eröffnungsserien.
| `MaCloseShift`, `MaOpenShift` | Auf die MA-Puffer angewendete Vorwärtsverschiebungen (Anzahl der Balken).
| `MaCloseSignalBar`, `MaOpenSignalBar` | Als Vergleichsreferenzen verwendete Balkenindizes. Null entspricht dem neuesten Wert, eins bezieht sich auf den vorherigen Balken, usw.
| `MaCloseMethod`, `MaOpenMethod` | Glättungsmethoden für gleitende Durchschnitte (SMA, EMA, SMMA, LWMA).
| `MaCloseAppliedPrice`, `MaOpenAppliedPrice` | Kerzenpreisquelle für jeden Indikator (Schluss, Eröffnung, Hoch, Tief, Median, Typisch, Gewichtet).
| `CandleType` | Zeitrahmen der von der Datenquelle angeforderten Kerzen.

## Implementierungshinweise
- Die Pip-Größe wird aus `Security.PriceStep` berechnet und mit 10 multipliziert, wenn das Instrument 3 oder 5 Dezimalstellen aufweist, was die MetaTrader-Anpassung zwischen Punkten und Pips widerspiegelt.
- Die Indikatorhistorie wird mit begrenzten Warteschlangen aufbewahrt, damit die Strategie `iMA`-Aufrufe mit beliebigen Signalbalken-Indizes und positiven Verschiebungen reproduzieren kann, ohne sich auf verbotene Indikatorzugriffe zu stützen.
- Die Abschlussbedingung für den schlusspreisbasierten gleitenden Durchschnitt vergleicht absichtlich gegen den **Eröffnungs**-MA-Puffer, weil der ursprüngliche Quellcode `iMAGet(handle_iMAOpen, MaClose_SignalBar)` aufrief. Diese Portierung behält das Verhalten bei, um die Kompatibilität mit alten Konfigurationen zu erhalten.
- Stops und Trailing-Logik werden auf abgeschlossenen Kerzen ausgeführt und approximieren die vom Expert Advisor vorgenommenen Ordermodifikationen unter Einhaltung der StockSharp-High-Level-API.

## Nutzungstipps
- Konfigurieren Sie den `CandleType`-Parameter entsprechend dem vom ursprünglichen EA verwendeten Zeitrahmen (typischerweise ein einzelner Instrument-Zeitrahmen).
- Lassen Sie `TrailingStopPips` auf null, wenn kein Trailing-Verhalten gewünscht wird; stellen Sie andernfalls sicher, dass `TrailingStepPips` strikt positiv ist, um den von der Strategie erzwungenen Initialisierungsfehler zu vermeiden.
- Stellen Sie beim Backtesting in StockSharp sicher, dass `PriceStep` und `Decimals` des Instruments die beabsichtigte Pip-Definition widerspiegeln, damit Risikoabstände korrekt umgerechnet werden.
