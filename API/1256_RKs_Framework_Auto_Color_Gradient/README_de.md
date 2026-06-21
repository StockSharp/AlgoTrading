# RK's Framework Auto Color Gradient-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert Bollinger Bands %B und RSI zu einem einzelnen Oszillator, bildet ihn auf einen Farbverlauf ab und handelt, wenn er die Mittellinie kreuzt.

## Logik
- Berechnet Bollinger Bands %B und den Relative Strength Index.
- Normalisiert beide mit einem stochastischen Prozess und mittelt sie.
- Konvertiert das Ergebnis in einen auswählbaren Farbverlauf.
- Kauft, wenn der gemittelte Wert über null liegt.
- Verkauft, wenn der gemittelte Wert unter null liegt.
