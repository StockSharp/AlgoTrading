# Estrategia Exp ADX Cross Hull Style
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina señales de cruce del Average Directional Index (ADX) con un filtro de Hull Moving Average (HMA). Las entradas ocurren cuando la línea +DI cruza por encima de la línea -DI para posiciones largas o por debajo para posiciones cortas. Las salidas son gestionadas por un par de medias móviles Hull: la media rápida cruzando la media lenta cierra las posiciones. La estrategia opera en el marco temporal de 4 horas por defecto.

## Detalles
- **Criterios de entrada**  
  - **Largo**: +DI cruza por encima de -DI.  
  - **Corto**: -DI cruza por encima de +DI.
- **Criterios de salida**  
  - **Largo**: HMA rápida cae por debajo de HMA lenta.  
  - **Corto**: HMA rápida sube por encima de HMA lenta.
- **Indicadores**  
  - AverageDirectionalIndex (período 14).  
  - HullMovingAverage longitud rápida 20.  
  - HullMovingAverage longitud lenta 50.
- **Marco temporal**: velas de 4 horas (configurable).
- **Stops**: ninguno por defecto.
- **Dirección**: largo y corto.

La estrategia no depende de colecciones históricas; reacciona a datos de velas en streaming. Los parámetros pueden optimizarse para diferentes mercados. La salida del gráfico muestra velas de precio con ambas medias móviles Hull y marcas de operaciones.
