# Bollinger Heikin Ashi Entry

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger-Umkehr auf Heikin-Ashi-Kerzen mit tatsächlichem Gewinnabschluss der halben Position und Trailing des Rests.

## Handelsregeln

HA-Schluss: `(Open+High+Low+Close)/4`. Erste HA-Eröffnung: `(Open+Close)/2`, danach `(vorherige HA-Eröffnung+vorheriger HA-Schluss)/2`. HA-Extrema enthalten HA-Eröffnung/Schluss und Roh-Extrema. Bollinger nutzt HA-Schlusskurse ab der ersten Archivkerze, keine Rohschlusskurse. Doji hat keine Farbe. Berührung: `HA low <= lower` / `HA high >= upper`.

Mindestens zwei aufeinanderfolgende rote HA-Kerzen müssen jeweils ihr unteres Band berühren. Eine grüne HA-Kerze mit Schluss strikt über ihrem unteren Band eröffnet Long. Umgekehrte Farben, obere Bandberührungen und roter Schluss unter dem oberen Band eröffnen Short.

Einstieg mit `Volume` nur ohne Position. Anfangsstop am Tief/Hoch der vorherigen abgeschlossenen Rohkerze, vor Auftragserteilung fixiert. `R` ist der positive Abstand vom gewichteten tatsächlichen Einstieg zum Stop. Bei `1R` wird die Hälfte des ausgeführten Bestands per Marktorder geschlossen, auf `VolumeStep` abgerundet. Ist eine Hälfte kleiner als `MinVolume`, wird am Ziel vollständig geschlossen. Ein Einstieg jenseits des Stops wird beim nächsten ausführbaren Kurs geschlossen.

Erst eine tatsächliche Zielausführung aktiviert Trailing anhand des Tiefs/Hochs der letzten abgeschlossenen Rohkerze; der Stop wird nie gelockert. Bid für Long bzw. Ask für Short lösen den vollständigen Stop zwischen Kerzen aus; Stop hat Vorrang vor Ziel. Ausstehende Orders verhindern Duplikate. Frühere Extrema lösen später erzeugte Ebenen nicht aus. Kein fixes Prozentziel oder versteckte Wartezeit.

Einstiege verwenden nur abgeschlossene Kerzen und vollständig gebildete Indikatoren. Offene Positionen werden bis zum Ausstieg verwaltet; ausstehende Marktorders verhindern doppelte Aufträge.

Bollinger verwendet die Populationsstandardabweichung des gleitenden Schlusskursfensters einschließlich der aktuellen abgeschlossenen Kerze.

## Parameter

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `20` |
| `BollingerDeviation` | `2` |
| `CandleType` | `5m` |
