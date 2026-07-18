# FT-Trendfolger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
FT Trend Follower ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `FT_TrendFollower.mq4`. Die Strategie nutzt mittelfristige Trends, indem sie einen GMMA-Fächer (Guppy Multiple Moving Average) mit einem Laguerre-Oszillator-Trigger, einem schnellen/langsamen EMA-Crossover und einem MACD-Filter kombiniert. Einstiege werden erst ausgelöst, wenn der Markt in das GMMA-Bündel eintaucht, sich von einem Laguerre-Extrem erholt und die Mehrheit der GMMA-Linien wieder in Richtung des Handels tendiert. Das Gewinnmanagement spiegelt das ursprüngliche EA wider: ein optionaler Swing-basierter Stop, ein Stop mit fester Distanz und drei sich gegenseitig ausschließende abgestufte Exit-Module, die durch tägliche Pivot-Levels oder Kanaldurchschnitte gesteuert werden.

## Strategielogik
### GMMA-Struktur- und Trenderkennung
* Der GMMA-Fächer erstreckt sich von `StartGmmaPeriod` bis `EndGmmaPeriod`. Punkte werden auf fünf Gruppen von jeweils `BandsPerGroup`-Zeilen verteilt und replizieren so die ursprüngliche `CountLine`-Logik.
* Die Trendrichtung vergleicht die langsamere GMMA-Gruppe (Index `CountLine + CountLine` vom Ende) mit der schnelleren Langzeitgruppe (Index `CountLine` vom Ende). Steigende langfristige Durchschnittswerte definieren einen Aufwärtstrend; fallende Werte definieren einen Abwärtstrend.
* Die Steigungsbestätigung zählt, wie viele kurz-, mittel- und langfristige GMMA-Linien im Vergleich zum vorherigen Balken gestiegen oder gefallen sind. Für einen Handel muss die Anzahl der steigenden (oder fallenden) Steigungen die Hälfte der gesamten GMMA-Linien überschreiten, was den Schwellenwert `controlvverh`/`controlvverhS` in MetaTrader nachahmt.

### Signalvorbereitung
* **Close Reset** – Wenn die vorherige Kerze unter der langsamsten GMMA-Linie schließt, wird das lange Modul aktiviert; wenn es oberhalb der langsamsten Linie schließt, schwenkt das kurze Modul ein. Beim erneuten Überqueren (oder Unterschreiten) des schnellsten GMMA werden die Aktivierungsflags gelöscht, genau wie bei der ursprünglichen `CloseOk`-Logik.
* **Laguerre-Trigger** – Ein Laguerre-Filter (`LaguerreGamma`) muss zuerst unter `LaguerreOversold` (Long-Setup) fallen oder über `LaguerreOverbought` (Short-Setup) steigen, während die Kerze noch den langfristigen GMMA respektiert. Erst nachdem sich der Oszillator wieder über die Schwelle zurückgezogen hat, kann ein Eintrag ausgelöst werden.
* **EMA Crossover** – Der schnelle EMA (`FastSignalLength`) muss unter den langsamen EMA (`SlowSignalLength`) abtauchen, um das lange Modul scharfzuschalten, und dann wieder darüber kreuzen, um den Eingang freizugeben. Shorts kehren die Ungleichheit um.
* **MACD-Filter** – Die Hauptlinie MACD (5/35/5 wie in EA) muss für Long-Positionen positiv und für Short-Positionen negativ sein.

### Einreisebestimmungen
Ein Long-Trade wird ausgeführt, wenn:
1. Die Trenderkennung meldet einen Aufwärtstrend und die GMMA-Steigungsabstimmung überschreitet die Hälfte der verfügbaren Linien.
2. Der Laguerre-Trigger war zuvor aktiviert und der aktuelle Wert schließt wieder über `LaguerreOversold`.
3. Der schnelle EMA liegt über dem langsamen EMA, nachdem er zuvor darunter lag.
4. MACD ist größer als Null.

Kurze Einträge erfordern die symmetrischen Bedingungen, wobei der Oszillator den Wert `LaguerreOverbought` und MACD negativ kreuzt. Beim Umkehren einer bestehenden Position gleicht die Ordergröße automatisch das vorherige Engagement aus, sodass die endgültige Nettoposition gleich `Volume` ist.

### Risikomanagement und Exits
* **Stopps** – Wählen Sie entweder den Swing-Stop (`UseSwingStop`), der um `SwingStopPips` Punkte unter (über) der vorherigen Kerze positioniert ist, oder den Stop mit festem Abstand (`UseFixedStop`), der um `FixedStopPips` Punkte versetzt ist. Wenn beide gleichzeitig aktiviert sind, bricht die Strategie beim Start ab und reproduziert die EA-Validierungsregeln.
* **Pivot-Exit-Modul (Quit)** – Wenn aktiviert, wird der erste Teilabschluss (50 % von `Volume`) ausgelöst, sobald der Preis den R1/S1-Pivot des Vortages mit nicht realisiertem Gewinn überschreitet. Der Rest wird geschlossen, sobald der Hull MA einen gültigen Wert erzeugt, der mit der Pufferprüfung `hma1` von MetaTrader übereinstimmt.
* **Pivot-Range-Exit-Modul (Quit1)** – Der anfängliche teilweise Abschluss erfolgt immer noch bei R1/S1. Der Rest wird bei R2/S2 ausgegeben, sobald der Handel profitabel bleibt.
* **Kanalausgangsmodul (Quit2)** – Das erste teilweise Schließen erfolgt bei R1/S1. Die Strategie schließt den Rest, wenn die Kerze wieder unterhalb des unteren SMA-Kanals (`ChannelPeriod`) für Long-Positionen oder über dem oberen SMA-Kanal für Short-Positionen öffnet, was den ursprünglichen Volatilitätsfilter widerspiegelt.

Es kann jeweils nur ein Exit-Modul aktiv sein, genau wie die Parametervalidierung von EA.

## Parameter
* **Volumen** – Auftragsgröße für neue Trades.
* **StartGmmaPeriod / EndGmmaPeriod** – Grenzen für den GMMA-Fan.
* **BandsPerGroup** – Anzahl der pro Gruppe abgetasteten GMMA-Linien (CountLine in MT4).
* **FastSignalLength / SlowSignalLength** – EMA Längen, die für die Crossover-Bestätigung verwendet werden.
* **TradeShift** – Aus Kompatibilitätsgründen beibehalten; Die Implementierung arbeitet mit fertigen Kerzen, sodass andere Werte als 0 oder 1 abgelehnt werden.
* **UseSwingStop / SwingStopPips** – Aktiviert und konfiguriert den schwenkbasierten Schutzstopp.
* **UseFixedStop / FixedStopPips** – Aktiviert den Stop mit fester Distanz, gemessen in Preispunkten.
* **EnablePivotExit / EnablePivotRangeExit / EnableChannelExit** – Sich gegenseitig ausschließende Staged-Exit-Module.
* **LaguerreOversold / LaguerreOverbought / LaguerreGamma** – Laguerre-Triggerschwellenwerte und Glättungsfaktor.
* **HmaPeriod** – Rumpf-MA-Länge, die vom Pivot-Exit-Modul verwendet wird.
* **ChannelPeriod** – Länge des Hoch-/Tief-SMA-Kanals für Quit2.
* **CandleType** – Zeitrahmen, der die Strategieberechnungen steuert (Standard: 1-Stunden-Kerzen).

## Zusätzliche Hinweise
* Tägliche Pivot-Level werden anhand der letzten abgeschlossenen täglichen Kerze berechnet, die von einem sekundären Abonnement bereitgestellt wird.
* Preispunkte und Pip-Konvertierungen hängen vom `PriceStep` des Wertpapiers ab. Symbole mit unterschiedlichen Tick-Größen passen sich automatisch an.
* Die Strategie abonniert nur Indikatoren auf hoher Ebene und vermeidet direkte Pufferlesevorgänge, wobei die API-Richtlinien des Projekts auf hoher Ebene eingehalten werden.
* In diesem Paket ist keine Python-Implementierung enthalten.
