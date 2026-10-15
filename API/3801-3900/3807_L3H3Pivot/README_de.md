# L3H3-Pivot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **L3H3 Pivot Strategy** ist ein StockSharp-Port des MetaTrader-Experten „L3_H3_Expert“. Das ursprüngliche Skript baut eine tägliche Pivot-Struktur auf und setzt zwei ausstehende Orders ein, um potenzielle Ausbrüche oder Rückschläge rund um die Hochs und Tiefs der vorherigen Sitzung zu handeln. Die StockSharp-Version behält die gleiche Idee bei: Sie berechnet die Pivot-Levels nach jeder abgeschlossenen Kerze im höheren Zeitrahmen neu (standardmäßig täglich) und entscheidet zwischen Stop- oder Limit-Einträgen basierend darauf, wo der Markt im Verhältnis zur gestrigen Spanne aktuell handelt.

## Handelslogik

1. **Sitzungsstatistik**
   - Nach jeder abgeschlossenen Pivot-Kerze (Standard: täglich) erfasst die Strategie die Eröffnungs-, Höchst-, Tiefst- und Schlusswerte der vorherigen Sitzung.
   - Das klassische Pivot-Level wird als `(High + Low + Close) / 3` berechnet.
   - Diese Ebenen bleiben für die gesamte nächste Sitzung aktiv.

2. **Eintrag einrichten**
   - Ein Kaufeinstiegspreis liegt leicht über dem vorherigen Tief verankert. Der Offset entspricht dem Parameter `EntryOffsetPips`, ausgedrückt in Vielfachen der Pip-Größe.
   - Ein Verkaufseinstiegspreis ist am vorherigen Hoch verankert (was den ursprünglichen Experten widerspiegelt, der das Rohhoch ohne zusätzlichen Puffer verwendet hat).
   - Für jeden neuen Handelstag (erkannt über das Hauptkerzenabonnement) platziert die Strategie neue ausstehende Aufträge:
     - Wenn der Markt **unterhalb** des gestrigen Tiefs notiert, wird ein **Kaufstopp** gesetzt, um einen Ausbruch nach oben zu verhindern.
     - Wenn der Markt **über** dem gestrigen Hoch handelt, wird ein **Verkaufsstopp** gesetzt, um eine Abwärtsumkehr zu erzielen.
     - Andernfalls bevorzugt der Algorithmus **Limit-Orders** auf den gleichen Preisniveaus, um Rückgänge zu kaufen oder Rallyes zurück in die Spanne zu verkaufen.
   - Stop-Loss-Orders werden `StopLossPips` vom Referenztief/-hoch entfernt positioniert, genau wie die MQL-Version einen 16-Punkte-Stopppuffer festgelegt hat.
   - Der Take-Profit beider ausstehender Aufträge ist an der Pivot-Ebene ausgerichtet und spiegelt die im Quellcode gefundene Zielplatzierung wider.

3. **Auftragsverwaltung**
   - Jedes Mal, wenn ein neuer Pivot berechnet wird, werden alle aktiven ausstehenden Aufträge storniert und mit den neuen Niveaus neu berechnet.
   - Die Strategie storniert außerdem veraltete ausstehende Orders, wenn eine neue Sitzung beginnt, und verhindert so die Anhäufung inaktiver Bestellungen.
   - Wenn eine Bestellung ausgeführt wird, wird ihre interne Referenz automatisch gelöscht, um doppelte Stornierungen zu vermeiden.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `EntryCandleType` | Kerzenserien, die zur Überwachung der aktuellen Sitzung und zur Auslösung der Auftragserteilung verwendet werden. | Zeitrahmen von 5 Minuten |
| `PivotCandleType` | Kerze mit höherem Zeitrahmen, die zum Messen der vorherigen Sitzungsstatistiken verwendet wird. | Täglicher Zeitrahmen |
| `EntryOffsetPips` | Distanz (in Pips), die über dem vorherigen Tief für Long-Einstiege hinzugefügt wird. | 2 |
| `StopLossPips` | Abstand (in Pips), der über das Referenztief/-hoch hinaus angewendet wird, um Stop-Losses zu positionieren. | 16 |

## Unterschiede zum MQL Expert

- Das MetaTrader-Skript wählte über magische Zahlen und Zeitfenster verschiedene Handelssitzungen (Asien, London, New York) aus. Die StockSharp-Version konsolidiert das Verhalten, indem sie eine konfigurierbare Kerze mit höherem Zeitrahmen (standardmäßig täglich) verwendet, um die Pivot-Ebenen abzuleiten, was die Prüfung und Anpassung der Logik über Broker hinweg erleichtert.
- MetaTrader stützte sich bei der Entscheidung zwischen Stop- und Limit-Orders auf den aktuellen Geld-/Briefkurs. Die StockSharp-Implementierung verwendet für diesen Vergleich die zuletzt abgeschlossene Kerze der `EntryCandleType`-Reihe, um den Workflow ereignisgesteuert zu halten.
- Bestellkommentare und magische Zahlen waren in MT4 plattformspezifisch. Sie werden hier absichtlich weggelassen; Stattdessen behält die Strategie direkte Verweise auf ihre ausstehenden Aufträge bei.

## Nutzungshinweise

- Stellen Sie sicher, dass die zugrunde liegende Sicherheit einen gültigen `PriceStep` bereitstellt. Die Strategie löst beim Start eine Ausnahme aus, wenn die Brokerverbindung keine Informationen zur Pip-Größe bereitstellt.
- Um das ursprüngliche Verhalten besser zu reproduzieren, stellen Sie `PivotCandleType` auf eine stündliche Kerzenserie ein, die über Ihre gewünschte Sitzung aggregiert wird, und passen Sie die Offset-/Stopp-Parameter entsprechend an.
- Wie bei jeder Pending-Order-Strategie sollten Sie beim Live-Einsatz die Mindestentfernung und die Ablaufrichtlinien des Brokers für Pending-Orders berücksichtigen.
