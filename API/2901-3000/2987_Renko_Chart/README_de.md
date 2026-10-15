# Renko Chart-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
RenkoChartStrategy ist eine direkte Konvertierung des ursprünglichen **RenkoChart.mq5**-Experten. Anstatt Orders zu platzieren, konzentriert sich die Strategie darauf, den benutzerdefinierten Renko-Symbol-Workflow innerhalb von StockSharp zu recreieren. Sie abonniert Tick-Daten, erzeugt einen Renko-Kerzenstrom mit konfigurierbarer Stein-Größe und exponiert ihn über die Plattform, sodass er visualisiert oder an andere Komponenten weitergeleitet werden kann. Jeder abgeschlossene Stein wird mit dem letzten Tick protokolliert, der ihn ausgelöst hat, was dem Operator ermöglicht, die generierte Serie gegen die MQL-Implementierung zu validieren.

## Zuordnung vom MQL-Expert
- **StartDateTime** → `StartTime`: der anfängliche Zeitstempel, der beim Setzen der Renko-Historie verwendet wird.
- **BaseSymbol** → `Strategy.Security`: StockSharp weist das Basisinstrument bereits zu, daher wurde der Parameter ersetzt, indem auf das ausgewählte Wertpapier vertraut wird. Die Strategie setzt dem generierten Streamnamen weiterhin `RenkoPrefix` voran, um die "Renko-\<symbol\>"-Namenskonvention nachzuahmen.
- **Mode (Bid/Last)** → `UseBidTicks`: schaltet um, ob Bid-Updates oder Trade-Ticks den Live-Monitoring-Feed antreiben.
- **Range** → `BrickSizeSteps`: Anzahl der Preisschritte, die einen Renko-Stein bilden. Die Strategie multipliziert den Wert mit dem `PriceStep` des Wertpapiers, um die absolute Boxgröße zu erhalten.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `StartTime` | `DateTimeOffset` | 2018‑08‑01 09:00:00 UTC | Steine mit einer Öffnungszeit vor diesem Moment werden ignoriert und entsprechen dem ursprünglichen Warmup-Verhalten. |
| `BrickSizeSteps` | `int` | 5 | Renko-Steingröße ausgedrückt in Preisschritten. Wird in absoluten Preis umgerechnet, wenn die Renko-Serie erstellt wird. |
| `UseBidTicks` | `bool` | `false` | Wenn `false` lauscht die Strategie auf Trade-Ticks, wenn `true` lauscht sie auf Bid-Updates um den MQL-`Bid`-Modus zu emulieren. |
| `RenkoPrefix` | `string` | `"Renko-"` | Präfix zu den Protokollnachrichten hinzugefügt, damit der Streamname der benutzerdefinierten Symbol-Namenskonvention entspricht. |

> **Hinweis:** die berechnete `BrickSize`-Eigenschaft exponiert die absolute Boxgröße und kann nützlich sein, wenn die Strategie mit anderen Komponenten verdrahtet wird, die ein Preis-Delta anstelle von Schrittanzahlen erwarten.

## Datenfluss
1. `GetWorkingSecurities` konfiguriert ein Renko-Kerzen-Abonnement mit `RenkoBuildFrom.Points` und der berechneten Boxgröße.
2. `OnStarted` startet das Renko-Abonnement, abonniert entweder Trade- oder Bid-Ticks (abhängig von `UseBidTicks`) und zeichnet den Renko-Stream auf dem Chart, falls einer verfügbar ist.
3. `ProcessTrade` / `ProcessLevel1` speichern den neuesten Tick-Preis und Zeitstempel für Protokollierungszwecke.
4. `ProcessCandle` ignoriert unfertige Steine, filtert Daten vor `StartTime` heraus und protokolliert jeden abgeschlossenen Stein mit den vorherigen und neuen Schlussniveaus sowie den neuesten Tick-Informationen.

## Verwendungshinweise
- Hängen Sie die Strategie an ein beliebiges Instrument an, das entweder Trades oder Level-1-Updates bereitstellt. Der Renko-Stream erscheint im Standard-Chartbereich mit dem konfigurierten Präfix.
- Da die Implementierung keine Orders sendet, kann sie parallel zu anderen Trading-Strategien ausgeführt werden, um eine synchronisierte Renko-Ansicht des Marktes zu liefern.
- Die Protokolleinträge enthalten sowohl die Steinrichtung als auch den auslösenden Tick. Dies ist praktisch beim Vergleich der Ausgabe mit historischen Daten, die aus MetaTrader exportiert wurden.

## Unterschiede zur MQL-Version
- StockSharp verwaltet Symbole bereits, daher wurde die explizite benutzerdefinierte Symbolerstellung durch Protokollierungs- und Chartausgabe ersetzt.
- Alle Berechnungen verwenden Dezimalarithmetik anstelle von Arrays und stützen sich auf den integrierten Renko-Kerzen-Builder.
- Die Strategie übernimmt das Abonnementmodell und den Schutzhelfer von StockSharp, wodurch sie bereit ist, bei Bedarf mit Handelslogik erweitert zu werden.
