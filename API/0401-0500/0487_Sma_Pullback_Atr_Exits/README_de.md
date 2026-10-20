# Sma Pullback Atr Exits

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Bei abgeschlossenen Kerzen und gebildeten Schlusskurs-SMAs sowie Wilder-ATR nur kaufen, wenn Schluss < schneller SMA und schneller SMA > langsamer SMA. Short nur bei Schluss > schnellem SMA und schnellem SMA < langsamem SMA. Gleichheit ist kein Einstieg. Nur ohne Position Volume handeln und offene Orders abwarten. SL = ATR×AtrMultiplierSl und TP = ATR×AtrMultiplierTp am Signal fixieren. Nativer lokaler Marktschutz verankert die Abstände an der Ausführung und überwacht Bid/Ask. Spätere ATR-Werte verschieben nichts. Schutz schließt den gesamten Restbestand; Gegensignale drehen offene Positionen nicht um.

## Parameter

| Parameter | Standard |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(30).TimeFrame() |
| `FastSmaLength` | 8 |
| `SlowSmaLength` | 30 |
| `AtrLength` | 14 |
| `AtrMultiplierSl` | 1.2 |
| `AtrMultiplierTp` | 2 |

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Daten, unabhängigen Formeln, Orderseiten/-mengen, Ausführungen, Positionen und Ausstiegen geprüft. Datierte Preis-/Faktor- und Kalenderfixtures prüfen Mechanik; verschobene OHLC/Volumina sind keine mehrjährige Marktgeschichte. Zwei Ranggruppen, TopN=1 und SMA=2 sind ausdrücklich kleine Testprofile. Lange Originalfenster und Standardwerte werden separat geprüft. Keine Renditezusage.
