# Estrategia Exp Candles XSmoothed
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia monitorea los máximos y mínimos de las velas suavizados por una media móvil ponderada (WMA). Cuando el precio de cierre rompe por encima del máximo suavizado más un búfer configurable, abre una posición larga y cierra cualquier posición corta existente. A la inversa, un cierre por debajo del mínimo suavizado menos el búfer abre una posición corta y cierra cualquier posición larga existente.

## Parámetros
- **MA Length** – número de períodos para las medias móviles ponderadas aplicadas a los máximos y mínimos.
- **Level** – búfer de ruptura en puntos añadido al máximo suavizado y restado del mínimo suavizado.
- **Candle Type** – marco temporal de las velas utilizadas para el análisis.
- **Buy Open / Sell Open** – permisos para abrir posiciones largas o cortas.
- **Buy Close / Sell Close** – permisos para cerrar posiciones existentes cuando ocurre una ruptura opuesta.

La estrategia dibuja líneas de máximos y mínimos suavizados en el gráfico para confirmación visual y utiliza la protección de posición integrada una vez iniciada.
