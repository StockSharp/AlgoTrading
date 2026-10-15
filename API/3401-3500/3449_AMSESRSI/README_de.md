# AMS ES RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
Die AMS ES RSI-Strategie repliziert das Verhalten des MetaTrader-Experten `Expert_AMS_ES_RSI` innerhalb von StockSharp. Es kombiniert Morgen-/Abendsternkerzenformationen mit einem Bestätigungsfilter für den Relative Strength Index (RSI). Long-Trades werden eröffnet, wenn ein bullischer Morgenstern erscheint, während RSI überverkaufte Bedingungen anzeigt. Short-Trades werden eingegangen, wenn sich in Verbindung mit einem überkauften RSI ein rückläufiger Abendstern bildet. Positionen werden geschlossen, wenn RSI wieder die konfigurierbaren Schwellenwerte überschreitet.

## Marktannahmen
- Funktioniert auf jedem Instrument, das normale OHLC-Kerzen erzeugt. Spot FX und Index-Futures waren die ursprünglichen Ziele des MQL-Experten.
- Die Strategie erwartet eine reibungslose Preisbewegung, bei der japanische Candlestick-Muster sinnvoll sind. Extrem verrauschte Tick-Charts erzeugen möglicherweise keine zuverlässigen Signale.

## Eingabelogik
1. Abonnieren Sie den konfigurierten Zeitrahmen (Standard: 1 Stunde) und warten Sie, bis drei vollständig geschlossene Kerzen vorhanden sind.
2. Berechnen Sie die durchschnittliche Körpergröße über die letzten *BodyAveragePeriod*-Kerzen (Standard: 3).
3. Erkennen Sie einen **Morgenstern**, wenn:
   - Kerze 3 ist stark bärisch (`Open - Close` größer als die durchschnittliche Körpergröße).
   - Kerze 2 hat einen kleinen realen Körper (weniger als die Hälfte des Durchschnitts) und Lücken unter Kerze 3.
   - Kerze 1 schließt über dem Mittelpunkt von Kerze 3.
4. Erkennen Sie einen **Abendstern** mit den symmetrischen rückläufigen Bedingungen.
5. Bestätigen Sie lange Einträge, wenn der aktuelle RSI-Wert unter *LongEntryRsi* liegt (Standard: 40). Bestätigen Sie kurze Einträge, wenn RSI über *ShortEntryRsi* liegt (Standard: 60).
6. Führen Sie Marktaufträge mit der Strategie `Volume` aus.

## Exit-Logik
- Schließen Sie Long-Positionen, wenn RSI durch *UpperExitRsi* (Standard: 70) oder *LowerExitRsi* (Standard: 30) nach unten kreuzt.
- Schließen Sie Short-Positionen, wenn RSI die gleichen Niveaus nach oben kreuzt.
- Es wird kein harter Stop-Loss oder Take-Profit angewendet. Das Risikomanagement muss extern oder durch Anpassung der Schwellenwerte erfolgen.

## Parameter
| Name | Beschreibung | Standard | Reichweite |
| ---- | ----------- | ------- | ----- |
| `CandleType` | Datentyp, der die zu abonnierende Kerzenserie darstellt. | 1-stündiger Zeitrahmen | Jeder unterstützte Kerzentyp |
| `RsiPeriod` | RSI Berechnungslänge. | 47 | Optimierbar (10–70) |
| `BodyAveragePeriod` | Anzahl der Kerzen, die zur Berechnung der durchschnittlichen Körpergröße verwendet werden, die für die Mustervalidierung erforderlich ist. | 3 | Optimierbar (2–6) |
| `LongEntryRsi` | Maximaler RSI-Wert, der lange Einträge zulässt. | 40 | Optimierbar (20–50) |
| `ShortEntryRsi` | Mindestwert RSI, der kurze Einträge zulässt. | 60 | Optimierbar (50–80) |
| `LowerExitRsi` | Untere Grenze, die beim Überschreiten nach oben Ausgänge auslöst. | 30 | Optimierbar (20–40) |
| `UpperExitRsi` | Obere Grenze, die beim Überschreiten nach unten Ausgänge auslöst. | 70 | Optimierbar (60–80) |

## Implementierungshinweise
- Verwendet das StockSharp-High-Level-API mit automatischen Kerzenabonnements.
- Verlässt sich ausschließlich auf die von `Bind` bereitgestellten Indikatorwerte und vermeidet manuelle `GetValue`-Aufrufe gemäß den Projektrichtlinien.
- Behält nur einen minimalen Speicherverlauf (drei aktuelle Kerzen) zur Mustervalidierung bei.
- Die Strategie ruft beim Start automatisch `StartProtection()` auf, um integrierte Sicherheitsmechanismen zu aktivieren.

## Nutzungstipps
1. Hängen Sie die Strategie an ein Instrument/Portfolio-Paar an und stellen Sie sicher, dass die Kerzenserie über Ihren Connector verfügbar ist.
2. Passen Sie die RSI-Werte entsprechend der Vermögensvolatilität an. Höhere Schwellenwerte verringern die Anzahl der Trades, erhöhen jedoch die Bestätigungsqualität.
3. Kombinieren Sie es mit externen Positionsgrößenmodulen (z. B. risikobasiertes Volumen), um das feste Lotverhalten des Originals EA zu emulieren.
4. Stellen Sie beim Backtesting sicher, dass die Kerzendaten Lücken enthalten, damit die Sternmuster korrekt identifiziert werden können.
