# Ema50 Crossover Monthly Dca

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Abgeschlossene Wochenkerzen und einen gebildeten EmaLength-EMA der Schlusskurse verwenden; Startwert ist das Mittel des ersten vollständigen Fensters. Zulässig sind Schlusszeiten ab StartDate. Im ersten beobachteten zulässigen Kalendermonat einmal DcaAmount gutschreiben, danach für verstrichene Monate, auch während der EMA-Vorwärmung. Keine jahrzehntelangen Beiträge vor der ersten beobachteten Kerze erfinden. Über EMA angesammeltes Geld in gerundete native Mengen investieren, darunter den gesamten tatsächlichen Long verkaufen; bei Gleichheit halten. Der Geldbestand folgt Bruttoausführungen und Verkaufserlösen, als Strategiezuordnung, nicht Kontoeinzahlung oder Finanzierungsmodell. Offene Orders abwarten, Mengengrenzen einhalten, Restgeld behalten. Keine Shorts, RSI-Filter, Wartebalken oder Schutzstops.

## Parameter

| Parameter | Standard |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(10080).TimeFrame() |
| `EmaLength` | 50 |
| `DcaAmount` | 100000 |
| `StartDate` | 1980-01-01 |

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Daten, unabhängigen Formeln, Orderseiten/-mengen, Ausführungen, Positionen und Ausstiegen geprüft. Datierte Preis-/Faktor- und Kalenderfixtures prüfen Mechanik; verschobene OHLC/Volumina sind keine mehrjährige Marktgeschichte. Zwei Ranggruppen, TopN=1 und SMA=2 sind ausdrücklich kleine Testprofile. Lange Originalfenster und Standardwerte werden separat geprüft. Keine Renditezusage.
