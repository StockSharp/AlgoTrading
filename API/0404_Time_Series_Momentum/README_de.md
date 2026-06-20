# Zeitreihen-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser Ansatz geht bei jedem Asset long oder short, basierend auf seinen eigenen vergangenen Renditen. Ist die zurückliegende Rendite positiv, kauft das Modell; ist sie negativ, verkauft es – und bildet so ein diversifiziertes Trendfolge-Portfolio.

Signale werden monatlich mit einem Einjahres-Rückblick ausgewertet, und die Positionen sind über alle Assets gleichgewichtet.

## Details

- **Daten**: Monatliche Gesamtrenditen je Asset.
- **Einstieg**: Long, wenn 12-Monats-Rendite > 0; Short, wenn < 0.
- **Ausstieg**: Umkehren, wenn das Signal das Vorzeichen wechselt.
- **Instrumente**: Breites Set aus Futures oder ETF.
- **Risiko**: Volatilitätsskalierung und Diversifikation.

