# MACD Signal-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Dieses Beispiel wandelt den ursprünglichen MetaTrader 4-Expertenberater `MACD_v1.mq4` in eine StockSharp-Strategie auf hoher Ebene um. Der Algorithmus verfolgt Überkreuzungen der gleitenden durchschnittlichen Konvergenzdivergenz (MACD) und handelt in Richtung des neuen Trends. Optionale Schutzausstiege reproduzieren das Verhalten des ursprünglichen Beraters: einen Stop-Loss, einen entfernten Take-Profit und ein Teilgewinnziel, das die Hälfte der aktuellen Position liquidiert.

## Handelslogik
1. **Datenquelle** – Die Strategie abonniert die konfigurierte Kerzenserie (standardmäßig 5-Minuten-Kerzen) und bindet einen `MovingAverageConvergenceDivergenceSignal`-Indikator.
2. **Eintrittsbedingungen**:
   - Geben Sie **long** ein, wenn die MACD-Linie die Signallinie kreuzt. Wenn eine Short-Position aktiv ist, wird diese geschlossen, bevor die Long-Position eröffnet wird.
   - Geben Sie **short** ein, wenn die MACD-Linie die Signallinie unterschreitet. Wenn eine Long-Position besteht, wird diese zuerst geschlossen.
3. **Exit-Bedingungen**:
   - Gegenüberliegende MACD-Kreuzung schließt die aktuelle Position und öffnet eine neue Position in der entgegengesetzten Richtung.
   - Ein konfigurierbarer Take-Profit und Stop-Loss, der von `StartProtection` verwaltet wird, spiegelt die ursprünglichen TP/SL-Parameter wider (gemessen in Instrumentenpunkten).
   - Ein Teilgewinnziel schließt die Hälfte der offenen Position, sobald der Preis um einen bestimmten Betrag vom Einstiegsniveau steigt. Der Teilausstieg wird nur einmal pro Position ausgelöst.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| **Schnelle Periode** | 23 | Schnelle EMA-Länge für die MACD-Berechnung (spiegelt den MQL-Parameter `a = 2300` wider). |
| **Langsamer Zeitraum** | 40 | Langsame EMA-Länge für die MACD-Berechnung (`b = 4000` in der Quelle). |
| **Signalperiode** | 8 | Länge der Signalleitung (`c = 800` in der Quelle). |
| **Gewinn mitnehmen** | 500 | Abstand in Preispunkten für die schützende Take-Profit-Order. Zum Deaktivieren auf `0` setzen. |
| **Stop-Loss** | 80 | Abstand in Preispunkten für die schützende Stop-Loss-Order. Zum Deaktivieren auf `0` setzen. |
| **Teilgewinn** | 70 | Abstand in Preispunkten, um die Hälfte der offenen Position zu schließen. Auf `0` setzen, um Teilausstiege zu deaktivieren. |
| **Kerzentyp** | Zeitrahmen von 5 Minuten | Für Indikatorberechnungen verwendete Kerzenserien.

## Notizen
- Indikatorparameter wurden auf typische MACD-Längen (23/40/8) skaliert, da das MQL-Skript sie als Hundertstel (2300/4000/800) ausdrückte.
- Die Strategie stellt das Teilausstiegsflag automatisch wieder her, wenn eine neue Position akkumuliert wird.
- Hilfsmittel zum Zeichnen von Diagrammen heben Kerzen, MACD-Werte und die Trades der Strategie hervor, wenn ein Diagrammfeld verfügbar ist.
- Die Volumenverarbeitung basiert auf der Eigenschaft der Basisstrategie `Volume`. Passen Sie es vor Beginn der Strategie an Ihre Instrumentengröße an.
