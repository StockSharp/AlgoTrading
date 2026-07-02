# Gleitende durchschnittliche Verschiebungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Bei dieser Strategie handelt es sich um eine StockSharp-High-Level-Portierung des klassischen Expert Advisors **Moving Average**, der mit MetaTrader ausgeliefert wird. 4. Das System beobachtet abgeschlossene Kerzen und vergleicht sie mit einem verschobenen einfachen gleitenden Durchschnitt (SMA), um Richtungsänderungen zu erkennen. Aufträge werden immer zum Marktwert ausgeführt und die Strategie bleibt mit höchstens einer offenen Position zu jedem Zeitpunkt auf dem Markt.

## Handelslogik

1. Abonnieren Sie Kerzen des konfigurierbaren Zeitrahmens (Standard: 5 Minuten) und berechnen Sie einen SMA mit dem gewünschten Zeitraum.
2. Verschieben Sie SMA um die angegebene Anzahl abgeschlossener Kerzen, um das ursprüngliche Verhalten der Funktion `iMA` zu emulieren.
3. Bewerten Sie die zuvor fertige Kerze:
   - **Bullish Cross** (eröffnet unter dem verschobenen SMA und schließt darüber) löst einen Long-Einstieg aus, wenn keine Position offen ist.
   - **Bearish Cross** (eröffnet über und schließt unter dem verschobenen SMA) löst einen Short-Einstieg aus, wenn keine Position offen ist.
4. Verwalten Sie Exits mit denselben Kreuzregeln:
   - Eine Long-Position wird geschlossen, wenn die letzte Kerze den verschobenen SMA unterschreitet.
   - Eine Short-Position wird geschlossen, wenn die letzte Kerze den verschobenen SMA überschreitet.
5. Es kann immer nur eine Position existieren, die dem Verhalten des ursprünglichen EA entspricht, der zwischen Kauf- und Verkaufsaufträgen wechselte.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Für Berechnungen verwendete Kerzenreihe. Es kann ein beliebiger Zeitrahmen `DataType` ausgewählt werden. | Zeitrahmen von 5 Minuten |
| `MovingPeriod` | Anzahl der Kerzen für die Länge SMA. | 12 |
| `MovingShift` | Offset des SMA-Werts in abgeschlossenen Kerzen. Emuliert das `shift`-Argument von `iMA`. | 6 |
| `BaseVolume` | Standard-Auftragsvolumen für Einträge. Für Long- und Short-Trades wird das gleiche Volumen verwendet. | 1 |

## Umgang mit Indikatoren

- Ein `SimpleMovingAverage`-Indikator wird in `OnStarted` erstellt und über das übergeordnete `Bind` API an das Kerzenabonnement gebunden.
- Die Rohausgabe von SMA wird in einer kleinen FIFO-Warteschlange gepuffert, um den Wert von `MovingShift` Kerzen zu erhalten. Es wird keine manuelle Neuberechnung des Indikators durchgeführt.
- Die Warteschlange behält nur `MovingShift + 1`-Werte bei, sodass die Speichernutzung auch bei großen Schichten konstant bleibt.

## Auftrags- und Risikomanagement

- Bestellungen werden mit `BuyMarket`/`SellMarket` aufgegeben und durch den Parameter `BaseVolume` dimensioniert. Beim Schließen wird die aktuelle absolute Positionsgröße verwendet, um einen vollständigen Ausstieg sicherzustellen.
- Die ursprüngliche MetaTrader-Implementierung passte die Losgröße basierend auf der freien Marge und den jüngsten Verlusten dynamisch an. Der Port StockSharp hält die Logik deterministisch und delegiert die Positionsgröße über den Parameter `BaseVolume` an den Benutzer. Dies vermeidet die Abhängigkeit von Broker-spezifischen Kontometriken und behält gleichzeitig die Ein-/Ausstiegsregeln bei.

## Konvertierungshinweise

- Signale werden auf der **vorherigen** Kerze ausgewertet und entsprechen der `Volume[0] == 1`-Prüfung von MetaTrader, die auf einen neuen Balken wartete, bevor sie reagierte.
- Es werden nur abgeschlossene Kerzen (`CandleStates.Finished`) verarbeitet, um vorzeitige Trades zu vermeiden.
- Die Strategie nutzt die StockSharp-Chart-Helfer, um Kerzen, Indikatorwerte und Handelsmarkierungen zu zeichnen, wenn ein Chartbereich verfügbar ist.

## Nutzung

1. Kompilieren Sie die Strategie in StockSharp Designer, Shell oder Runner.
2. Wählen Sie das gewünschte Instrument aus und weisen Sie ein Portfolio zu.
3. Konfigurieren Sie die Parameter, wenn unterschiedliche Zeitrahmen, Längen oder Volumina erforderlich sind.
4. Starten Sie die Strategie; Es abonniert die ausgewählte Kerzenserie, überwacht SMA Kreuze und handelt entsprechend.

## Weitere Ideen

- Fügen Sie Schutzstopps oder Take-Profit-Levels mit `StartProtection` hinzu, wenn ein Risikomanagement erforderlich ist, das über den grundlegenden Umkehrausstieg hinausgeht.
- Ersetzen Sie den einfachen SMA durch einen anderen Indikator (EMA, LWMA usw.), indem Sie die Indikatorinstanz ändern und dabei den bestehenden Abonnement-Workflow beibehalten.
- Führen Sie Positionsskalierungsregeln ein, indem Sie die Methode `GetEntryVolume` anpassen.
