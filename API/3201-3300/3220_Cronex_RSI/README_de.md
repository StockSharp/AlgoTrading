# Cronex RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Cronex RSI-Strategie** bildet den Exp_CronexRSI.mq5 Experten-Advisor auf der StockSharp High-Level-API nach. Der Indikatorstack kombiniert einen klassischen Relative Strength Index (RSI) mit zwei sequentiellen gleitenden Durchschnitten zur Rauschreduzierung. Handelsentscheidungen basieren auf Crossovern zwischen den schnellen und langsamen geglätteten RSI-Kurven, mit konfigurierbaren Ein-/Ausstiegsberechtigungen, die den ursprünglichen MQL5-Parametern entsprechen.

## Handelslogik

1. Den RSI aus dem ausgewählten angewandten Preis und dem Rückblickzeitraum aufbauen.
2. Den RSI-Wert mit einem *schnellen* gleitenden Durchschnitt glätten, dann das Ergebnis mit einem *langsamen* gleitenden Durchschnitt glätten.
3. Crossover mit einem konfigurierbaren Bestätigungs-Shift auswerten:
   - Wenn die schnelle Kurve eine Bar früher über der langsamen Kurve lag und auf der bestätigten Bar darunter fällt, schließt die Strategie Short-Positionen und öffnet, wenn aktiviert, eine Long-Position.
   - Wenn die schnelle Kurve unter der langsamen Kurve lag und auf der bestätigten Bar darüber kreuzt, schließt die Strategie Longs und kann Short-Trades eingehen.
4. Volumen sind in beiden Richtungen symmetrisch. Wenn ein neues Signal die Position umkehrt, deckt die Strategie zunächst die bestehende Exposition ab und öffnet dann die neue Seite mit dem konfigurierten Basisvolumen.

Standardmäßig wartet die Strategie auf eine vollständig geschlossene Kerze, bevor sie auf ein Signal reagiert, und reproduziert das `SignalBar = 1`-Verhalten von Exp_CronexRSI. Den Shift auf null setzen verarbeitet den Crossover sofort auf der Schlusskursbalk.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `RsiPeriod` | RSI-Rückblickzeitraum. |
| `FastPeriod` | Länge des schnellen Glättungs-Durchschnitts. |
| `SlowPeriod` | Länge des zweiten Glättungs-Durchschnitts. |
| `SignalShift` | Anzahl abgeschlossener Balken zur Bestätigung (0 reagiert sofort). |
| `SmoothingMethod` | Gleitender-Durchschnitt-Typ während beider Glättungsstufen (einfach, exponentiell, geglättet, linear gewichtet, volumengewichtet). |
| `AppliedPrice` | Preiskomponente, die an den RSI übergeben wird (Schlusskurs, Eröffnung, Hoch, Tief, Median, typisch, gewichtet). |
| `CandleType` | Kerzenserie, die von der Strategie verarbeitet wird. |
| `TradeVolume` | Basis-Ordergröße für neue Einstiege. |
| `EnableLongEntry` / `EnableShortEntry` | Öffnen von Long-/Short-Positionen erlauben. |
| `EnableLongExit` / `EnableShortExit` | Schließen von Positionen als Reaktion auf entgegengesetzte Signale erlauben. |

## Implementierungshinweise

- Die Glättungsmethode verwendet StockSharp-Klassen für gleitende Durchschnitte. Die Option `VolumeWeighted` deckt auch die VIDYA/AMA-Stile von MQL5 ab, indem ein pragmatischer volumengewichteter Ersatz angewendet wird.
- Die Auswahl des angewandten Preises entspricht den Cronex-Indikatoreinstellungen und spiegelt den Helfer wider, der im ursprünglichen Experten-Advisor verwendet wird.
- Alle Indikatorwerte werden durch `DecimalIndicatorValue`-Instanzen verarbeitet, um mit der Indikator-Pipeline von StockSharp kompatibel zu bleiben und direktes Wert-Polling zu vermeiden.
- Die Strategie skaliert ihren internen Verlauf automatisch, wenn sich der Bestätigungs-Shift ändert, und stellt sicher, dass die Crossover-Logik die genaue Rückblickstruktur der MQL5-Version beibehält.

## Verwendung

1. Die Strategie einem Portfolio und Wertpapier im StockSharp-Designer oder per Code zuordnen.
2. Zeitrahmen, Glättungsstil und Handelsberechtigungen konfigurieren, um dem bevorzugten Cronex RSI-Setup zu entsprechen.
3. Die Strategie starten. Sie abonniert die ausgewählte Kerzenserie, aktualisiert die RSI/MA-Kombination und sendet Market-Orders bei bestätigten Crossovern.
4. Die integrierten Diagramm-Helfer verwenden, um Indikator-Kurven und ausgeführte Trades zur weiteren Validierung zu visualisieren.
