# Exp Skyscraper Fix + ColorAML + X2MA Candle MMRec-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- C#-Konvertierung des MetaTrader-Experten **Exp_Skyscraper_Fix_ColorAML_X2MACandle_MMRec**.
- Kombiniert drei unabhängige farbbasierte Filter (Skyscraper Fix-Kanal, ColorAML adaptives Niveau, X2MACandle doppelt geglättete Kerzen).
- Jeder Filter kann eigenständig Trades öffnen oder schließen, während er dasselbe Symbol teilt, was kooperative Trendfolge und schnelle Umkehrungen ermöglicht.
- Beinhaltet ein vereinfachtes Geldverwaltungsmodul: Wenn die letzten Trades einer Richtung wiederholt Verluste machen, wechselt das Modul auf das reduzierte Volumen (`SmallMM`).

## Strategielogik
### Skyscraper Fix-Block
1. Baut den Skyscraper Fix-Trailing-Kanal durch Analyse des ATR-Bereichs und der gewählten Preisquelle (High/Low oder Close).
2. Wenn die Kanalfarbe bullisch wird, tut der Block:
   - schließt jede ausstehende Short-Position (wenn `Skyscraper Close Shorts` aktiviert ist);
   - kann eine neue Long-Position nach der konfigurierten Signalverzögerung öffnen (wenn `Skyscraper Buy` aktiviert ist).
3. Wenn die Farbe bärisch wird, spiegelt die Logik die Schritte für Short-Trades.
4. Die High/Low-Envelopes, der ATR-Multiplikator (`Kv`) und der prozentuale Versatz reproduzieren das Verhalten des ursprünglichen Indikators.

### ColorAML-Block
1. Berechnet das Adaptive Market Level (AML) durch Messung des Bereichs zweier aufeinanderfolgender Fraktal-Fenster und Glättung des zusammengesetzten Preises.
2. Der Indikator gibt drei Farben aus: `2` (bullisch), `0` (bärisch) und `1` (neutral). Neutrale Kerzen lösen keine Aktion aus.
3. Eine bullische Farbe schließt Shorts (wenn erlaubt) und kann einen Long öffnen, wenn die Farbe auf der vorherigen untersuchten Kerze anders war.
4. Eine bärische Farbe führt die symmetrischen Aktionen für Short-Trades aus.

### X2MACandle-Block
1. Kaskadiert zwei konfigurierbare gleitende Durchschnitte über jede OHLC-Komponente (Open, High, Low, Close), um eine synthetische Kerze zu erstellen.
2. Die Farbe wird durch den geglätteten Kerzenkörper bestimmt: bullisch wenn Close > Open, bärisch wenn Close < Open, neutral sonst.
3. Ein kleiner Lücken-Schwellenwert (in Preisschritten) glättet sehr kleine Kerzenkörper, um schnelle Farbwechsel zu vermeiden.
4. Bullische Farben schließen Shorts und können Longs öffnen; bärische Farben machen das Gegenteil.

### Geldverwaltung
1. Jeder Block pflegt eine unabhängige Historie seiner eigenen Trades für Long- und Short-Richtungen.
2. Nachdem ein Trade geschlossen wird, zeichnet das Modul auf, ob er mit einem Verlust endete.
3. Wenn die letzten `Loss Trigger`-Trades für eine Richtung alle Verluste waren, wechselt die nächste Order von diesem Block auf das reduzierte Volumen (`SmallMM`).
4. Wenn ein profitabler oder neutraler Trade die Verlustserie bricht, kehrt das Modul automatisch auf das normale Volumen (`MM`) zurück.

## Parameter
| Abschnitt | Name | Beschreibung | Standard |
| --- | --- | --- | --- |
| Skyscraper | `Skyscraper Candle` | Zeitrahmen für Kerzen des Skyscraper Fix-Indikators. | 4h |
| Skyscraper | `Skyscraper Length` | ATR-Mittelungsfenster (Anzahl der Kerzen). | 10 |
| Skyscraper | `Skyscraper Kv` | Sensitivitätsmultiplikator auf den ATR-Schritt. | 0.9 |
| Skyscraper | `Skyscraper Percentage` | Zusätzlicher Prozentsatz zur/von der Mittellinie. | 0 |
| Skyscraper | `Skyscraper Mode` | Preisquelle (High/Low oder Close) für Envelopes. | High/Low |
| Skyscraper | `Skyscraper Signal Bar` | Anzahl bereits geschlossener Kerzen, die vor dem Reagieren auf eine Farbe gewartet werden. | 1 |
| Skyscraper | `Skyscraper Buy` / `Skyscraper Sell` | Öffnung von Long-/Short-Trades erlauben. | true |
| Skyscraper | `Skyscraper Close Long` / `Skyscraper Close Short` | Diesem Block erlauben, Long-/Short-Trades zu beenden. | true |
| Skyscraper | `Skyscraper Normal Volume` | Basis-Ordervolumen (`MM` im EA). | 0.1 |
| Skyscraper | `Skyscraper Reduced Volume` | Reduziertes Ordervolumen nach einer Verlustserie (`SmallMM`). | 0.01 |
| Skyscraper | `Skyscraper Buy Loss Trigger` / `Skyscraper Sell Loss Trigger` | Anzahl aufeinanderfolgender Verluste, die auf reduziertes Volumen umschalten. | 2 |
| ColorAML | `ColorAML Candle` | Von ColorAML verwendeter Kerzentyp. | 4h |
| ColorAML | `ColorAML Fractal` | Fraktal-Fenster (in Bars) für die Range-Berechnung. | 6 |
| ColorAML | `ColorAML Lag` | Lag-Parameter für die adaptive Glättung. | 7 |
| ColorAML | `ColorAML Signal Bar` | Kerzenoffset vor der Farbbewertung. | 1 |
| ColorAML | `ColorAML Buy` / `ColorAML Sell` | Long-/Short-Einstiege von ColorAML aktivieren. | true |
| ColorAML | `ColorAML Close Long` / `ColorAML Close Short` | ColorAML erlauben, Long-/Short-Positionen zu schließen. | true |
| ColorAML | `ColorAML Normal Volume` / `ColorAML Reduced Volume` | Basis- und reduzierte Volumina für diesen Block. | 0.1 / 0.01 |
| ColorAML | `ColorAML Buy Loss Trigger` / `ColorAML Sell Loss Trigger` | Aufeinanderfolgende Verluste, die reduziertes Volumen aktivieren. | 2 |
| X2MA | `X2MA Candle` | Zeitrahmen für die X2MACandle-Rekonstruktion. | 4h |
| X2MA | `First Method` / `Second Method` | Glättungsmethoden für erste und zweite gleitende Durchschnitte. | SMA / JJMA |
| X2MA | `First Length` / `Second Length` | Perioden der zwei Glättungsstufen. | 12 / 5 |
| X2MA | `First Phase` / `Second Phase` | Kompatibilitätsphasen für Jurik-Gleitende-Durchschnitte. | 15 |
| X2MA | `Gap Points` | Lücken-Schwellenwert (in Preisschritten), der kleine Kerzenkörper glättet. | 10 |
| X2MA | `X2MA Signal Bar` | Kerzenoffset vor der Reaktion auf Farben. | 1 |
| X2MA | `X2MA Buy` / `X2MA Sell` | Öffnung von Long-/Short-Trades aus dem X2MACandle-Block erlauben. | true |
| X2MA | `X2MA Close Long` / `X2MA Close Short` | Dem Block erlauben, Long-/Short-Positionen zu beenden. | true |
| X2MA | `X2MA Normal Volume` / `X2MA Reduced Volume` | Basis- und reduzierte Volumina für X2MACandle-Trades. | 0.1 / 0.01 |
| X2MA | `X2MA Buy Loss Trigger` / `X2MA Sell Loss Trigger` | Anzahl aufeinanderfolgender Verluste vor dem Wechsel auf reduziertes Volumen. | 2 |

## Verwendungstipps
1. Passen Sie Kerzentypen an die Marktvolatilität an (z. B. 1h für Intraday-Handel, 4h für Swing-Handel).
2. Die drei Module können unabhängig abgestimmt werden — das Deaktivieren eines Blocks lässt die anderen aktiv.
3. Die Geldverwaltungsschwellen sind bewusst konservativ gewählt. Erhöhen Sie die Auslöser, wenn das Instrument stark trendet und Sie das Basisvolumen länger aufrechterhalten möchten.
4. Da die Strategie auf abgeschlossenen Kerzen basiert, versorgen Sie sie immer mit Kerzendaten, die den konfigurierten Zeitrahmen entsprechen.
