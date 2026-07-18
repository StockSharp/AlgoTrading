# Strategie Pipsover
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Pipsover ist eine Momentum-Umkehrstrategie, die auf starke Extreme des Chaikin-Oszillators reagiert. Der ursprüngliche MetaTrader 5 Expert Advisor öffnet einen neuen Trade, wenn der Oszillator einen ausgeprägten Spike druckt, während die vorherige Kerze zur 20-Perioden-Simple-Moving-Average zurückzieht. Der C#-Port behält die gleiche Idee bei, indem er den Chaikin-Oszillator mit der Akkumulations-/Distributions-Linie und zwei exponentiellen gleitenden Durchschnitten rekonstruiert. Jeder Trade wird mit den gleichen Stop-Loss- und Take-Profit-Abständen geschützt, die im Skript definiert sind, damit die Risikokontrolle mit der Referenzimplementierung übereinstimmt.

## Indikatoren und Werkzeuge
- **Simple Moving Average (SMA 20)** – liefert den Mean-Reversion-Anker. Die Strategie erfordert, dass die vorherige Kerze den Durchschnitt berührt oder kreuzt, bevor sie für einen Trade in Frage kommt.
- **Chaikin-Oszillator (EMA 3 – EMA 10 von ADL)** – misst den Druck zwischen Preis und Volumen. Extrem negative Werte lösen Long-Gelegenheiten aus und extrem positive Werte lösen Short-Gelegenheiten aus.
- **Akkumulations-/Distributions-Linie (ADL)** – speist den Chaikin-Oszillator. Die schnellen und langsamen EMAs laufen auf diesem Wertstrom, um den `iChaikin`-Indikator aus MQL5 nachzuahmen.

## Handelslogik
### Long-Einstieg
1. Auf eine abgeschlossene Kerze warten, damit alle Indikatorwerte endgültig sind.
2. Prüfen, dass die vorherige Kerze bullisch schloss (`Close > Open`).
3. Bestätigen, dass das vorherige Tief unter die SMA20 tauchte und damit einen Rücksetzer signalisiert.
4. Den Chaikin-Oszillatorwert des vorherigen Balkens lesen. Er muss kleiner als `-OpenLevel` sein, um einen überverkauften Spike widerzuspiegeln.
5. Wenn alle Bedingungen erfüllt sind und keine Position aktuell offen ist, eine Markt-Kauforder senden.

### Short-Einstieg
1. Auf eine abgeschlossene Kerze warten.
2. Prüfen, dass die vorherige Kerze bärisch schloss (`Close < Open`).
3. Bestätigen, dass das vorherige Hoch die SMA20 überschritt.
4. Sicherstellen, dass der Chaikin-Oszillator des vorherigen Balkens größer als `OpenLevel` ist.
5. Wenn keine aktive Position vorhanden ist, eine Markt-Verkaufsorder platzieren.

### Exit-Logik
- **Long-Positionen** schließen, wenn die nächste Kerze nach dem Einstieg eine bärische Struktur (Schluss unter Öffnung) zeigt, ihr Hoch über der SMA20 bleibt und der Chaikin-Oszillator über `CloseLevel` steigt.
- **Short-Positionen** schließen, wenn die nächste Kerze eine bullische Struktur zeigt, ihr Tief unter die SMA20 fällt und der Chaikin-Oszillator unter `-CloseLevel` fällt.
- Schutzausstiege überwachen jede fertige Kerze. Ein Long schließt, wenn der Preis am oder unter dem berechneten Stop-Loss oder am oder über dem berechneten Take-Profit handelt. Für Shorts ist der Vergleich umgekehrt.

## Positionsmanagement
- Es ist zu jedem Zeitpunkt nur eine Nettoposition erlaubt. Ausstehende Orders werden vor dem Öffnen eines neuen Trades storniert, um das Einzelpositions-Verhalten von MQL5 zu replizieren.
- Stop-Loss- und Take-Profit-Werte werden aus dem Instrument-Preisschritt berechnet. Für Longs wird der Stop `StopLossPoints * PriceStep` unter dem Ausführungspreis und der Take-Profit `TakeProfitPoints * PriceStep` darüber gesetzt. Shorts verwenden symmetrische, aber invertierte Abstände.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `TradeVolume` | 0.1 | Ordergröße für jede Marktorder. |
| `MaLength` | 20 | Periode der Rücksetzer-SMA. |
| `StopLossPoints` | 65 | Stop-Loss-Versatz in Preisschritten vom Einstieg. |
| `TakeProfitPoints` | 100 | Take-Profit-Versatz in Preisschritten vom Einstieg. |
| `OpenLevel` | 100 | Absoluter Chaikin-Schwellenwert, der neue Einstiege ermöglicht. |
| `CloseLevel` | 125 | Absoluter Chaikin-Schwellenwert, der den Positionsausstieg erzwingt. |
| `ChaikinFastLength` | 3 | Schnelle EMA-Länge des Chaikin-Oszillators. |
| `ChaikinSlowLength` | 10 | Langsame EMA-Länge des Chaikin-Oszillators. |
| `CandleType` | 1 Stunde | Zeitrahmen für die Kerzen-Subscription; anpassen, um die gewünschte Handelssitzung abzubilden. |

## Implementierungshinweise
- Die Strategie bindet die Akkumulations-/Distributions-Linie und SMA über `SubscribeCandles().Bind(...)` an den Kerzenfeed, sodass Indikatorwerte bereits mit jeder fertigen Kerze synchronisiert ankommen.
- Chaikin-Werte werden manuell innerhalb von `ProcessCandle` rekonstruiert, um den Low-Level-Buffer-Zugriff zu vermeiden, der von den Konvertierungsrichtlinien verboten wird.
- Der Algorithmus speichert die letzte abgeschlossene Kerze, den SMA-Wert und die Chaikin-Ablesung, um die `shift=1`-Logik (`iClose(...,1)`, `iLow(...,1)`, `iChaikin(...,1)`) des MQL5-Skripts zu reproduzieren.
- Schutz-Zielniveaus werden innerhalb der Strategieklasse verfolgt statt auf broker-verwaltete Stops zu verlassen, sodass das Verhalten zwischen Simulationen und Livetrading konsistent ist.
