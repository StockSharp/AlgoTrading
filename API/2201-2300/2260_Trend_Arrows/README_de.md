# Trend-Pfeile-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche, wenn der Schlusskurs sich über kürzliche Extremwerte hinausbewegt.
Sie berechnet die höchsten und niedrigsten Schlusskurse über einen konfigurierbaren Zeitraum.
Ein neuer Aufwärtstrend wird erkannt, wenn der Schlusskurs das jüngste Hoch überschreitet,
während ein Abwärtstrend beginnt, wenn der Schlusskurs unter das jüngste Tief fällt.

Wenn ein neuer Aufwärtstrend erkannt wird, können bestehende Short-Positionen geschlossen und optionale Long-Positionen geöffnet werden.
Umgekehrt ermöglicht ein neuer Abwärtstrend das Schließen von Long-Positionen und optional das Öffnen von Shorts.
Die Strategie verarbeitet nur abgeschlossene Kerzen und verwendet die High-Level-API von StockSharp.

## Parameter
- **Period** – Anzahl der Balken zur Bestimmung kürzlicher Extremwerte.
- **Candle Type** – Zeitrahmen der Kerzen.
- **Open Long** – Long-Positionen eröffnen erlauben.
- **Open Short** – Short-Positionen eröffnen erlauben.
- **Close Long** – Long-Positionen schließen erlauben.
- **Close Short** – Short-Positionen schließen erlauben.

## Logik
1. Kerzendaten des ausgewählten Zeitrahmens abonnieren.
2. Höchste und niedrigste Schlusskurse über den Zeitraum mit den Indikatoren `Highest` und `Lowest` verfolgen.
3. Wenn der Preis über den höchsten Schlusskurs bricht, Aufwärtstrend signalisieren; wenn unter dem niedrigsten Schlusskurs, Abwärtstrend signalisieren.
4. Positionen entsprechend dem neuen Trend und den aktivierten Optionen eingehen oder verlassen.
