# SuperATR 7-Stufen-Gewinn-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert einen adaptiven ATR-Trendfilter mit einem siebenstufigen Gewinnmitnahme-System. Momentum-normalisierter ATR definiert die Trendstärke, während Einstiege erfolgen, wenn der kurze gleitende Durchschnitt mit der bestätigten Trendrichtung übereinstimmt.

- **Long**: Trendstärke über Schwellenwert, Preis über kurzem MA und kurzer MA über langem MA.
- **Short**: Trendstärke unter negativem Schwellenwert, Preis unter kurzem MA und kurzer MA unter langem MA.
- **Indikatoren**: Momentum, Standard Deviation, SMA, ATR.
- **Gewinnmitnahme**: Vier ATR-basierte Niveaus und drei Festprozent-Niveaus, jedes schließt bei Aktivierung einen Teil der Position.

