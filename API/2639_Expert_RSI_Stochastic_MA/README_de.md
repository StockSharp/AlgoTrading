# Experten RSI Stochastic MA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Experten RSI Stochastic MA Strategie** ist eine Konvertierung des MetaTrader 5-Expertenberaters `Expert_RSI_Stochastic_MA.mq5`. Die C#-Implementierung nutzt StockSharp's High-Level-Strategie-API und reproduziert dabei die ursprüngliche Logik: ein Trendfilter basierend auf einem konfigurierbaren gleitenden Durchschnitt, Momentum-Bestätigung durch RSI, und ein Dual-Line-Stochastic-Oszillator für präzises Timing. Das Schutzverhalten repliziert den Quellalgorithmus mit einer optionalen festen Verlustschwelle und einem Stochastic-gesteuerten Trailing-Ausstieg.

## Indikatoren und Parameter
Die Strategie bietet dieselben Eingaben wie die MetaTrader-Version und behält deren Standardwerte bei. Alle Parameter sind für die Optimierung über die StockSharp-UI verfügbar.

| Kategorie | Parameter | Standard | Beschreibung |
| --- | --- | --- | --- |
| Allgemein | `CandleType` | 15-Minuten-Zeitrahmen | Kerzen-Aggregation für Indikatorberechnungen. |
| Trading | `TradeVolume` | `0.01` | Basisauftragsgröße in Lots/Kontrakten. |
| RSI | `RsiPeriod` | `3` | Anzahl der Balken für die RSI-Berechnung. |
| RSI | `RsiPriceType` | Schluss | Angewendeter Preis für RSI (Schluss, Eröffnung, Hoch, Tief, Median, Typisch, Gewichtet). |
| RSI | `RsiUpperLevel` | `80` | Überkauft-Schwelle, die Short-Bedingungen auslöst. |
| RSI | `RsiLowerLevel` | `20` | Überverkauft-Schwelle, die Long-Bedingungen auslöst. |
| Stochastic | `StochKPeriod` | `6` | Periode der %K-Linie. |
| Stochastic | `StochDPeriod` | `3` | Periode der %D-Glättungslinie. |
| Stochastic | `StochSlowing` | `3` | Zusätzlicher Verlangsamungsfaktor für %K. |
| Stochastic | `StochUpperLevel` | `70` | Überkauft-Niveau für beide Stochastic-Linien. |
| Stochastic | `StochLowerLevel` | `30` | Überverkauft-Niveau für beide Stochastic-Linien. |
| Gleitender Durchschnitt | `MaMethod` | Einfach | Typ des gleitenden Durchschnitts (einfach, exponentiell, geglättet, gewichtet). |
| Gleitender Durchschnitt | `MaPriceType` | Schluss | Angewendeter Preis für den gleitenden Durchschnitt. |
| Gleitender Durchschnitt | `MaPeriod` | `150` | Länge des gleitenden Durchschnitts. |
| Gleitender Durchschnitt | `MaShift` | `0` | Anzahl abgeschlossener Balken zur Rückverschiebung des gleitenden Durchschnittwerts. |
| Risiko | `AllowLossPoints` | `30` | Maximale negative Kursabweichung in Punkten vor dem Ausstieg aus einem Verlusthandel (0 deaktiviert). |
| Risiko | `TrailingStopPoints` | `30` | Abstand in Punkten für den Stochastic-basierten Trailing-Stop (0 schließt bei Stochastic ohne Trailing). |

> **Punktberechnung** – Die Implementierung konvertiert die Parameter `AllowLoss` und `TrailingStop` in absolute Preise mithilfe von `Security.PriceStep`. Bei Instrumenten mit 3 oder 5 Dezimalstellen wird der Wert mit 10 multipliziert, um MetaTrader's Pip-Behandlung zu emulieren.

## Handelslogik
### Long-Setup
1. **Trendfilter** – Kerzenschluss muss über dem verschobenen gleitenden Durchschnitt bleiben.
2. **Momentum-Bestätigung** – RSI muss unter `RsiLowerLevel` liegen.
3. **Timing** – Beide Stochastic-Linien (%K und %D) müssen unter `StochLowerLevel` liegen.
4. **Positionsfilter** – Long-Orders werden nur platziert, wenn keine Long-Exposition vorhanden ist (`Position <= 0`). Die Auftragsgröße ist `TradeVolume` plus jede Menge, die zum Schließen einer bestehenden Short-Position erforderlich ist.

### Short-Setup
1. **Trendfilter** – Kerzenschluss muss unter dem verschobenen gleitenden Durchschnitt liegen.
2. **Momentum-Bestätigung** – RSI muss `RsiUpperLevel` überschreiten.
3. **Timing** – Beide Stochastic-Linien müssen über `StochUpperLevel` liegen.
4. **Positionsfilter** – Neue Short-Positionen erfordern `Position >= 0`. Die Strategie gleicht bestehende Longs automatisch aus, falls nötig.

### Ausstiegsverwaltung
- **Verlusthandel**
  - Wenn `AllowLossPoints` null ist, wartet die Strategie, bis die Stochastic-Hauptlinie in das entgegengesetzte Extrem wechselt (`StochUpperLevel` für Longs, `StochLowerLevel` für Shorts), bevor negative Trades geschlossen werden.
  - Wenn `AllowLossPoints` positiv ist, konvertiert die Strategie den Wert in einen Preisoffset und schließt den Trade, sobald der Verlust diesen Schwellenwert überschreitet *und* der Stochastic in die neutrale Zone zurückkehrt (`stochMain > StochLowerLevel` für Longs, `< StochUpperLevel` für Shorts).
- **Trailing-Ausstieg**
  - Mit `TrailingStopPoints > 0` wird bei jedem abgeschlossenen Kerze ein Trailing-Stop gesetzt, sobald ein Trade profitabel ist und der Stochastic seine Extremzone erreicht. Bei Long-Trades verfolgt der Stop den Preis von unten; bei Short-Trades von oben.
  - Mit `TrailingStopPoints = 0` werden profitable Trades sofort geschlossen, wenn der Stochastic das Extremniveau erreicht (entspricht dem Verhalten des ursprünglichen EAs).
- **Trailing-Auslöser** – Trailing-Updates erfolgen nur bei abgeschlossenen Kerzen und spiegeln die MQL-Implementierung wider, die Updates auf eine pro Balken beschränkte.

## Implementierungshinweise
- Die Verschiebung des gleitenden Durchschnitts wird durch Speichern aktueller Werte und Lesen des Werts `MaShift` Balken zurück behandelt, was MetaTrader's `shift`-Parameter reproduziert.
- RSI- und gleitende Durchschnitt-Eingaben unterstützen mehrere angewendete Preise, um MetaTrader-Optionen abzugleichen. Stochastic-Berechnungen stützen sich auf StockSharp's integrierten Oszillator (Low/High-Modus) und berücksichtigen die konfigurierten Glättungslängen.
- Trailing- und Verlust-Schwellenwerte werden in *Punkten* gemessen. Der Helfer skaliert den Wert automatisch für typische FX-Tick-Größen (3 oder 5 Dezimalstellen) und verwendet standardmäßig einen `PriceStep`.
- Die Chartausgabe enthält Kerzen, den gleitenden Durchschnitt, RSI und Stochastic-Indikatoren, was eine visuelle Validierung ähnlich der ursprünglichen Vorlage ermöglicht.
- Es gibt keine Python-Begleitversion auf Anfrage; nur die C#-Implementierung wird bereitgestellt.

## Verwendungshinweise
- Beim Einsatz auf Wertpapieren mit unkonventionellen Tick-Größen stellen Sie sicher, dass `Security.PriceStep` ausgefüllt ist; andernfalls wird die Standardkonvertierung verwendet (1 Punkt = 1 Preiseinheit).
- Kombinieren Sie den integrierten `StartProtection` oder zusätzliche Risikomodule, wenn weitere Stop-Loss- oder Take-Profit-Verwaltung erforderlich ist.
- Optimieren Sie Indikatorlängen und Risikoschwellenwerte zusammen – die Strategie bietet absichtlich alle primären Einstellregler des MetaTrader-Experten.
