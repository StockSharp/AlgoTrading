# RSI Eraser-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die RSI Eraser-Strategie ist ein direkter Port des MetaTrader 5 Expert Advisors, erstellt von Vladimir Karputov.
Sie verwendet Stunden-Kerzen, um den Relative Strength Index (RSI) zu bewerten, und sucht nach Mean-Reversion-Einstiegen, wenn sich das Momentum um das neutrale 50-Niveau verändert.
Trades werden durch die Hoch-/Tief-Spanne des vorherigen Tages gefiltert, und die Strategie dimensioniert jede Position entsprechend einem festen Prozentsatz des Konto-Eigenkapitals.

## Kernlogik

- **Primärer Zeitrahmen** – 1-Stunden-Kerzen steuern Indikatorberechnungen und Handelssignale.
- **Filter-Zeitrahmen** – Abgeschlossene Tageskerzen liefern das gestrige Hoch und Tief, die Einstiege gateN.
- **Indikator** – Klassischer RSI mit konfigurierbarer Rückblicklänge.
- **Richtung** – Long wenn RSI > neutrales Niveau, Short wenn RSI < neutrales Niveau.
- **Risikodimensionierung** – Positionsvolumen wird aus dem Abstand zwischen Einstieg und Stop multipliziert mit dem gewählten Risikoprozentsatz abgeleitet.

## Einstiegsregeln

1. Auf den Schluss der Stundenkerze warten und RSI berechnen.
2. Sicherstellen, dass mindestens eine abgeschlossene Tageskerze verfügbar ist.
3. **Long-Setup**
   - RSI-Wert strikt über dem neutralen Schwellenwert (Standard 50).
   - Vorgeschlagenes Stop-Level (Einstieg − Stop-Loss-Abstand) darf nicht unter dem gestrigen Tief minus dem Tagespuffer liegen.
   - Einstieg wird abgelehnt, wenn an demselben Kalendertag bereits ein Long-Trade eröffnet wurde.
4. **Short-Setup**
   - RSI-Wert strikt unter dem neutralen Schwellenwert.
   - Vorgeschlagenes Stop-Level (Einstieg + Stop-Loss-Abstand) darf nicht über dem gestrigen Hoch plus dem Tagespuffer liegen.
   - Einstieg wird abgelehnt, wenn an demselben Kalendertag bereits ein Short-Trade eröffnet wurde.
5. Wenn Bedingungen erfüllt sind, sendet die Strategie einen Marktauftrag mit risikobasiertem Volumen.
   Wenn eine entgegengesetzte Position vorhanden ist, schließt der neue Auftrag sie und dreht die Richtung in einem einzigen Vorgang.

## Ausstiegsregeln

- Anfänglicher Stop-Loss und Take-Profit werden aus dem konfigurierten Pip-Abstand und Multiplikator berechnet.
- Die Strategie überwacht kontinuierlich abgeschlossene Kerzen:
  - Ein Long-Trade steigt aus, wenn der Preis bis zum Stop fällt oder bis zum Take-Profit-Level steigt.
  - Ein Short-Trade steigt aus, wenn der Preis bis zum Stop steigt oder bis zum Take-Profit-Level fällt.
- Breakeven-Schutz: Sobald sich der Preis um mindestens den ursprünglichen Stop-Abstand zu Gunsten bewegt,
  wird der Stop auf den genauen Einstiegspreis erhöht (oder für Shorts gesenkt).
- Wenn keine Position offen ist, werden alle Risiko-Level geleert, um veraltete Werte zu vermeiden.

## Risikomanagement

- `RiskPercent` definiert den Anteil des Portfolio-Eigenkapitals, der bei jedem Trade riskiert werden soll.
- Die Positionsgröße wird als `risk_amount / stop_distance` berechnet, mit einer Reserve auf das Basis-`Volume` der Strategie, wenn Eigenkapital-Informationen nicht verfügbar sind.
- Der Tagespuffer fügt eine zusätzliche Sicherheitsmarge um die gestrige Spanne hinzu, um Trades zu verhindern, die Stops zu nah an aktuellen Swing-Extremen platzieren würden.

## Standardwerte

- `RsiPeriod` = 14
- `RsiNeutralLevel` = 50
- `StopLossPips` = 50
- `TakeProfitMultiplier` = 3
- `DailyBufferPips` = 10
- `RiskPercent` = 5%
- `CandleType` = 1 Stunde
- `DailyCandleType` = 1 Tag

## Implementierungshinweise

- Die Strategie abonniert stündliche und tägliche Kerzen-Feeds mit der High-Level-StockSharp-API.
- Alle Kommentare und Log-Meldungen werden auf Englisch bereitgestellt, um den Repository-Richtlinien zu entsprechen.
- Die Breakeven-Behandlung und die Einschränkung auf einen Trade pro Tag folgen der ursprünglichen MetaTrader-Logik.
