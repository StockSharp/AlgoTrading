# ADX MACD Deev-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **ADX MACD Deev-Strategie** ist ein StockSharp-Port des MetaTrader-Expertenberaters mit demselben Namen. Sie kombiniert das Trendstärkesignal des Average Directional Index (ADX) mit der Momentum-Bestätigung des Moving Average Convergence Divergence (MACD). Die Strategie handelt nur, wenn beide Indikatoren in der Marktrichtung übereinstimmen, und kann Gewinne optional durch Trailing Stops und partielle Positionsausstiege sichern.

## Funktionsweise
1. **Indikatorvorbereitung**
   - ADX wird mit einer konfigurierbaren Mittelungsperiode berechnet. Die Strategie verfolgt die neuesten ADX-Werte und erfordert, dass sie sich konsistent in eine Richtung bewegen, bevor ein Trade erlaubt wird.
   - MACD verwendet konfigurierbare schnelle, langsame und Signal-EMAs. Das Histogramm und die Signallinie müssen gemeinsam ein anhaltendes Wachstum für Longs oder einen anhaltenden Rückgang für Shorts zeigen.
2. **Einstiegslogik**
   - **Long-Einstiege**: ausgelöst, wenn das MACD-Histogramm den `MACD Minimum (pips)`-Schwellenwert überschreitet, sowohl MACD-Histogramm als auch Signallinie für die gewählte Anzahl von Bars zunehmen und ADX über der erforderlichen Stärke bleibt und ebenfalls steigt.
   - **Short-Einstiege**: ausgelöst, wenn das MACD-Histogramm unter dem negativen Schwellenwert liegt, sowohl MACD-Histogramm als auch Signallinie über das gewählte Intervall sinken und ADX über dem Minimum bleibt, während es abnimmt.
   - Es kann jeweils nur eine Position offen sein.
3. **Risikomanagement**
   - Anfängliche Stop-Loss- und Take-Profit-Niveaus werden in Preiseinheiten platziert, die aus dem Instrument `PriceStep` und den gewählten Pip-Distanzen abgeleitet werden.
   - Ein Trailing Stop kann profitable Positionen verfolgen, sobald der Preis um `Trailing Stop + Trailing Step` Pips vorgerückt ist.
   - Wenn `Take Half Profit` aktiviert ist, schließt die Strategie die Hälfte der aktuellen Position auf dem Take-Profit-Niveau und lässt den Rest mit dem Trailing Stop laufen.

## Parameter
| Gruppe | Name | Beschreibung |
| --- | --- | --- |
| Trading | Order Volume | Volumen jeder neuen Market-Order. |
| Risiko | Stop Loss (pips) | Anfänglicher Stop-Loss-Abstand vom Einstieg. |
| Risiko | Take Profit (pips) | Anfänglicher Take-Profit-Abstand vom Einstieg. |
| Risiko | Trailing Stop (pips) | Trailing-Stop-Distanz. Auf null setzen, um Trailing zu deaktivieren. |
| Risiko | Trailing Step (pips) | Zusätzliche Preisbewegung, bevor der Trailing Stop sich wieder bewegt. |
| Risiko | Take Half Profit | Aktiviert den Teilausstieg, wenn das Take-Profit-Niveau erreicht ist. |
| Indikatoren | ADX Period | ADX-Mittelungsperiode. |
| Indikatoren | ADX Bars Interval | Anzahl der jüngsten ADX-Bars, die in eine Richtung tendieren müssen. |
| Indikatoren | ADX Minimum | Minimaler ADX-Wert, der für Einstiege erforderlich ist. |
| Indikatoren | MACD Fast EMA | Schnelle EMA-Länge für MACD. |
| Indikatoren | MACD Slow EMA | Langsame EMA-Länge für MACD. |
| Indikatoren | MACD Signal EMA | Signal-EMA-Länge für MACD. |
| Indikatoren | MACD Bars Interval | Anzahl der MACD-Bars, die in dieselbe Richtung ausgerichtet sein müssen. |
| Indikatoren | MACD Minimum (pips) | Minimale MACD-Magnitude in Pips konvertiert. |
| Allgemein | Candle Type | Kerzentyp oder Zeitrahmen für Berechnungen. |

## Verwendungshinweise
- Die Strategie benötigt Instrumente mit einem gültigen `PriceStep`. Wenn `PriceStep` null ist, fallen die pip-basierten Schwellenwerte auf rohe MACD-Werte zurück.
- Die Volumenrundung für Teilausstiege folgt dem `VolumeStep` des Instruments.
- Trailing-Stop-Anpassungen werden nur auf geschlossenen Kerzen bewertet.
- Die Strategie verwendet High-Level-API-Bindungen (`SubscribeCandles().BindEx(...)`) und basiert nicht auf manuellem Indikatorwert-Polling.
