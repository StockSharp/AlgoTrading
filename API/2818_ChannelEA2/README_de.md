# ChannelEA2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die ChannelEA2-Strategie repliziert den MetaTrader-Expertenberater „ChannelEA2" in StockSharp. Die Strategie erstellt einen Intraday-Preiskanal zwischen den konfigurierten Sitzungsstart- und -endstunden. Wenn die Sitzung endet, platziert sie Stop-Orders über dem Kanalhoch und unterhalb des Kanaltiefs. Jede Stop-Order trägt einen durch den gegenüberliegenden Kanalrand definierten Schutz-Stop-Loss. Der Ansatz zielt darauf ab, Ausbrüche nach einer Konsolidierungsphase während des Sitzungsfensters zu erfassen.

## Handelslogik
- Bei der ersten abgeschlossenen Kerze, deren Eröffnungszeit `BeginHour` überschreitet, setzt die Strategie die Sitzung zurück.
  - Alle offenen Positionen werden mit Market-Orders geschlossen.
  - Alle aktiven Orders, einschließlich früherer Stop-Einstiege oder Schutz-Stops, werden storniert.
  - Sitzungshoch und -tief werden mit der ersten Kerze innerhalb der neuen Sitzung initialisiert.
- Während der Sitzung (von `BeginHour` bis `EndHour`) aktualisieren Hoch und Tief jeder abgeschlossenen Kerze die Kanalgrenzen.
- Bei der ersten Kerze, die nach dem Ende der Sitzung (`EndHour`) eröffnet, berechnet die Strategie:
  - Eine Kauf-Stop-Order am aufgezeichneten Sitzungshoch plus einem optionalen Buffer in Preisschritten.
  - Eine Verkauf-Stop-Order am aufgezeichneten Sitzungstief minus demselben Buffer.
  - Der Stop-Loss für die Kauf-Order ist das Sitzungstief, der Stop-Loss für die Verkauf-Order ist das Sitzungshoch.
- Wenn eine Position eröffnet wird, wird die entgegengesetzte Einstiegs-Order storniert und ein Schutz-Stop mit dem gespeicherten Stop-Niveau im Markt registriert.
- Orders bleiben bis zum nächsten Sitzungsstart aktiv, wenn alles wieder zurückgesetzt wird.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `BeginHour` | Stunde (0-23), bei der die Sitzung zurückgesetzt wird und der Kanal beginnt, Daten zu sammeln. | `1` |
| `EndHour` | Stunde (0-23), bei der Stop-Orders geplant werden. Unterstützt Übernachtsitzungen, wenn `BeginHour > EndHour`. | `10` |
| `TradeVolume` | Volumen für jede Einstiegs-Order. | `1` |
| `CandleType` | Kerzenserie zum Aufbau des Kanals (Standard 1-Stunden-Kerzen). | `1 Stunde` |
| `StopBufferMultiplier` | Multiplikator des Instrument-Preisschritts als Sicherheitspuffer für Einstiegsaktivierung und Schutz-Stops. | `2` |

## Risikomanagement
- Die Strategie ruft automatisch `StartProtection()` auf, um StockSharp unerwartete Positionen verwalten zu lassen.
- Schutz-Stop-Orders werden sofort nach dem Erscheinen einer Position übermittelt. Sie werden storniert, wenn die Position auf null zurückgeht.
- Stop-Preise werden um `StopBufferMultiplier * PriceStep` verschoben, um die Mindest-Stop-Abstands-Grenzen der Börse nicht zu verletzen.

## Zusätzliche Hinweise
- Der Kanalbereich friert ein, sobald die Stop-Orders generiert werden; spätere Kerzen beeinflussen die Einstiegsniveaus bis zum nächsten Sitzungsstart nicht.
- Wenn das Instrument kein `PriceStep` definiert hat, wird der Buffer ignoriert und Orders werden an den genauen Kanalniveaus platziert.
- Volumenwerte sind Dezimalzahlen, was fraktionierte Kontrakte oder Lots erlaubt, wenn der Broker dies unterstützt.
- Die Strategie zeichnet Kerzen und ausgeführte Trades im Chart-Bereich zur visuellen Nachverfolgung.
