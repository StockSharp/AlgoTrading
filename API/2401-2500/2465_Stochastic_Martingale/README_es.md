# Estrategia Stochastic Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina una entrada clásica con el oscilador Stochastic con un promediado al estilo martingala.
Abre una posición cuando la línea %K cruza la línea %D y el oscilador está por encima/debajo de zonas configurables.
Si el precio se mueve en contra de la posición un paso definido, la estrategia incrementa el volumen por un multiplicador.
Las posiciones se cierran cuando la ganancia acumulada alcanza un número definido de puntos.

## Detalles
- **Criterios de entrada**
  - Largo: %K > %D y %D > ZoneBuy
  - Corto: %K < %D y %D < ZoneSell
- **Promediado**
  - Se colocan órdenes adicionales cada `Step` puntos (o `Step * número de órdenes` en el modo 1).
  - El volumen de cada nueva orden se multiplica por `Mult`.
- **Criterios de salida**
  - Largo: precio ≥ último precio de compra + `ProfitFactor * número de órdenes` puntos.
  - Corto: precio ≤ último precio de venta − `ProfitFactor * número de órdenes` puntos.
- **Parámetros** incluyen tamaño del paso, modo del paso, factor de ganancia, multiplicador, volúmenes iniciales y períodos del Stochastic.
- **Filtros**
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Stochastic
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
