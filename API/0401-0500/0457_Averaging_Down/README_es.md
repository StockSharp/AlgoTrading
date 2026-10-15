# Estrategia de Promediado a la Baja
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre una posición cuando el precio se mueve fuera de una banda basada en ATR alrededor del EMA. Si el mercado se mueve en contra de la posición, la estrategia añade a ella usando desviaciones porcentuales escalonadas (DCA). Se toma ganancia cuando el precio vuelve a la entrada promediada más un porcentaje fijo.

## Parámetros
- Candle Type – tipo de velas a procesar.
- EMA Length – período para el filtro de tendencia EMA.
- ATR Length – período para ATR.
- ATR Mult – multiplicador para las bandas ATR.
- TP % – porcentaje de toma de ganancias desde la entrada promedio.
- Base Deviation % – desviación inicial para el primer nivel DCA.
- Step Scale – multiplicador aplicado a la desviación para cada nuevo nivel DCA.
- DCA Size Multiplier – multiplicador de volumen para cada orden DCA.
- Max DCA Levels – número máximo de entradas de promediado.
- Initial Volume – volumen de la primera orden.
