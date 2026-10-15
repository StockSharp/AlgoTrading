# Strategie Volumen-Gewichtete MA Standardabweichung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wendet einen volumengewichteten gleitenden Durchschnitt (VWMA) mit einem Standardabweichungsfilter an. Sie misst den Impuls der VWMA und eröffnet eine Long-Position, wenn die Aufwärtsbewegung einen konfigurierbaren Abweichungsschwellenwert überschreitet. Eine Short-Position wird eröffnet, wenn die Abwärtsbewegung den negativen Schwellenwert kreuzt. Der Ansatz versucht, starke Richtungsbewegungen zu erfassen, die durch das Volumen bestätigt werden.

## Parameter
- Kerzentyp
- VWMA-Länge
- StdDev-Periode
- K1
- K2
