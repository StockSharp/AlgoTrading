# Drei-Rote / Drei-Grüne-Strategie mit ATR-Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eröffnet eine Long-Position nach drei aufeinanderfolgenden bärischen Kerzen, wenn der ATR über seinem 30-Perioden-SMA liegt. Steigt nach drei bullischen Kerzen aus oder wenn die maximale Handelsdauer erreicht ist.

## Parameter

- **CandleType**: Kerzentyp.
- **MaxTradeDuration**: Maximale Anzahl von Bars für eine offene Position.
- **UseGreenExit**: Ob nach drei grünen Kerzen ausgestiegen werden soll.
- **AtrPeriod**: Periode für die ATR-Berechnung (0 deaktiviert den Filter).
