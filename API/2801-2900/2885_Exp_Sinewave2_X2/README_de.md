# Exp Sinewave2 X2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Exp Sinewave2 X2 ist eine Multi-Timeframe-Trendfolge-Strategie, inspiriert von John Ehlers' Sinewave-Analyse. Der höhere Zeitrahmen-Filter definiert die dominante Richtung, während der niedrigere Zeitrahmen präzise Ein- und Ausstiegsauslöser liefert. Alle Berechnungen verwenden den rekonstruierten Sinewave2-Indikator, der intern auf das adaptive CyclePeriod-Modul angewiesen ist.

## Indikatoren
- **Höherer Zeitrahmen Sinewave2 (Lead vs. Sine-Linie)** – erkennt bullische oder bärische Tendenz mithilfe des Lead-Sine-Kreuzens über die Haupt-Sine-Komponente.
- **Niedrigerer Zeitrahmen Sinewave2** – überwacht die jüngsten Kreuzungsereignisse, um Trades auszulösen, die mit der höheren Zeitrahmensrichtung übereinstimmen.

## Handelslogik
1. **Trendfilter**
   - Sinewave2 auf dem höheren Zeitrahmen berechnen.
   - Lead- und Hauptlinien `SignalBarHigh` Bars zurück auswerten.
   - Trend ist bullisch, wenn `Lead > Sine`, bärisch wenn `Lead < Sine`, sonst neutral.
2. **Einstiegssignale**
   - Auf eine abgeschlossene Kerze auf dem niedrigeren Zeitrahmen warten.
   - Lead- und Sine-Werte bei den durch `SignalBarLow` (aktuell) und `SignalBarLow + 1` (vorherig) definierten Offsets abrufen.
   - Long-Einstieg: vorherige Kreuzung war abwärts (`Lead > Sine` zuvor, `Lead <= Sine` jetzt) während der höhere Zeitrahmen-Trend bullisch ist und `EnableBuyOpen` aktiviert ist.
   - Short-Einstieg: vorherige Kreuzung war aufwärts (`Lead < Sine` zuvor, `Lead >= Sine` jetzt) während der höhere Zeitrahmen-Trend bärisch ist und `EnableSellOpen` aktiviert ist.
3. **Ausstiegsregeln**
   - Niedrigerer Zeitrahmen-Ausstiegs-Booleans `EnableBuyCloseLower` und `EnableSellCloseLower` schließen Positionen bei entgegengesetzten Kreuzungen.
   - Höherer Zeitrahmen-Ausstiegs-Booleans `EnableBuyCloseTrend` und `EnableSellCloseTrend` schließen Positionen sofort, wenn der Haupttrend gegen die offene Richtung dreht.
   - Schutz-Stop-Loss und Take-Profit werden bei jeder Kerze mit intrabar Hochs/Tiefs und den in Preisschritten ausgedrückten Distanzen `StopLossPoints` / `TakeProfitPoints` ausgewertet.
4. **Risikomanagement**
   - Positions-Reversals dimensionieren neue Orders als `Volume + |Position|`, um die bestehende Position zu schließen, bevor die neue aufgebaut wird.
   - Nach jedem Einstieg berechnet `SetRiskLevels` absolute Stop-/Zielpreise unter Verwendung von `Security.PriceStep` (Fallback 1, wenn nicht verfügbar) neu.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `AlphaHigh` | Alpha-Faktor für den höheren Zeitrahmen Sinewave2-Filter. |
| `AlphaLow` | Alpha-Faktor für den niedrigeren Zeitrahmen Sinewave2-Auslöser. |
| `SignalBarHigh` | Anzahl der Bars zurück auf dem höheren Zeitrahmen zum Lesen des Trendzustands. |
| `SignalBarLow` | Anzahl der Bars zurück auf dem niedrigeren Zeitrahmen zum Lesen von Kreuzungszuständen. |
| `EnableBuyOpen` / `EnableSellOpen` | Long/Short-Einstiege von niedrigerem Zeitrahmen-Signalen erlauben. |
| `EnableBuyCloseTrend` / `EnableSellCloseTrend` | Erzwingung von Ausstiegen, wenn der höhere Zeitrahmen gegen die Position dreht. |
| `EnableBuyCloseLower` / `EnableSellCloseLower` | Positionen bei niedrigerem Zeitrahmen-Gegenkategorien schließen. |
| `StopLossPoints` | Stop-Loss-Abstand ausgedrückt in Instrumentenpreisschritten. |
| `TakeProfitPoints` | Take-Profit-Abstand ausgedrückt in Instrumentenpreisschritten. |
| `HigherCandleType` / `LowerCandleType` | Kerzendatentypen (Zeitrahmen) für Filter- und Auslöser-Streams. |

## Hinweise
- Die Strategie verarbeitet nur abgeschlossene Kerzen und ignoriert Teilaktualisierungen.
- Die adaptive Sinewave2-Implementierung verwendet den ursprünglichen CyclePeriod-Algorithmus, um der MQL-Version treu zu bleiben.
- Wenn höhere und niedrigere Kerzentypen identisch sind, teilen sich beide Indikatoren eine einzige Kerzenabonnement, um redundante Datenanfragen zu vermeiden.
- Passen Sie `Volume` in der Basis-`Strategy` an, um die Trade-Größe vor dem Einsatz zu steuern.
