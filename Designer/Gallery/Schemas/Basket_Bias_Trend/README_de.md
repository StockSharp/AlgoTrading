# SMMA-Trenddiagramm für ein einzelnes Instrument
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trotz des historischen Galerienamens ist dies kein Korb. Das Diagramm folgt dem aktuellen VectorStrategy-Code: ein Instrument, geglättete Durchschnitte auf Vierstundenkerzen, Netto-Umkehr und Ausstieg über absoluten schwebenden Geld-PnL.

![schema](schema.svg)

## Strategieübersicht

- Fertige Vierstundenkerzen speisen SMMA(3) und SMMA(7); ihre Reihenfolge bestimmt bullische oder bärische Tendenz.
- Ein einmaliges Flag startet N values und sperrt die ersten acht fertigen Kerzen.
- Bullisch darf bei Position <= 0 gekauft, bärisch bei Position >= 0 verkauft werden.
- Das Volumen ist abs(Position) plus Basisvolumen und verbindet Schließen und Öffnen zur Netto-Umkehr.
- P&L change schließt bei +5000 oder -300000 in Kontowährung.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Nach dem Warm-up liegt die schnelle SMMA über der langsamen und Position ist null oder short. Modify position kauft zum Markt, schließt Short und lässt eine Basiseinheit Long.
- **Short-Einstieg**: Nach dem Warm-up liegt die schnelle SMMA unter der langsamen und Position ist null oder long. Modify position verkauft zum Markt, schließt Long und lässt eine Basiseinheit Short.
- **Ausstieg**: Die Gegentendenz dreht die Position direkt. Zusätzlich startet unrealisierter PnL >= 5000 oder <= -300000 ein marktmäßiges ClosePosition; bleibt der Trend, ist später ein Wiedereinstieg möglich.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candle Time Frame | 04:00:00 | Fertiger Kerzenzeitrahmen für beide geglätteten Mittelwerte und Positionsentscheidungen. |
| Fast SMMA Length | 3 | Anzahl der Werte in der schnellen SmoothedMovingAverage. |
| Slow SMMA Length | 7 | Anzahl der Werte in der langsamen SmoothedMovingAverage. |
| MA Shift Warmup | 8 | Anfängliche fertige Kerzen, bevor Trendeinstiege erlaubt werden. |
| Base Volume | 1 | Positionsgröße nach Einstieg aus null oder Netto-Umkehr. |
| Profit Target, money | 5000 | Unrealisierter Gewinn in Kontowährung für ClosePosition. |
| Loss Limit, money | -300000 | Unrealisierte Verlustgrenze in Kontowährung; negativ belassen. |

## Diagrammdetails

- GetWorkingSecurities liefert nur (Security, CandleType); Index, Sync, zweites Instrument und Korbbestätigung fehlen daher bewusst.
- ProfitPercent 0,5 und LossPercent 30 werden beim Teststartsaldo 1.000.000 zu +5000 und -300000.
- Designer bietet unrealisierten PnL in Geld, aber keinen Startsaldo; Prozentgrenzen werden deshalb explizite Kontowährungswerte.
- Das Warm-up zählt wie processedBars die ersten acht fertigen Kerzen; SMMA(7) ist vor Handelsbeginn gebildet.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
