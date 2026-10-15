# Arpit Bollinger Band-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger Band-Ausbruchsstrategie, die auf einen Schlusskurs außerhalb der Bänder vor zwei Kerzen wartet und einsteigt, wenn der Preis das Extrem dieser Kerze durchbricht.

- **Indikatoren**: Bollinger Bands (EMA 20, Abweichung 1.5)
- **Einstieg**: Long, wenn der Preis vor zwei Kerzen unterhalb des unteren Bandes schloss und das aktuelle Hoch das Hoch dieser Kerze übersteigt. Short, wenn der Preis oberhalb des oberen Bandes schloss und das aktuelle Tief unter das Tief dieser Kerze fällt.
- **Stops**: Stop jenseits des aktuellen Kerzbereichs mit einem 5%-Puffer platziert und Take-Profit basierend auf einem Risiko‑Rendite-Verhältnis.

