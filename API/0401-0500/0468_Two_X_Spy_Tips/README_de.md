# Two X Spy Tips

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

SpySecurity und TipsSecurity sind verschiedene explizite Signalströme in Universe; auch das gehandelte Security gehört dazu. Bei der ersten gemeinsamen abgeschlossenen Monatskerze müssen beide letzten abgeschlossenen Tagesschlusskurse strikt über ihrem eigenen SmaLength-Tages-SMA einschließlich dieses Schlusskurses liegen. Ein vollständig berechneter, negativer Filter verbraucht die Monatsprüfung; fehlende Vorwärmung verschiebt sie. Ohne Position Capital×Leverage/Preis auf VolumeStep abrunden und kaufen. Long ohne Ausstieg, Short oder monatliche Aufstockung halten. USD sowie MinTradeUsd und native Mengengrenzen gelten.

## Parameter

| Parameter | Standard |
|---|---|
| `Universe` | erforderlich |
| `Capital` | 100000 |
| `Leverage` | 2 |
| `MinTradeUsd` | 200 |
| `CandleType` | TimeSpan.FromMinutes(1440).TimeFrame() |
| `PriceHistoryFile` | nicht gesetzt |
| `SpySecurity` | erforderlich |
| `TipsSecurity` | erforderlich |
| `SmaLength` | 200 |

## Eingabedaten

Optionale `PriceHistoryFile`-CSV: `SecurityId,Date,Close`, positive Kurse, eindeutige Titel/Datum-Paare, Datum `yyyy-MM-dd`. Intraday nur frühere UTC-Tage verwenden; eine abgeschlossene Tageskerze trägt ihren Schlusskurs bei. Zukünftige Zeilen greifen früheren Prüfungen nicht vor. Ausführungskerzen ersetzen ihren Tageswert.

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Daten, unabhängigen Formeln, Orderseiten/-mengen, Ausführungen, Positionen und Ausstiegen geprüft. Datierte Preis-/Faktor- und Kalenderfixtures prüfen Mechanik; verschobene OHLC/Volumina sind keine mehrjährige Marktgeschichte. Zwei Ranggruppen, TopN=1 und SMA=2 sind ausdrücklich kleine Testprofile. Lange Originalfenster und Standardwerte werden separat geprüft. Keine Renditezusage.
