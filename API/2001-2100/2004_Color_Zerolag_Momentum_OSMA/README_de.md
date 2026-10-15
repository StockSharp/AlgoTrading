# Color Zerolag Momentum OSMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erstellt einen benutzerdefinierten Zero-Lag-Momentum-OSMA-Oszillator aus fünf Momentum-Berechnungen.
Wenn der Oszillatorwert vor zwei Balken unter dem Wert vor drei Balken liegt, gilt der Trend als aufwärtsgerichtet.
In diesem Fall werden Short-Positionen geschlossen und eine neue Long-Position kann eröffnet werden, wenn der aktuellste Wert über dem Wert vor zwei Balken liegt.
Wenn der Wert vor zwei Balken über dem Wert vor drei Balken liegt, ist der Trend abwärtsgerichtet, Long-Positionen werden geschlossen, und eine Short-Position kann eröffnet werden, wenn der letzte Wert unter dem Wert vor zwei Balken liegt.

## Parameter

- `Smoothing1` – erster Glättungsfaktor für den langsamen Trend.
- `Smoothing2` – zweiter Glättungsfaktor für die OSMA-Linie.
- `Factor1-5` – Gewichte für jede Momentum-Komponente.
- `MomentumPeriod1-5` – Perioden für die Momentum-Indikatoren.
- `CandleType` – Kerzen-Zeitrahmen für Berechnungen.
- `BuyOpen` – Long-Positionen eröffnen erlauben.
- `SellOpen` – Short-Positionen eröffnen erlauben.
- `BuyClose` – Long-Positionen schließen erlauben.
- `SellClose` – Short-Positionen schließen erlauben.
