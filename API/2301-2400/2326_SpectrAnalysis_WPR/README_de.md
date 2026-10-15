# SpectrAnalysis WPR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wurde aus dem MQL5-Expert *Exp_i-SpectrAnalysis_WPR* konvertiert.
Sie analysiert die Richtung des Williams %R-Indikators und öffnet oder schließt Positionen entsprechend den Indikatordrehungen.

## Logik

1. Kerzen des ausgewählten Zeitrahmens abonnieren.
2. Williams %R mit der konfigurierten Periode berechnen.
3. Die letzten zwei Indikatorwerte aufbewahren, um die Auf- oder Abwärtsrichtung zu erkennen.
4. Wenn der Indikator nach oben dreht und Long-Einstiege erlaubt sind:
   - Short-Positionen schließen, wenn aktiviert.
   - Eine neue Long-Position eröffnen.
5. Wenn der Indikator nach unten dreht und Short-Einstiege erlaubt sind:
   - Long-Positionen schließen, wenn aktiviert.
   - Eine neue Short-Position eröffnen.

Es werden nur abgeschlossene Kerzen verarbeitet. Die Strategie verwendet keine komplexen historischen Abfragen und nutzt High-Level-API-Bindungen.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Candle Type` | Zeitrahmen der für Berechnungen verwendeten Kerzen | `4h` |
| `WPR Period` | Periode des Williams %R-Indikators | `13` |
| `Allow Long Entry` | Eröffnen von Long-Positionen erlauben | `true` |
| `Allow Short Entry` | Eröffnen von Short-Positionen erlauben | `true` |
| `Allow Long Exit` | Schließen von Long-Positionen erlauben | `true` |
| `Allow Short Exit` | Schließen von Short-Positionen erlauben | `true` |

## Hinweise

Die ursprüngliche MQL-Version wandte Spektralanalyse auf die Williams %R-Ausgabe an.
Diese C#-Konvertierung verwendet den Standard-Williams %R-Indikator und repliziert die Signallogik durch Verfolgung der jüngsten Indikatorwerte.
