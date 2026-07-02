# Rubberbands 3 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters **RUBBERBANDS_3**. Es hält zwei laufende Preisextreme aufrecht, eröffnet zusätzliche Positionen, wenn der Preis um eine konfigurierbare Distanz steigt, und liquidiert die gesamte Sequenz, sobald eine Gegenbewegung einer bestimmten Größe auftritt. Nach einem Retracement wechselt die Strategie optional in die entgegengesetzte Richtung und überwacht dabei ein Gewinn- und Verlustziel auf Sitzungsebene.

> **Hinweis:** StockSharp arbeitet mit saldierten Positionen. Das ursprüngliche MT4-Skript kann Long- und Short-Orders gleichzeitig halten, aber der Port schließt die aktive Sequenz, bevor er die Richtung umkehrt. Das allgemeine Verhalten des Skalierens in Trends und des Abwickelns bei Pullbacks bleibt erhalten.

## Handelslogik

1. Notieren Sie den aktuellen Schlusskurs sowohl als laufendes Maximum als auch als Minimum (oder verwenden Sie gespeicherte Werte beim Neustart erneut).
2. Wenn der Preis um `PipStep` Punkte über das aktuelle Maximum steigt, erteilen Sie eine Marktkauforder der Größe `OrderVolume` und aktualisieren Sie das Maximum auf den neuen Preis.
3. Wenn der Preis um `PipStep` Punkte unter den aktuellen Mindestpreis fällt, übermitteln Sie einen Marktverkaufsauftrag der Größe `OrderVolume` und aktualisieren Sie den Mindestpreis.
4. Wenn der Markt um `BackStep` Punkte gegen die aktive Richtung zurückgeht, schließen Sie alle Positionen in dieser Richtung und richten Sie eine Umkehr ein. Die Gegenseite wird geöffnet, sobald die vorherige Sequenz vollständig liquidiert ist.
5. Überwachen Sie das kumulative Sitzungsergebnis. Wenn der realisierte plus offene Gewinn `SessionTakeProfit` × `OrderVolume` erreicht, schließen Sie die Sitzung. Wenn der Absenkvorgang beim Rückwärtsfahren mehr als `SessionStopLoss` × `OrderVolume` beträgt, schließen Sie ebenfalls alles.
6. Der `QuiesceNow`-Schalter verhindert neue Trades, wenn die Strategie flach ist. Das Flag `StopNow` pausiert die gesamte Logik und `CloseNow` fordert eine sofortige Reduzierung des Portfolios an.

Aufträge werden aus fertigen Kerzen der konfigurierten `CandleType` generiert. Der Standardzeitraum beträgt eine Minute und entspricht dem Timing des ursprünglichen EA, der zu Beginn jeder Minute Prüfungen auslöste.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `OrderVolume` | Basisgröße jeder Marktorder. | `0.02` |
| `MaxOrders` | Maximale Anzahl gleichzeitiger Positionen in einer Richtung. Bei Erreichen des Limits werden weitere Einträge gesperrt. | `10` |
| `PipStep` | Erweiterungsentfernung in Punkten, die einen neuen Handel hinzufügt. | `100` |
| `BackStep` | Gegenbewegung in Punkten, die einen Ausstieg erzwingt und eine Umkehr vorbereitet. | `20` |
| `QuiesceNow` | Bei `true` bleibt die Strategie inaktiv, solange keine Positionen offen sind. | `false` |
| `DoNow` | Öffnet die allererste lange Sequenz unmittelbar nach Beginn der Strategie. | `false` |
| `StopNow` | Hard-Stop-Flag, das jede weitere Verarbeitung verhindert. Bestehende Positionen bleiben unberührt. | `false` |
| `CloseNow` | Fordert eine sofortige flache Position an und löst sequenzielle Schließungen aus. | `false` |
| `UseSessionTakeProfit` | Aktiviert den kumulativen Sitzungs-Take-Profit. | `true` |
| `SessionTakeProfit` | Zielgewinn in Kontowährung pro Lot, das zum Schließen der Sitzung verwendet wird. | `2000` |
| `UseSessionStopLoss` | Aktiviert den kumulativen Sitzungs-Stop-Loss. | `true` |
| `SessionStopLoss` | Maximal tolerierter Verlust pro Los beim Rückwärtsfahren, bevor die Sitzung geschlossen wird. | `4000` |
| `UseInitialValues` | Verwenden Sie beim Neustart die manuell bereitgestellten `InitialMax` und `InitialMin` anstelle des letzten Schlusskurses erneut. | `false` |
| `InitialMax` | Gespeicherter oberer Extremwert wird wiederverwendet, wenn `UseInitialValues` aktiviert ist. | `0` |
| `InitialMin` | Gespeichertes unteres Extremwert wird wiederverwendet, wenn `UseInitialValues` aktiviert ist. | `0` |
| `CandleType` | Von der Strategie verarbeitete Kerzenserie. Standardmäßig werden Ein-Minuten-Kerzen verwendet. | `TimeFrame(1m)` |

## Sitzungsverwaltung

- **Gewinnaggregation:** Realisierte Gewinne werden nach jedem vollständigen Abschluss akkumuliert, während nicht realisierte Gewinne aus den gewichteten durchschnittlichen Einstiegspreisen aller offenen Positionen neu berechnet werden.
- **Sitzungs-Take-Profit:** Sobald `SessionTakeProfit` erreicht ist, schließt die Strategie alle Trades und setzt die gespeicherten Extreme zurück.
- **Session Stop-Loss:** Während einer Umkehrsequenz (`BackStep` ausgelöst) verfolgt die Strategie den schwebenden Verlust. Wenn der Drawdown `SessionStopLoss` überschreitet, werden alle Positionen liquidiert und die Sitzung beginnt mit gelöschten Statistiken neu.

## Nutzungshinweise

- Der Preisschritt, der zum Umrechnen von Punkten in Preise verwendet wird, wird von `Security.PriceStep` übernommen. Konfigurieren Sie die Instrumentenmetadaten entsprechend; andernfalls wird ein Fallback von `0.0001` angewendet.
- Da Aufträge saldiert werden, führt die Strategie Abschlussgeschäfte aus, bevor sie in die entgegengesetzte Richtung eröffnet. Beachten Sie bei der Migration von Altdaten, dass die Bestellhistorie bei abgesicherten Plattformen abweichen kann.
- Die Flagge `DoNow` eröffnet nur die allererste Long-Position. Weitere Einträge folgen den regulären Breakout-Bedingungen.
- Verwenden Sie `QuiesceNow`, wenn Sie die Strategie geladen, aber inaktiv lassen möchten, nachdem sie das Buch reduziert hat.
