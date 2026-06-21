# LUBE-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie misst die "Reibung" rund um den aktuellen Schlusskurs, indem sie vorherige Kerzen durchsucht. Ein FIR-Filter legt die Trendrichtung fest.

- **Long** wenn die Reibung unter das Auslöseniveau fällt und der Trend aufwärts zeigt.
- **Short** wenn die Reibung unter das Auslöseniveau fällt und der Trend abwärts zeigt.
- **Ausstieg** wenn die Reibung über das mittlere Niveau steigt oder ein gegensätzliches Signal erscheint.

## Details
- **Indikatoren**: benutzerdefinierte Reibungsberechnung, FIR-Filter.
- **Zeitrahmen**: standardmäßig 30m-Kerzen.
- **Beide Seiten**: ja, Shorts optional.
