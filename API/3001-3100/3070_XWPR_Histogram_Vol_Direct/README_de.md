# Exp XWPR Histogramm Vol Direkt Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist ein StockSharp-Port des MetaTrader Expert Advisors **Exp_XWPR_Histogram_Vol_Direct**. Sie reproduziert den ursprünglichen
Ansatz, Williams %R-Werte nach Volumen zu gewichten, das Ergebnis zu glätten und Trades zu eröffnen, wenn sich die Histogrammsteigung
ändert. Orders werden bei vollständig ausgebildeten Kerzen ausgelöst und verwenden optionale Schutz-Stop-Loss und Take-Profit in Preisschritten.

## Kernlogik

1. Williams %R auf dem ausgewählten Zeitrahmen berechnen.
2. Den Oszillator um +50 verschieben, mit der gewählten Volumenquelle (Tick oder Real) multiplizieren und den Strom mit einem konfigurierbaren
   gleitenden Durchschnitt glätten.
3. Das Rohvolumen mit demselben gleitenden Durchschnitt glätten, um die Indikatorbänder (HighLevel2, HighLevel1, LowLevel1, LowLevel2) wieder aufzubauen.
4. Die Farbe der Histogrammsteigung verfolgen: blau (`0`) wenn der geglättete Wert steigt, magenta (`1`) wenn er fällt. Die Strategie
   hält einen kurzen Historiebuffer, um die letzten zwei abgeschlossenen Farben unter Berücksichtigung des `SignalShift`-Parameters zu vergleichen.
5. Aktionen ausführen, wenn sich die vorherige Farbe ändert:
   - Farbübergang `0 → 1`: Shorts schließen (wenn aktiviert) und optional eine neue Long-Position eröffnen.
   - Farbübergang `1 → 0`: Longs schließen (wenn aktiviert) und optional eine neue Short-Position eröffnen.

Die Zonenklassifikation (Neutral/Bullisch/Bärisch/Extrem) wird für Kontext protokolliert, blockiert aber keine Trades, was dem Verhalten des
ursprünglichen Advisors entspricht, der nur den Farbbuffer liest.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `WilliamsPeriod` | Rückschaulänge für Williams %R. |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Multiplikatoren auf das geglättete Volumen zum Wiederaufbau der Indikatorbänder. |
| `SmoothingType` | Gleitender-Durchschnitt-Familie für sowohl den gewichteten Wert als auch die Volumenströme (SMA, EMA, SMMA, WMA, Hull, VWMA, DEMA, TEMA). |
| `SmoothingLength` | Länge des glättenden gleitenden Durchschnitts. |
| `SignalShift` | Wie viele Balken zurück der Farbbuffer gelesen wird (1 reproduziert den MetaTrader-Standard). |
| `EnableLongEntries` / `EnableShortEntries` | Long/Short-Positionen öffnen erlauben oder sperren. |
| `EnableLongExits` / `EnableShortExits` | Long/Short-Positionen schließen erlauben oder sperren. |
| `VolumeSource` | Zwischen Tick-Anzahl oder Realvolumen für die Gewichtung wählen. |
| `StopLossPoints` / `TakeProfitPoints` | Optionale Schutzziele in Preisschritten ausgedrückt. |
| `CandleType` | Kerzentyp und Zeitrahmen für Analyse und Handel. |

Verwenden Sie die Basis-`Volume`-Eigenschaft der Strategie, um die Einstiegsgröße zu definieren. Positionsumkehrungen werden durch das Senden der absoluten
Positionsmenge plus der konfigurierten Lot-Größe behandelt, ähnlich dem MQL-Expert-Advisor.

## Verwendungshinweise

- Die Glättungsphase (`MA_Phase` in MetaTrader) wird nicht unterstützt, da StockSharp-gleitende Durchschnitte diesen Parameter nicht offenlegen.
- Stellen Sie sicher, dass ausreichend Geschichte für den gewählten Zeitrahmen geladen ist, damit die gleitenden Durchschnitte vollständig ausgebildet sind, bevor das Trading beginnt.
- Die Strategie funktioniert auf jedem von StockSharp unterstützten Instrument; setzen Sie `CandleType` auf die gewünschte Auflösung (zum Beispiel
  4-Stunden-Zeitrahmen, um den ursprünglichen Standardwerten zu entsprechen).
- Die Tick-Volumen-Gewichtung erfordert Datenquellen, die Tick-Anzahlen in Kerzenmeldungen bereitstellen. Andernfalls zu Realvolumen wechseln.

## Protokollierung und Visualisierung

Die Strategie zeichnet Kerzen und den Williams %R-Indikator im Standard-Chartbereich. Handelsaktionen protokollieren die erkannte Zone und den
geglätteten Histogrammwert, um das Debugging und den Vergleich mit der MetaTrader-Referenzimplementierung zu unterstützen.
