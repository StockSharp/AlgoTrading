# Alligator Volatilitätsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Alligator-Volatilitätsstrategie ist eine hochrangige StockSharp-Portierung des Expertenberaters „Alligator vol 1.1“ MetaTrader. Es kombiniert den Bill Williams' Alligator-Indikator mit optionaler Fraktal-Breakout-Bestätigung, Mittelungsaufträgen im Martingal-Stil und Trailing-Risikomanagement. Das Modul ist für diskretionäre Händler gedacht, die den ursprünglichen Arbeitsablauf automatisieren und gleichzeitig eine detaillierte Kontrolle über Positionsgröße und Filter behalten möchten.

## Logikübersicht

- Abonniert die ausgewählten Zeitrahmenkerzen und berechnet drei geglättete gleitende Durchschnitte (Kiefer, Zähne, Lippen), die den Indikator Alligator bilden.
- Erkennt bullische Phasen, wenn die Lippen mindestens um den konfigurierten `EntryGap` über dem Kiefer und um `ExitGap` über den Zähnen bleiben. Bärische Phasen erfordern, dass der Kiefer die Lippen dominiert und gleichzeitig über den Zähnen bleibt.
- Verfolgt Bill Williams-Fraktale innerhalb der letzten `FractalBars`-Kerzen. The fractal breakout filter is optional and ensures fresh highs for longs or fresh lows for shorts.
- Platziert eine erste Marktorder, sobald ein neuer Alligator-Status erscheint. Wenn Martingal aktiviert ist, werden zusätzliche durchschnittliche Limit-Orders um ein Vielfaches der Stop-Loss-Distanz mit exponentieller Positionsgröße verteilt.
- Verwaltet Positionsausstiege durch Take-Profit, Stop-Loss, optionalen Trailing-Stop und optionale Alligator-Statusumkehr.

## Einreisebestimmungen

1. The strategy waits for finished candles and ignores partial data.
2. Für eine lange Einrichtung ist eine der folgenden Voraussetzungen erforderlich:
   - Wenn der Eintrag Alligator aktiviert ist, wechselt der bullische Zustand von „falsch“ zu „wahr“ und (falls aktiviert) ist ein gültiges oberes Fraktal mindestens `FractalDistancePips` vom aktuellen Schlusskurs entfernt.
   - Alligator-Eintrag deaktiviert, aber (falls aktiviert) besteht die Fraktal-Ausbruchsbedingung weiterhin.
3. Ein Short-Setup spiegelt die Long-Bedingungen unter Verwendung des bärischen Alligator-Zustands und niedrigerer Fraktale wider.
4. The `ManualMode` parameter blocks automatic entries, allowing discretionary order submission through the UI.
5. Wenn `OnlyOnePosition` wahr ist, weigert sich die Strategie, eine neue Position zu eröffnen, wenn bereits ein gegenteiliges Exposure besteht.

## Ausgangsregeln

- Erste Stopps und Ziele werden unmittelbar nach der Positionserhöhung angebracht. Entfernungen werden aus dem durchschnittlichen Einstiegspreis berechnet, indem `StopLossPips` und `TakeProfitPips` mit der Preisstufe des Instruments umgerechnet werden.
- If `EnableTrailing` is true, the stop follows price after the trade gains at least `TrailingActivationPips` of profit. Long-Positionen liegen unter dem höchsten Schluss-/Hochkurs der Kerze, Short-Positionen liegen über dem niedrigsten Schluss-/Tiefkurs.
- Wenn `UseAlligatorExit` aktiv ist, wird die Position geschlossen, sobald der Alligator-Zustand zusammenbricht (der bullische Zustand verschwindet für Long-Positionen oder der bärische Zustand für Short-Positionen).
- Das Erreichen des Take-Profit- oder Stop-Loss-Preises schließt die Position und storniert ausstehende Durchschnittsaufträge auf dieser Seite.

## Martingale Raster

- `EnableMartingale` aktiviert nach dem Markteintritt eine Leiter mit Limit-Orders.
- Jeder Schritt multipliziert das zuvor ausgeführte Volumen mit `2 * MartingaleMultiplier` (begrenzt auf `MaxVolume`).
- Limit prices are spaced by the stop-loss distance (`StopLossPips`) and shifted by `GridSpreadPips` to compensate for the broker spread.
- Ausstehende Aufträge werden storniert, wenn ein neues Signal verarbeitet wird, die Position abgeflacht wird oder ein manueller Ausstieg erfolgt.

## Geldmanagement

- Das Auftragsvolumen wird aus dem Kontoguthaben mit `RiskPerThousand`: `volume = equity / 1000 * RiskPerThousand` berechnet.
- `MinVolume` fungiert als Ersatz, wenn die Eigenkapitalinformationen nicht verfügbar sind. `MaxVolume` begrenzt sowohl die anfänglichen Handels- als auch die Martingal-Schritte.
- Alle Preise werden vor der Übermittlung von Aufträgen auf den nächsten Börsentick gerundet.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Datentyp, der für das Kerzenabonnement verwendet wird. | 15-minütiger Zeitrahmen |
| `ManualMode` | Automatische Einträge deaktivieren, wenn wahr. | `false` |
| `UseAlligatorEntry` | Vor dem Betreten muss die Erweiterung Alligator erfolgen. | `true` |
| `UseFractalFilter` | Erzwingen Sie die Bestätigung eines fraktalen Ausbruchs. | `false` |
| `UseAlligatorExit` | Schließen Sie Trades, wenn der Alligator zusammenbricht. | `false` |
| `OnlyOnePosition` | Erlauben Sie nur eine einzige offene Position. | `true` |
| `EnableMartingale` | Fügen Sie durchschnittliche Limit-Orders hinzu. | `true` |
| `EnableTrailing` | Aktivieren Sie die Trailing-Stop-Verwaltung. | `true` |
| `RiskPerThousand` | Eigenkapitalbasierter Volumenmultiplikator. | `0.04` |
| `MaxVolume` | Maximal zulässige Bestellgröße. | `0.5` |
| `MinVolume` | Fallback order size. | `0.01` |
| `StopLossPips` / `TakeProfitPips` | Entfernung zum Stopp und Ziel in Pips. | `80` |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips. | `30` |
| `TrailingActivationPips` | Erforderlicher Gewinn vor der Nachlaufanpassung. | `20` |
| `EntryGap` | Mindestabstand zwischen Lippen und Kiefer (Preiseinheiten). | `0.0005` |
| `ExitGap` | Mindestabstand zu den Zähnen (Preiseinheiten). | `0.0001` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | SMMA-Längen für die Alligator-Zeilen. | `13 / 8 / 5` |
| `JawShift`, `TeethShift`, `LipsShift` | Bei der Auswertung von Signalen wird eine Balkenverschiebung angewendet. | `8 / 5 / 3` |
| `FractalBars` | Anzahl der nach Fraktalen gescannten Kerzen. | `10` |
| `FractalDistancePips` | Erforderlicher Abstand zwischen Preis und Fraktal. | `30` |
| `MartingaleDepth` | Anzahl der durchschnittlichen Limit-Orders. | `10` |
| `MartingaleMultiplier` | Zusätzlicher Multiplikator zur Mittelung des Volumens. | `1.3` |
| `GridSpreadPips` | Auf das Raster angewendeter Spread-Offset. | `10` |

## Notizen

- The Alligator indicator is processed on candle medians and uses one-bar delays to avoid working with unfinished values.
- `EntryGap` und `ExitGap` werden in absoluten Preiseinheiten ausgedrückt. Passen Sie sie bei Bedarf an die Tick-Größe des Instruments an.
- Die fraktale Erkennung spiegelt das Standardmuster von Bill Williams mit fünf Balken wider. When the filter is active it ignores setups until enough history is collected.
- Die Strategie erstellt keine schützenden Stop- oder Take-Profit-Orders an der Börse. Alle Exits werden intern von der Strategielogik verarbeitet.
- Manuelle Änderungen an ausstehenden oder aktiven Aufträgen werden unterstützt; Die Strategie bereinigt ihre internen Raster, wenn Aufträge ausgeführt oder storniert werden.
