# Zehn-Punkte-3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- Konvertiert den MetaTrader 4 Expert Advisor **10p3v004 ("10points 3")** in das StockSharp High-Level-Strategie-Framework.
- Erstellt die MACD-Steigungs-basierte Gittereintrittslogik zusammen mit Martingal-Skalierung, Trailing-Schutz und eigenkapitalbasierten Ausstiegen neu.
- Bietet eine umfassende Dokumentation aller Parameter, sodass das Verhalten des ursprünglichen EA reproduziert oder sicher optimiert werden kann.

## Handelslogik
1. **Signalerkennung.** Bei jeder abgeschlossenen Kerze des konfigurierten Zeitrahmens berechnet die Strategie einen MACD mit benutzerdefinierten schnellen, langsamen und Signallängen. Wenn der Hauptwert MACD im Vergleich zum vorherigen Balken steigt, erstellt das System ein langes Raster; Wenn es fällt, wird ein kurzes Gitter vorbereitet. Das Flag `ReverseSignals` kehrt diese Interpretation um.
2. **Gittereinträge.** Es kann jeweils nur ein Richtungsgitter aktiv sein. Die erste Bestellung erfolgt unmittelbar nach einem Signal. Zusätzliche Bestellungen werden hinzugefügt, wenn:
   - Die aktive Gitterrichtung entspricht dem aktuellen Signal und
   - Der Preis hat sich seit der letzten Füllung um mindestens `GridSpacingPoints * PriceStep` in die günstige Durchschnittsrichtung bewegt, und
   - Die Anzahl der Open-Grid-Trades hat nicht `MaxTrades` erreicht.
Die Bestellgröße wird für kleine Gitter (bis zu 12 Einträge) mit `2^n` oder für größere Gitter mit `1.5^n` multipliziert, wodurch die Martingallogik aus dem Quellcode reproduziert wird. Die endgültige Größe wird auf den Instrumentenvolumenschritt gerundet und sowohl durch die Sicherheitsgrenzen als auch durch die Sicherheitsobergrenze `MaxVolumeCap` begrenzt.
3. **Geldverwaltung.** Wenn `UseMoneyManagement` aktiviert ist, wird die Basislosgröße aus dem aktuellen Portfoliowert und `RiskPerTenThousand` abgeleitet. Das ursprüngliche EA verwendete separate Regeln für Standard- und Minikonten; Diese Konvertierung behält das gleiche Verhalten über den Parameter `IsStandardAccount` bei. Wenn die Einstellung deaktiviert ist, wird das feste `BaseVolume` verwendet.
4. **Ausgangsregeln.**
   - Der optionale **Anfangsstopp** schließt das gesamte Raster, wenn sich die aggregierte Position um `InitialStopPoints` dagegen bewegt.
   - Der optionale **Take Profit** schließt das Raster, sobald der Preis `TakeProfitPoints` zugunsten der Nettoposition erreicht.
   - Der optionale **Trailing Stop** beginnt mit der Verfolgung des Preises, nachdem er sich um `(TrailingStopPoints + GridSpacingPoints)` vom durchschnittlichen Einstiegspreis entfernt hat, und behält einen Trailing-Puffer von `TrailingStopPoints` bei.
   - Der optionale **Aktienschutz** überwacht nicht realisierte Gewinne, gemessen in Punkten mal Volumen. Wenn `OrdersToProtect` oder mehr Positionen offen sind und der Gewinn `SecureProfit` erreicht, wird die Strategie sofort beendet.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Primärer Zeitrahmen für MACD-Berechnungen und Auftragsabwicklung. | 30-Minuten-Kerzen |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD Konfiguration identisch mit dem MT4-Indikator (standardmäßig 14/26/9). | 14./26./9 |
| `BaseVolume` | Die anfängliche Losgröße wird verwendet, wenn keine Rasterposition vorhanden ist und die Geldverwaltung deaktiviert ist. | 0,01 |
| `GridSpacingPoints` | Mindestabstand zwischen aufeinanderfolgenden Rastereinträgen, ausgedrückt in Preisschritten. | 15 |
| `TakeProfitPoints` | Abstand vom durchschnittlichen Einstieg, um einen vollständigen Take-Profit auszulösen. Zum Deaktivieren auf `0` setzen. | 40 |
| `InitialStopPoints` | Maximaler nachteiliger Abstand, der vor dem Abflachen des Gitters toleriert wird. Zum Deaktivieren auf `0` setzen. | 0 |
| `TrailingStopPoints` | Größe des nachgestellten Puffers. Der Trail wird aktiviert, nachdem der Preis um `GridSpacingPoints + TrailingStopPoints` gestiegen ist. | 20 |
| `MaxTrades` | Maximale Anzahl von Mittelungsaufträgen pro Richtung. | 9 |
| `OrdersToProtect` | Mindestanzahl offener Geschäfte erforderlich, bevor die Eigenkapitalschutzprüfung ausgewertet wird. | 3 |
| `SecureProfit` | Nicht realisiertes Gewinnziel (Punkte × Volumen), das den Aktienschutzausstieg auslöst. | 8 |
| `AccountProtectionEnabled` | Aktiviert oder deaktiviert den Aktienschutzblock. | `true` |
| `ReverseSignals` | Kehrt die MACD-Steigungsinterpretation um (nützlich für gespiegelte Tests). | `false` |
| `UseMoneyManagement` | Ermöglicht die dynamische Volumenberechnung mit `RiskPerTenThousand`. | `false` |
| `RiskPerTenThousand` | Risikobetrag pro 10.000 Einheiten des Guthabens, der bei aktivem Geldmanagement verwendet wird. | 12 |
| `IsStandardAccount` | Repliziert die ursprünglichen Losrundungsregeln (`true` = Standardlose, `false` = Minilose). | `true` |
| `MaxVolumeCap` | Nach der Martingal-Skalierung wird eine feste Kappe angewendet, um die Positionsgröße unter Kontrolle zu halten. | 100 |

## Konvertierungshinweise
- Der MQL-Experte unterhielt separate Haltestellen auf Ticketebene. In StockSharp wird das Raster als einzelne aggregierte Position verwaltet. Daher werden die Trailing- und Protective-Levels aus dem volumengewichteten durchschnittlichen Einstiegspreis neu berechnet.
- Der EA stützte sich auf den Tick-Wert des Brokers, um Gewinne in Währung umzurechnen. Hier wird der Aktienschutzschwellenwert in Punkten multipliziert mit dem Volumen gemessen und spiegelt den Pip-basierten Vergleich der Quelle wider.
- Für `AccountFreeMarginCheck` und andere kontospezifische MT4-Validierungen gibt es kein direktes StockSharp-Äquivalent. Die Strategie respektiert stattdessen die Volumengrenzen des Instruments und das optionale `MaxVolumeCap`.
- Bestellkommentare, magische Zahlen und grafische Anmerkungen aus MT4 werden nicht reproduziert, da sie kein StockSharp-Gegenstück haben.

## Nutzung
1. Fügen Sie die Strategie Ihrem Projekt hinzu und legen Sie `Security` und `Portfolio` wie üblich für StockSharp-Strategien fest.
2. Passen Sie `CandleType` an den zu analysierenden Zeitrahmen an (die MT4-Version funktionierte im aktuellen Zeitrahmen des Diagramms).
3. Optimieren Sie die Risikoparameter: Behalten Sie entweder den festen Wert `BaseVolume` bei oder aktivieren Sie `UseMoneyManagement` mit den entsprechenden Optionen `RiskPerTenThousand` und `IsStandardAccount`.
4. Entscheiden Sie, welche Schutzschichten aktiviert werden sollen (Initial Stop, Take Profit, Trailing Stop, Aktienschutz) und legen Sie die Schwellenwerte entsprechend der Volatilität des Instruments fest.
5. Starten Sie die Strategie; Die integrierten Chart-Helfer zeigen Kerzen, MACD-Werte und ausgeführte Trades an.

## Weiterentwicklungsideen
- Integrieren Sie eine adaptive Abstandslogik (z. B. mit ATR) anstelle des festen `GridSpacingPoints`.
- Stellen Sie separate nachgestellte Parameter für lange und kurze Gitter bereit oder lassen Sie asymmetrische Gitter zu.
- Kombinieren Sie die MACD-Steigung mit Trendfiltern (gleitende Durchschnitte, höhere Zeitrahmenbestätigung), um die Anzahl der gegenläufigen Trendgitter zu reduzieren.

> **Hinweis:** Für diese Strategie wird keine Python-Implementierung bereitgestellt, die der Anfrage und der aktuellen Projektstruktur entspricht.
