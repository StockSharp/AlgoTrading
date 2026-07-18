# MA-Oszillator-Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine Übersetzung des MQL5-Expert Advisors **Exp_MAOscillatorHist.mq5**. Sie verwendet die Differenz zwischen einem schnellen und einem langsamen Simple Moving Average (SMA), um einen Oszillator zu bilden. Handelssignale werden generiert, wenn der Oszillator lokale Minima oder Maxima bildet, die als potenzielle Trendumkehrungen interpretiert werden.

## Handelslogik
1. Zwei SMAs werden auf dem ausgewählten Kerzen-Zeitrahmen berechnet:
   - **Schneller SMA** mit einem kürzeren Zeitraum.
   - **Langsamer SMA** mit einem längeren Zeitraum.
2. Der Oszillatorwert ist der schnelle SMA minus dem langsamen SMA.
3. Die Strategie verfolgt die letzten drei Oszillatorwerte. Ein lokales Minimum tritt auf, wenn der ältere Wert höher als der vorherige ist und der vorherige Wert niedriger als der aktuelle. Ein lokales Maximum ist das Gegenteil.
4. Wenn ein lokales Minimum erkannt wird:
   - Short-Positionen schließen (wenn erlaubt).
   - Eine neue Long-Position eröffnen (wenn erlaubt).
5. Wenn ein lokales Maximum erkannt wird:
   - Long-Positionen schließen (wenn erlaubt).
   - Eine neue Short-Position eröffnen (wenn erlaubt).

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| **Fast Period** | Zeitraum des schnellen SMA. |
| **Slow Period** | Zeitraum des langsamen SMA. |
| **Enable Buy Open** | Wenn wahr, können Long-Positionen eröffnet werden. |
| **Enable Sell Open** | Wenn wahr, können Short-Positionen eröffnet werden. |
| **Enable Buy Close** | Wenn wahr, können Long-Positionen bei entgegengesetzten Signalen geschlossen werden. |
| **Enable Sell Close** | Wenn wahr, können Short-Positionen bei entgegengesetzten Signalen geschlossen werden. |
| **Candle Type** | Zeitrahmen der für Berechnungen verwendeten Kerzen. |

## Hinweise
- Die Strategie verwendet die hochrangige StockSharp-API mit `SubscribeCandles` und Indikator-Binding.
- `StartProtection` ist mit Marktaufträgen für eine sicherere Ausführung aktiviert.
- Keine Python-Version verfügbar.
