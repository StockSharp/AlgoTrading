# Previous Day High Low Long
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nur Long: aufwärts gerichtete Schlusskurskreuzung des beobachteten Hochs/Tiefs des vorherigen UTC-Tages mit steigendem nativem ADX in einer UTC-Sitzung. Ausstiege über tatsächliches Ausführungsziel, Trailing-Stop oder Sitzungsende.

## Niveaus und Einstieg

Nur abgeschlossene Kerzen, standardmäßig fünf Minuten. Hochs/Tiefs nach UTC-Eröffnungsdatum gruppieren und beim nächsten Datum das beobachtete Tagesmaximum/-minimum einfrieren. Der vorherige Tag muss mit einer beobachteten Mitternachtskerze beginnen, die Daten müssen aufeinanderfolgen. Für einen anfänglichen Teil- oder fehlenden Tag keine erfundenen Niveaus; aktuelle Kerzen erweitern vorige Niveaus nicht.

Hochkreuz: `previousClose <= previousDayHigh && close > previousDayHigh`; Tief entsprechend aufwärts. Nativer `AdxLength=11` ADX muss gebildet und strikt größer als der vorherige gebildete Wert sein; kein weiterer ADX-Schwellenwert oder DI-Richtungsfilter.

Aus Null Markt-Kauf über `Volume` bei Einreichungszeit in `[SessionStart,SessionEnd)`, Standard09:30–16:00 UTC. Schlusskurs-, Tages- und ADX-Historie aktualisieren bei jeder Kerze; abgelehnte Kreuze werden nicht vorgemerkt. Neuer Kreuz nach Schließung erlaubt Einstieg, ohne Tageslimit oder sechsstündige Wartezeit. Kein Short.

## Schutz und Sitzungsende

`MaxProfit=150` und `MaxStopLoss=15` sind absolute Preisabstände, keine Prozente oder Instrument-Ticks. Tatsächlicher Kaufpreis initialisiert entryPrice/highWater; zusätzliche Teilkäufe aktualisieren den volumengewichteten Preis. Reduktion erhält die Basis, Nullposition setzt sie zurück.

Positiver ausführbarer Bid oder abgeschlossener Schlusskurs erhöht highWater auf das Maximum des bisherigen und aktuellen Preises. Markt-Verkauf des gesamten tatsächlichen Longs bei Preis≤`highWater-MaxStopLoss` oder≥`entryPrice+MaxProfit`. Priorität: Sitzungsende, Trailing-Stop, Gewinnziel. Keine Kerzenhochs als erfundene ausführbare Preise.

Einminütiger Marktzeittimer, Bids und Kerzen schließen ab `SessionEnd` oder am nächsten UTC-Datum. Geordnete Zeiten innerhalb eines Tages;00:00–24:00 endet nächste Mitternacht. Ausstehende Orders blockieren Duplikate, Tages-/ADX-Historie läuft weiter. Ein schließender Kerzen-Callback kehrt ohne neuen Einstieg zurück.

Lokale Überwachung, keine native Börsenstoporder. Zwischenkerzenschutz über Kurse braucht Level1 Bid; sonst bleiben Schlusskurse. Ausführung kann vom Auslöseschwellwert abweichen.

| Parameter | Standard |
|---|---:|
| `MaxProfit` / `MaxStopLoss` | 150 / 15, positive absolute Preisabstände |
| `AdxLength` | 11, positiv |
| `SessionStart` / `SessionEnd` | 09:30 / 16:00 UTC, Beginn inklusive, Ende exklusiv |
| `CandleType` | 5 Minuten |

C#-/Python-Tests gruppieren Tage und berechnen Wilder ATR/DI/DX/ADX unabhängig im vorhandenen BTC/TON-Archiv. Jeder tatsächliche Einstieg/Ausstieg, beide Vortagesniveaus, ADX-Ablehnungen, anfängliche Teilhistorie, Ausführungen, Kursstops/-ziele und Sitzungsende werden geprüft. Kein Rentabilitätsnachweis.
