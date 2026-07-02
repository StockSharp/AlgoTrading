# Cs2011-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Cs2011-Strategie ist ein Umkehrsystem, das aus dem ursprünglichen `cs2011.mq5`-Expertenberater übersetzt wurde. Es überwacht das MACD-Histogramm und die Signallinie jeder fertigen Kerze und sucht nach Erschöpfungsmustern rund um die Nulllinie. Der C#-Port behält die Kern-Timing-Regeln bei und macht sie über die hohe Ebene StockSharp API verfügbar.

## Handelslogik
- **Nulllinienumkehr** – wenn der MACD-Wert des vorherigen Balkens über Null liegt, während der Balken davor unter Null lag, gibt die Strategie ein **Short**-Signal aus. Der entgegengesetzte Übergang (von positiv nach negativ) gibt ein **langes** Signal aus. Dies ahmt die konträren Einträge nach, die im Skript MQL5 implementiert sind.
- **Signalleitungsextreme** – Die Strategie speichert die letzten drei Signalleitungswerte. Ein lokales Maximum, während MACD negativ blieb, löst einen zusätzlichen Short-Eintrag aus; ein lokales Minimum, während MACD positiv blieb, löst einen Long-Eintrag aus. Dies reproduziert die Musterprüfungen basierend auf `Sig[0]`, `Sig[1]` und `Sig[2]` in der Quelle EA.
- Signale werden nur für fertige Kerzen ausgewertet, die von `SubscribeCandles` bereitgestellt werden, daher werden Teildaten ignoriert.

## Positionsverwaltung
- Die Strategie zielt auf eine **feste absolute Positionsgröße** (`TargetVolume`) ab. Wenn ein bullisches Signal eintrifft, werden genügend Kontrakte gekauft, um `+TargetVolume` zu erreichen. Das Gleiche gilt für bärische Signale für `-TargetVolume`. Bestehendes Engagement in die gleiche Richtung wird respektiert – es werden keine weiteren Aufträge erteilt, wenn das Ziel bereits erreicht ist.
- `StartProtection` spiegelt die ursprünglichen Take-Profit- und Stop-Loss-Einstellungen wider. Punktabstände werden in `UnitTypes.Point`-Werte umgewandelt und an das integrierte Risikomodul übergeben. Wenn Sie einen Wert auf `0` belassen, wird die entsprechende Barriere deaktiviert.
- Anstelle der Low-Level-Anfragestruktur aus der MQL-Version werden High-Level-Helfer (`BuyMarket`, `SellMarket`) verwendet.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `TargetVolume` | `1` Los | Absolute Positionsgröße, die nach einem Signal erreicht wird. Ersetzt die `Risk` × Balance-Größenbestimmungsroutine aus EA. |
| `TakeProfitPoints` | `2200` | Abstand in Preispunkten für Take-Profit-Management. `0` deaktiviert den Take-Profit. |
| `StopLossPoints` | `0` | Abstand in Preispunkten für den Stop-Loss. `0` deaktiviert den Stop-Loss und entspricht den EA-Standardwerten. |
| `FastEmaPeriod` | `30` | Schnelle EMA-Länge für den MACD-Kern. |
| `SlowEmaPeriod` | `500` | Langsame EMA-Länge für MACD. |
| `SignalPeriod` | `36` | Glättungszeitraum der Signalleitung. |
| `CandleType` | `1 hour` Zeitrahmen | Von `SubscribeCandles` verwendete Kerzenquelle. Passen Sie dies an den in MetaTrader verwendeten Diagrammzeitraum an. |

Alle Parameter werden über `Param()` registriert, sodass sie in der Optimierer-Benutzeroberfläche von StockSharp optimiert werden können.

## Unterschiede zur MQL5-Version
- Die Geldverwaltungsroutine (`Money_M`) stützte sich auf historische Geschäfte und den Kontostand von MetaTrader. StockSharp-Strategien arbeiten mit Broker-agnostischen Portfolios, daher stellt der Port einen einfachen `TargetVolume`-Parameter bereit. Benutzer können ihre eigene Geldverwaltung verbinden, indem sie den Parameterwert oder die Methode `ExecuteSignals` überschreiben.
- Orderanfragen werden zu Single-Market-Orders vereinfacht. Wiederholungslogik, Spread-basierte Abweichung und Handelskontextprüfungen werden von der StockSharp-Infrastruktur übernommen.
- Die Strategie läuft auf Kerzenabonnements statt auf dem benutzerdefinierten `IsNewBar`-Helper. Dadurch ist gewährleistet, dass nur fertig geformte Kerzen verarbeitet werden.

## Nutzungshinweise
1. Konfigurieren Sie das Wertpapier, das Portfolio und den Kerzentyp, bevor Sie die Strategie starten.
2. Passen Sie `TargetVolume` an die gewünschte nominale Losgröße an.
3. Passen Sie optional `TakeProfitPoints` und `StopLossPoints` an, um die Schutzstufen des Originals EA zu reproduzieren.
4. Starten Sie die Strategie – Protokollierungsnachrichten zeichnen jeden Handelsauslöser zusammen mit dem angestrebten Engagement auf.

Der Code enthält englische Inline-Kommentare, die jeden Schritt des Portierungsprozesses beschreiben.
