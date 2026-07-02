# Äquidistanzkanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Äquidistanzkanal-Strategie** portiert den ursprünglichen MQL4 Expert Advisor "Equidistant Channel" auf die High-Level-API von StockSharp. Die Strategie analysiert Kreuzungen der MACD-Linie und verwaltet bestehende Positionen über Berührungen der Bollinger Bands, Breakeven-Logik und geldbasierte Trailing-Ziele.

Wenn die MACD-Linie ihre Signallinie nach oben kreuzt, eröffnet die Strategie Long-Positionen; wenn sie die Signallinie nach unten kreuzt, eröffnet sie Short-Positionen. Während ein Trade aktiv ist, überwacht die Strategie Ausstiege, wenn der Preis Bollinger Bands erreicht, wenn schwebender Gewinn konfigurierbare monetäre oder prozentuale Ziele erreicht oder wenn ein Trailing-Drawdown-Schwellenwert verletzt wird. Ein Breakeven-Modus spiegelt die MetaTrader-Implementierung, indem er den Schutz-Stop verschiebt, sobald der Gewinn eine konfigurierbare Anzahl von Preisschritten überschreitet.

## Indikatoren
- **MACD (12, 26, 9)** - erzeugt Einstiegssignale bei Kreuzungen zwischen der MACD-Linie und ihrer Signallinie.
- **Bollinger Bands (20, 2)** - liefern Ausstiegsniveaus, sobald der Kerzenschluss das obere oder untere Band trifft.

## Positionsverwaltung
- Optionale Stop-Loss-, Take-Profit- und Trailing-Stop-Distanzen, die über `StartProtection` in Preispunkten ausgedrückt werden.
- Geldbasierte Take-Profit- und Trailing-Logik, die schwebenden Gewinn anhand der Preis-/Schrittgrößenmetadaten des Instruments verfolgt.
- Prozentualer Take-Profit, berechnet aus dem Startwert des Portfolios.
- Breakeven-Modus, der den Stop auf Einstieg plus Offset verschiebt, sobald der Gewinn einen definierten Trigger erreicht.

## Parameter
| Gruppe | Name | Standard | Beschreibung |
| --- | --- | --- | --- |
| Handel | Volumen | 1 | Ordervolumen für neue Einstiege. |
| Allgemein | Kerzentyp | 5 Minuten | Für Berechnungen verwendete Kerzenserie. |
| Indikatoren | MACD Schnell | 12 | Länge der schnellen EMA für MACD. |
| Indikatoren | MACD Langsam | 26 | Länge der langsamen EMA für MACD. |
| Indikatoren | MACD Signal | 9 | Länge der Signallinie für MACD. |
| Indikatoren | BB-Periode | 20 | Rückblickperiode der Bollinger Bands. |
| Indikatoren | BB-Abweichung | 2 | Breite der Bollinger Bands in Standardabweichungen. |
| Risiko | Stop Loss | 20 | Stop-Loss-Distanz in Preispunkten. |
| Risiko | Take Profit | 50 | Take-Profit-Distanz in Preispunkten. |
| Risiko | Trailing Stop | 40 | Trailing-Stop-Distanz in Preispunkten. |
| Risiko | TP verwenden (Geld) | false | Schließt, wenn schwebender Gewinn ein absolutes Geldziel erreicht. |
| Risiko | TP Geld | 10 | Absoluter Take-Profit-Wert in Kontowährung. |
| Risiko | TP verwenden (%) | false | Schließt, wenn schwebender Gewinn einen Prozentsatz des Anfangskapitals erreicht. |
| Risiko | TP Prozent | 10 | Prozentsatz des Anfangskapitals für den prozentualen Take-Profit. |
| Risiko | Trailing aktivieren | true | Aktiviert Trailing-Logik auf schwebendem Gewinn. |
| Risiko | Trailing aktivieren ab | 40 | Gewinnniveau (Währung), das die Trailing-Logik scharf schaltet. |
| Risiko | Trailing-Schritt | 10 | Maximal zulässiger Drawdown vom Gewinnhoch (Währung). |
| Risiko | BB Stop verwenden | true | Aktiviert Ausstiege, wenn der Preis Bollinger Bands berührt. |
| Risiko | Breakeven verwenden | true | Aktiviert das Breakeven-Verhalten. |
| Risiko | Breakeven-Trigger | 10 | Gewinn (Preisschritte), der zum Scharfschalten des Breakeven-Stops erforderlich ist. |
| Risiko | Breakeven-Offset | 5 | Offset (Preisschritte), der auf das Breakeven-Niveau angewendet wird. |

## Hinweise
- Die Strategie arbeitet mit einem einzelnen Instrument, das gültige `PriceStep`- und `StepPrice`-Metadaten bereitstellt, damit monetäre Berechnungen genau sind.
- Das Trailing-Profit-Modul folgt dem MetaTrader-Verhalten: Sobald schwebender Gewinn die Aktivierungsschwelle überschreitet, zeichnet die Strategie das laufende Maximum auf und schließt den Trade, wenn der Drawdown den konfigurierten Trailing-Schritt übersteigt.
- Die Breakeven-Logik spiegelt den ursprünglichen EA, indem sie Trigger und Offsets auf Basis von Preisschritten verwendet.
- Alle Kommentare im Strategiecode sind gemäß den Projektrichtlinien auf Englisch geschrieben.
