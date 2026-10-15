# Strategiediagramm zum Schließen am Geldziel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm ergänzt die SMA(10)/SMA(30)-Richtung um einen Geld-Notausstieg. Einstiege sind Pending Limits, damit Mass order cancellation beim Erreichen einer schwebenden Gewinn- oder Verlustgrenze echte Orders vor ClosePosition entfernt.

![schema](schema.svg)

## Strategieübersicht

- Fertige Fünfminutenkerzen speisen beide SMAs; Greater und Less prüfen ihren Zustand auf jeder Kerze statt eines Kreuzereignisses.
- Bullisch mit Position <= 0 setzt ein Kauflimit am Schluss, bärisch mit Position >= 0 ein Verkaufslimit.
- Volumen ist abs(Position) plus Basisvolumen und erhält die Umkehr als eine Nettoorder.
- P&L change vergleicht unrealisierten Strategieerfolg mit +300 und -150 in Kontowährung.
- Jede Grenze startet gleichzeitig Mass order cancellation und marktmäßiges ClosePosition.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Schnelle SMA über langsamer und Position null oder short: Das Kauflimit deckt Short und lässt eine Basiseinheit Long.
- **Short-Einstieg**: Schnelle SMA unter langsamer und Position null oder long: Das Verkaufslimit deckt Long und lässt eine Basiseinheit Short.
- **Ausstieg**: PnLUnreal >= 300 oder <= -150 storniert alle aktiven Strategieorders und schließt die Position zum Markt. Bei P&L null kann die Strategie erneut handeln.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candle Time Frame | 00:05:00 | Fertiger Kerzenzeitrahmen für beide einfachen Mittelwerte und Einstiegsentscheidungen. |
| Fast SMA Length | 10 | Anzahl der Werte in der schnellen SimpleMovingAverage. |
| Slow SMA Length | 30 | Anzahl der Werte in der langsamen SimpleMovingAverage. |
| Base Volume | 1 | Positionsgröße nach flachem Einstieg oder Netto-Umkehr. |
| Profit Target, money | 300 | Unrealisierter Gewinn in Kontowährung, der die Liquidation startet. |
| Loss Limit, money | -150 | Unrealisierte Verlustgrenze in Kontowährung; negativ belassen. |

## Diagrammdetails

- RequestCloseAll wird im C# nie aufgerufen; der aktive Pfad handelt nur SMA-Zustand zum Markt. Das Diagramm setzt die angekündigte Geldlogik um.
- Quellparameter sind Equity-Niveaus und standardmäßig null; hier werden PnLUnreal sowie +300/-150 verwendet.
- Limits am Schluss ersetzen Markteinstiege, damit Mass order cancellation Arbeitsorders hat; Preisrundung ist für Replay ohne Schritt aus.
- Der Code würde nach Liquidation Stop aufrufen. Das Diagramm bleibt für wiederholte Monatszyklen aktiv.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
