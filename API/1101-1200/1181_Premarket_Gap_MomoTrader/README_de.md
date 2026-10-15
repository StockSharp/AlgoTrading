# Premarket Gap MomoTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kerzenmomentum ausschließlich Long, mit einem Einstieg pro New Yorker Kalenderdatum. „Gap“ bezeichnet den Schlusskursanstieg zum vorherigen Schlusskurs; eine Übernacht-Kurslücke oder EMA-Kreuzung ist nicht erforderlich.

## Einstieg und Größe

Nur abgeschlossene Kerzen, standardmäßig fünf Minuten. Die erste speichert Schlusskurs und Volumen. Aus der Nullposition sind eine strikt bullische Kerze, positive Hoch-Tief-Spanne, Gesamtvolumen > `MinVolume`, positiver vorheriger Schlusskurs und `100 * (close - previousClose) / previousClose >= MinGainPct` erforderlich.

`bodyPercent = 100 * (close - open) / (high - low)`: ≥90% fordert 100% des Strategie-`Volume`, ≥85% und <90% fordert 50%, ≥75% und <85% fordert 25%; unter 75% kein Einstieg. Die unteren Grenzen sind inklusive.

Ein vorhandener positiver `VolumeStep` rundet die Ordergröße nach unten. Nullgrößen oder Größen außerhalb angegebener Instrument-Minima/Maxima werden abgelehnt. Bestehende Positionen werden nicht vergrößert.

Mit `UseSession=true` muss die Einreichung in New Yorker Zeit innerhalb `[SessionStart, SessionEnd)` liegen, standardmäßig 04:00–09:30, einschließlich Sommerzeitumstellung. Auch ohne Sitzungsfilter gilt ein Einstieg pro Datum. Eine eingereichte Einstiegsorder verbraucht das Tagesrecht, selbst bei anschließendem Fehler.

## Halten und Ausstieg

Die erste rote Kerze (`close < open`) oder Kerze mit `volume <= previousVolume` schließt die gesamte tatsächliche Longposition. Bullische Kerzen mit steigendem Volumen halten; ein Doji hält ebenfalls bei steigendem Volumen. Ausstiege gelten außerhalb der Einstiegssitzung und unabhängig von Einstiegsanstieg, Körperanteil und Mindestvolumen. Auf der Ausstiegskerze gibt es keinen neuen Einstieg. Ausstehende Orders verhindern Duplikate, während vorherige Werte bei jeder abgeschlossenen Kerze aktualisiert werden.

Keine Indikatoren, Short-Einstiege, sechsstündige Wartezeit oder gesonderte Stop-Loss-/Take-Profit-Regeln. Ein weiterer Einstieg braucht das nächste geeignete New Yorker Datum.

## Standardwerte

| Parameter | Standard | Bedeutung |
|---|---:|---|
| `MinGainPct` | 5 | Inklusive Mindeststeigerung in Prozent; nicht negativ |
| `MinVolume` | 15000 | Strikte Volumenuntergrenze; nicht negativ, in Dateneinheiten |
| `UseSession` | true | Zeitfilter für Einstiege; C#-Eigenschaft `IsUseSession` |
| `SessionStart` / `SessionEnd` | 04:00 / 09:30 | Geordnete Zeiten eines New Yorker Tages; Beginn inklusive, Ende exklusiv |
| `CandleType` | 5 Minuten | Abgeschlossene Signalkerzen |

Tests anhand des vorhandenen BTC/TON-Archivs prüfen jede Order und tatsächliche Position, drei Größenstufen, Lotrundung, Tageslimit, beide Ausstiegsgründe, Haltefälle und New Yorker Sommerzeit im Frühjahr. TON liefert eine passende Kerze mit den veröffentlichten 5%/15000; BTC-Mechaniktests ändern Schwellen ausdrücklich. Rentabilität wird nicht nachgewiesen.
