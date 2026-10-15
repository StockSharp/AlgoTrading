# Strategiediagramm für Pin-Bar-Pending-Orders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses reine Long-Diagramm sucht einen langen unteren Docht in einem steigenden Durchschnittsfächer. Statt am Signalschluss zu kaufen, legt es ein Kauflimit in den Docht, storniert eine nicht ausgeführte Order nach einer festen Zahl fertiger Kerzen, schützt einen Fill prozentual und schließt zusätzlich, wenn die schnelle EMA unter die mittlere fällt.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Dreißig-Minuten-Kerzen speisen Open-, High-, Low- und Close-Konverter sowie gebildete EMA 6, EMA 18 und SMA 50.
- Formula berechnet (min(open, close) - low) / (high - low). Der untere Docht muss mehr als 0,45 der gesamten Kerzenspanne ausmachen.
- Der Trendfilter verlangt EMA 6 > EMA 18 > SMA 50. Das Tief muss EMA 6 unterschreiten und der Schluss wieder darüber liegen.
- Das Entry-Gate verlangt alle Musterbedingungen, eine flache Position und sechs Kerzen seit dem letzten Strategie-Fill.
- Order registering stellt ein Kauflimit über Order Volume bei low * (1 + 0,25 / 100) ein, ohne den Preis zu runden.
- N values zählt sechs fertige Kerzen ab Registrierung und löst Order cancellation aus; Trades for order reicht Fills an den Schutz weiter.
- Position protection setzt 1,4% Take-Profit und 0,7% Stop-Loss; EMA 6 unter EMA 18 löst zusätzlich ClosePosition zum Markt aus.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Eine fertige Kerze qualifiziert sich bei Dochtanteil über 0,45, EMA 6 > EMA 18 > SMA 50, Tief unter EMA 6, Schluss wieder über EMA 6, flacher Position und mindestens sechs Kerzen seit dem letzten Fill. Das Diagramm registriert ein Kauflimit 0,25% über dem Tief. Der Einstieg erfolgt nur bei späterer Ausführung.
- **Short-Einstieg**: Das Diagramm besitzt keinen Short-Einstieg. Ein schwächer werdender Fächer ist ein Exit, kein Short-Signal.
- **Ausstieg**: Ein nicht ausgeführtes Limit wird nach sechs fertigen Kerzen storniert. Ein gefüllter Long schließt durch Position protection bei +1,4% oder -0,7% oder per ClosePosition zum Markt, sobald EMA 6 unter EMA 18 fällt. Der Markt-Exit-Fill wird zum Schutz zurückgeführt und löscht dessen Zustand.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:30:00 | Zeitrahmen der fertigen Kerzen für Muster, Indikatoren, Orderleben, Pause und Exits. |
| Fast EMA Length | 6 | Länge der schnellen ExponentialMovingAverage, die der Docht durchsticht und der Schluss zurückerobert. |
| Medium EMA Length | 18 | Länge der mittleren ExponentialMovingAverage im Fächer. |
| Slow SMA Length | 50 | Länge der langsamen SimpleMovingAverage an der Fächerbasis. |
| Wick Share | 0.45 | Mindestanteil des unteren Dochts an der gesamten Spanne. |
| Entry Offset, % | 0.25 | Prozentualer Aufschlag auf das Signaltief für das Kauflimit. |
| Order Volume | 1 | Menge jeder Pending-Kauforder. |
| Order Life, candles | 6 | Fertige Kerzen bis zur Stornierung eines ungefüllten Limits. |
| Take Profit, % | 1.4 | Günstige Entfernung vom Entry-Fill in Prozent. |
| Stop Loss, % | 0.7 | Ungünstige Entfernung vom Entry-Fill in Prozent. |
| Cooldown, candles | 6 | Mindestzahl fertiger Kerzen seit dem letzten Fill bis zum nächsten Entry. |

## Diagrammdetails

- Preisfelder, Indikatoren, Zustände und Zähler verwenden denselben fertigen Dreißig-Minuten-Strom; die Orderzeiten bleiben auf dem Handelstakt.
- Das AND-Gate kombiniert Docht, beide Fächervergleiche, Durchstich und Erholung der schnellen EMA, flache Position und fertige Pause.
- Order registration liefert dieselbe Order an N values, Order cancellation, Trades for order und Chart; der Lebenszähler startet mit der Registrierung.
- Strategy trades setzt den Pausenzähler bei jedem eigenen Fill auf null; jede Kerze erhöht ihn bis zum Limit, ein hoher Startwert erlaubt das erste Setup sofort.
- Die flache Position sperrt neue Entries nach einem Fill. Solange ein früheres Limit offen ist, kann eine weitere passende Kerze einen unabhängigen Pending-Versuch mit derselben Sechs-Kerzen-Stornierung erzeugen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
