# Balance Of Power Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Anpassung des ursprünglichen MetaTrader-Experts aus `MQL/16214`. Sie verwendet den **Balance of Power** (BOP)-Indikator, um Momentum-Änderungen im Markt zu erkennen.

## Logik

1. Die Strategie berechnet den Balance of Power für jede abgeschlossene Kerze:
   
   $$BOP = \frac{Close - Open}{High - Low}$$
2. Drei aufeinanderfolgende BOP-Werte werden verglichen.
   - Wenn der vorherige Wert niedriger als der davor liegende Wert ist und der aktuelle Wert höher als der vorherige, dreht BOP nach oben und die Strategie eröffnet eine Long-Position.
   - Wenn der vorherige Wert höher als der davor liegende Wert ist und der aktuelle Wert niedriger als der vorherige, dreht BOP nach unten und die Strategie eröffnet eine Short-Position.
3. Die Position wird nur nach einer abgeschlossenen Kerze geändert, um Fehlsignale zu vermeiden.

## Parameter

- **CandleType** – Zeitrahmen der Kerzen für Berechnungen. Standard sind Vier-Stunden-Kerzen.

## Hinweise

Dieser Port konzentriert sich auf das Kernverhalten der ursprünglichen Strategie und implementiert nicht die erweiterten Geldmanagement-Optionen der MQL-Version.
