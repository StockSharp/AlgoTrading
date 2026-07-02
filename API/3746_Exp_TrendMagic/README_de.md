# Exp TrendMagic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Exp TrendMagic-Strategie ist eine direkte Portierung des MetaTrader 5 Expertenberaters „Exp_TrendMagic“. Es überwacht die Farbänderungen des TrendMagic-Indikators, der einen Commodity Channel Index (CCI) mit einem Average True Range (ATR)-Kanal kombiniert. Wenn der Indikator die Farbe wechselt, schließt die Strategie Positionen auf der Gegenseite und eröffnet optional einen neuen Trade in Richtung des neuen Trends.

Bei der Konvertierung bleiben die ursprünglichen Geldverwaltungsoptionen, der konfigurierbare Signalversatz (`Signal Bar`) und die gleichen Berechtigungsschalter für den Einstieg oder Ausstieg aus Long- und Short-Trades erhalten.

## Handelslogik
1. **Anzeigeeingänge**
   - `CCI` (Commodity Channel Index) mit konfigurierbarem Zeitraum und angewendetem Preis.
   - `ATR` (Average True Range) mit konfigurierbarem Zeitraum.
   - Der TrendMagic-Wert wird wie folgt berechnet:
     - Wenn CCI ≥ 0: `TrendMagic = Low - ATR`, wird geklemmt, um eine Verringerung der Unterstützungslinie zu vermeiden.
     - Wenn CCI < 0: `TrendMagic = High + ATR`, wird geklemmt, um eine Erhöhung der Widerstandslinie zu vermeiden.
   - Die resultierende Linienfarbe ist **0** für bullisch (Unterstützung unter dem Preis) und **1** für bärisch (Widerstand über dem Preis).

2. **Signalauswertung**
   - Die Strategie speichert die Indikatorfarben in chronologischer Reihenfolge, um den MetaTrader-Puffer zu emulieren, und verwendet den `Signal Bar`-Offset, um den zuletzt abgeschlossenen Balken zu lesen.
   - Wenn die vorherige Farbe (`Signal Bar + 1`) **0** ist und die aktuelle Farbe (`Signal Bar`) **1** ist, behandelt der Algorithmus dies als bullischen Wechsel: Er schließt jede Short-Position und eröffnet, sofern zulässig, einen Long-Trade.
   - Wenn die vorherige Farbe **1** und die aktuelle Farbe **0** ist, schließt der Algorithmus alle offenen Long-Positionen und geht, sofern zulässig, einen Short-Trade ein.
   - Die Handelserlaubnis-Flags (`Allow Buy Entry`, `Allow Sell Entry`, `Allow Buy Exit`, `Allow Sell Exit`) folgen der genauen Semantik der MT5-Version.

3. **Geldmanagement**
   - `Money Management` bestimmt, wie viel Kapital pro Trade zugewiesen werden soll. Negative Werte werden als feste Losgröße interpretiert.
   - `Margin Mode` wählt die Interpretation des Geldverwaltungswerts aus:
     - `FreeMargin` / `Balance`: Investieren Sie einen Anteil des Kontokapitals geteilt durch den Preis.
     - `LossFreeMargin` / `LossBalance`: Risiko eines Kapitalanteils im Verhältnis zur Stop-Loss-Distanz.
     - `Lot`: Behandeln Sie den Wert als festes Volumen.
   - Die Volumina werden mit `VolumeStep`, `MinVolume` und `MaxVolume` des ausgewählten Wertpapiers abgeglichen.

4. **Risikomanagement**
   - Wenn ein neuer Trade platziert wird, zeichnet die Strategie den Einstiegspreis auf und erzwingt die ursprünglichen Stop-Loss- und Take-Profit-Abstände (ausgedrückt in Punkten, d. h. Vielfache von `PriceStep`).
   - Durch Erreichen des Stop-Loss oder Take-Profit wird die Position sofort geschlossen und der gespeicherte Einstiegspreis gelöscht.
   - Eine Drossel verhindert das erneute Öffnen einer Position in die gleiche Richtung, bevor sich die nächste Kerze öffnet, und reproduziert so den MQL „Zeitniveau“-Schutz.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `Money Management` | Anteil des Kapitals, der für die Größenbestimmung verwendet wird (negative Werte werden zu einem festen Volumen). |
| `Margin Mode` | Umrechnungsmodus für die Geldverwaltung in Volumen. |
| `Stop Loss` | Schutzstoppabstand in Preispunkten. |
| `Take Profit` | Gewinnziel in Preispunkten. |
| `Deviation` | Reserviert für die Kompatibilität mit dem MT5-Eingang (nicht direkt verwendet). |
| `Allow Buy Entry` / `Allow Sell Entry` | Zwischen langen und kurzen Einträgen umschalten. |
| `Allow Buy Exit` / `Allow Sell Exit` | Schalten Sie das Schließen von Short-/Long-Trades um. |
| `Candle Type` | Hauptzeitrahmen für Indikatoren und Signalauswertung. |
| `CCI Period` / `CCI Price` | CCI Länge und angewendete Preisquelle. |
| `ATR Period` | ATR Länge. |
| `Signal Bar` | Index des fertigen Balkens, der für Signale verwendet wird (0 = aktuell, 1 = vorhergehend usw.). |

## Notizen
- Die Strategie basiert nur auf fertigen Kerzen (`CandleStates.Finished`), um die tickbasierte MT5-Implementierung nachzuahmen.
- Beim Neustart der Strategie werden alle Indikatorwerte und Zustandsvariablen zurückgesetzt, um deterministische Optimierungsläufe sicherzustellen.
- Der Parameter `Deviation` wird aus Gründen der vollständigen Kompatibilität bereitgestellt, auch wenn Marktaufträge von StockSharp keinen expliziten Slippage-Parameter verwenden.
