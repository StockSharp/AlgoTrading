# Strategiediagramm für Market-Maker-Quotes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm macht aus einem Mean-Reversion-Signal einen vollständigen Quote-Lebenszyklus. Verlässt der Kurs ein Band um einen langsamen Durchschnitt, stellt es eine Limitorder auf der eigenen Buchseite, zieht sie per Ersetzung dem besten Preis nach, storniert veraltete oder widersprochene Orders und überquert den Spread erst nach Ablauf des passiven Versuchs.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen speisen einen SimpleMovingAverage mit Länge 100; zwei Formeln legen die Bänder 0,8% ober- und unterhalb fest.
- Market depth liefert BestBid und BestAsk. Variablen erfassen beide Preise im Kerzentakt und verhindern eine zeitliche Vermischung mit Buchaktualisierungen.
- Der aktuelle Schluss muss außerhalb, der vorige Schluss noch innerhalb des Bandes liegen. Position-Vergleiche verhindern zusätzliche Orders in Richtung einer bestehenden Position.
- Order registering stellt einen Kauf am erfassten BestBid oder einen Verkauf am BestAsk mit gemeinsamem Quote Volume ein.
- Combination hält die aktuelle Orderreferenz. Order replacing führt jede Ersetzung in denselben Strom zurück und verschiebt den Quote oberhalb des relativen Schwellenwerts.
- Order cancellation zieht den Gegenquote beim Bruch des anderen Bandes und veraltete Orders nach 12 Kerzen zurück. Bleibt der Kurs außerhalb und Position flach, eröffnet Modify position zum Marktpreis.
- Position protection schließt bei 0,8% Gewinn oder 0,4% Verlust; der Exit-Fill löst Mass order cancellation aus.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schluss fällt unter das untere Band, nachdem der vorige Schluss noch darauf oder darüber lag; Position ist nicht long und ein BestBid liegt vor. Dort wird ein Kauflimit gestellt und bei mehr als 0,001 Abweichung ersetzt. Nach 12 Kerzen wird es bei weiterhin flacher Position und Kurs unter dem Band storniert und durch einen OpenPosition-Marktkauf ersetzt.
- **Short-Einstieg**: Der Schluss steigt über das obere Band, nachdem der vorige Schluss noch darauf oder darunter lag; Position ist nicht short und ein BestAsk liegt vor. Dort wird ein Verkaufslimit gestellt und durch Ersetzungen nachgeführt. Nach 12 Kerzen wird es bei flacher Position und Kurs über dem Band storniert und durch einen OpenPosition-Marktverkauf ersetzt.
- **Ausstieg**: Alle Entry-Fills aus Registrierung, Ersetzung oder Markt-Fallback gehen an Position protection. Kerzenschlüsse steuern 0,8% Take-Profit und 0,4% Stop-Loss. Nach einem Schutz-Fill entfernt Mass order cancellation alle verbleibenden Quotes.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candle Time Frame | 00:05:00 | Zeitrahmen für Durchschnitt, Bandsignale, Buchabtastung, Quote-Alter und Schutzprüfung. |
| SMA Length | 100 | Länge des zentralen SimpleMovingAverage. |
| Band Deviation | 0.008 | Relative Bandhalbbreite; 0,008 bedeutet 0,8% auf jeder Seite. |
| Quote Volume | 1 | Menge für Quotes, Ersetzungen und Markt-Fallbacks. |
| Re-quote Threshold | 0.001 | Relative Entfernung zum besten Preis, die eine Ersetzung auslöst. |
| Quote Life, candles | 12 | Anzahl abgeschlossener Kerzen bis zum Ablauf des passiven Versuchs. |
| Take Profit, % | 0.8 | Günstige Entfernung vom Entry-Fill in Prozent. |
| Stop Loss, % | 0.4 | Ungünstige Entfernung vom Entry-Fill in Prozent. |

## Diagrammdetails

- Previous value speichert die vorige Kerze vor der Schlusskurskonvertierung; das Setup ist daher ein einmaliges Verlassen des Bandes.
- BestBid, BestAsk und Position werden in Variablen gehalten und von der fertigen Kerze freigegeben, sodass Vergleiche und Orders dieselbe Zeit tragen.
- Jede registrierte oder ersetzte Order läuft in einen Combination<Order>-Bus, der Konverter, Ersetzung, Stornierung und Chart stets mit der neuesten Order versorgt.
- Der Alterszähler startet beim Verlassen eines Bandes neu, steigt einmal je Kerze und wird eine Stufe über dem Limit gekappt; Gleichheit mit 12 erzeugt genau ein Ablaufereignis.
- Limitregistrierung und Ersetzung behalten den gelieferten Preis. Der Markt-Fallback ist OpenPosition, Schutz und Massenstornierung bereinigen den Zustand nach dem Exit.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
