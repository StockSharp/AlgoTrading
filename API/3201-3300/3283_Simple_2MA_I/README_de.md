# Simple-2-MA-I-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Simple 2 MA I ist eine Trendfolge-Strategie, die die Kernlogik des ursprünglichen MetaTrader Expert Advisors repliziert. Sie verwendet ein Paar linear gewichteter gleitender Durchschnitte (LWMAs), die auf typischen Preisen berechnet werden, um den dominanten Trend zu erkennen. Momentum-Bestätigung und MACD-Richtungsfilter entfernen schwache Signale. Optional verwaltet die Strategie Risiko über automatische Stop-Loss-, Take-Profit-, Break-even-Bewegungen und kerzenbasierte Trailing Stops.

## Handelslogik

### Long-Setup

1. Die schnelle LWMA liegt über der langsamen LWMA und bestätigt einen Aufwärtstrend.
2. Das Tief der Kerze vor zwei Bars liegt unter dem Hoch der vorherigen Bar und signalisiert frische bullische Struktur.
3. Mindestens eine der letzten drei Rate-of-Change-Messungen liegt über dem konfigurierten Momentum-Schwellenwert.
4. Die MACD-Linie liegt über der Signallinie.
5. Das Nettopositionsvolumen liegt unter dem Limit `Max Net Volume`.

Wenn alle Bedingungen erfüllt sind, schließt die Strategie Short-Exposure (falls vorhanden) und kauft zum Markt.

### Short-Setup

1. Die schnelle LWMA liegt unter der langsamen LWMA und bestätigt einen Abwärtstrend.
2. Das Tief der vorherigen Bar liegt unter dem Hoch der Bar vor zwei Perioden und zeigt bärische Struktur.
3. Mindestens eine der letzten drei Rate-of-Change-Messungen liegt über dem Momentum-Schwellenwert (absoluter Wert).
4. Die MACD-Linie liegt unter der Signallinie.
5. Das Nettopositionsvolumen liegt unter `Max Net Volume`.

Wenn die Bedingungen gelten, deckt die Strategie Longs (falls vorhanden) und verkauft zum Markt.

### Risikomanagement

* **Stop-Loss / Take-Profit:** optionale feste Distanzen in Punkten relativ zum Einstiegspreis.
* **Break-even:** Sobald der Preis die Trigger-Distanz im Gewinn erreicht, wird der Stop auf Einstieg ± Offset verschoben.
* **Kerzen-Trailing:** Nach Erreichen der Aktivierungsdistanz folgt der Stop Kerzenextremen mit einem konfigurierbaren Puffer.
* Schutzorders werden automatisch storniert, sobald die Position geschlossen ist.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ------------ | -------- |
| Candle Type | Zeitrahmen für Indikatorberechnungen. | 15-Minuten-Kerzen |
| Fast LWMA | Periode der schnellen LWMA. | 6 |
| Slow LWMA | Periode der langsamen LWMA. | 85 |
| Momentum Length | Rückblickperiode für den Rate-of-Change-Indikator. | 14 |
| Momentum Threshold | Erforderlicher minimaler absoluter Rate-of-Change-Wert. | 0.3 |
| MACD Fast | Schnelle EMA-Länge in MACD. | 12 |
| MACD Slow | Langsame EMA-Länge in MACD. | 26 |
| MACD Signal | Signal-EMA-Länge in MACD. | 9 |
| Use Stop-Loss | Aktiviert das Platzieren von Stop-Loss-Orders. | true |
| Stop-Loss (points) | Distanz vom Einstiegspreis zum Stop-Loss. | 20 |
| Use Take-Profit | Aktiviert das Platzieren von Take-Profit-Orders. | true |
| Take-Profit (points) | Distanz vom Einstiegspreis zum Take-Profit. | 50 |
| Use Break-Even | Aktiviert die automatische Break-even-Bewegung. | true |
| Break-Even Trigger | Gewinn (Punkte), der vor Break-even erforderlich ist. | 30 |
| Break-Even Offset | Offset (Punkte), der beim Verschieben auf Break-even hinzugefügt wird. | 30 |
| Use Candle Trailing | Aktiviert Trailing Stops auf Basis von Kerzenextremen. | true |
| Trailing Activation | Gewinn (Punkte), der vor Aktivierung von Trailing erforderlich ist. | 40 |
| Trailing Padding | Zusätzliche Distanz (Punkte), die zum Kerzenextrem hinzugefügt wird. | 10 |
| Max Net Volume | Maximal zulässiges absolutes Nettovolumen. | 1 |

## Hinweise

* Alle Preisdistanzen werden in Wertpapier-Preisschritten (Punkten) ausgedrückt. Die Strategie multipliziert Parameterwerte automatisch mit der Tickgröße des Wertpapiers.
* Die Standard-Zeitrahmenzuordnung folgt den ursprünglichen Expert-Defaults, kann aber frei angepasst werden.
* Die Strategie erwartet abgeschlossene Kerzen. Unvollendete Bars werden ignoriert, um mit dem Quell-EA konsistent zu bleiben.
