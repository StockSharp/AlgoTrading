# Estrategia PROphet
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia PROphet evalúa los rangos de precios de las últimas tres velas completadas para generar señales durante horas de trading especificadas. Una función personalizada combina los rangos con coeficientes definidos por el usuario. Si la función es positiva, la estrategia abre una posición en la dirección correspondiente.

Las operaciones largas usan los coeficientes `X1..X4` y un trailing stop definido por `BuyStopPoints`. Las operaciones cortas usan los coeficientes `Y1..Y4` y `SellStopPoints`. Los stops siguen el precio cuando este se mueve a favor de la posición en más del spread más el doble de la distancia del stop. Las posiciones se cierran después de las 18:00 o cuando el trailing stop es alcanzado.

## Detalles

- **Criterios de entrada**
  - **Largo**: `Qu(X1,X2,X3,X4) > 0` y la hora actual entre las 10 y las 18.
  - **Corto**: `Qu(Y1,Y2,Y3,Y4) > 0` y la hora actual entre las 10 y las 18.
- **Criterios de salida**
  - **Largo**: Hora > 18 o el mejor precio de compra cae por debajo del trailing stop.
  - **Corto**: Hora > 18 o el mejor precio de venta sube por encima del trailing stop.
- **Parámetros**
  - `EnableBuy` – permitir abrir posiciones largas.
  - `EnableSell` – permitir abrir posiciones cortas.
  - `X1, X2, X3, X4` – coeficientes para la función de señal larga.
  - `Y1, Y2, Y3, Y4` – coeficientes para la función de señal corta.
  - `BuyStopPoints` – distancia del trailing stop en puntos para operaciones largas.
  - `SellStopPoints` – distancia del trailing stop en puntos para operaciones cortas.
  - `CandleType` – tipo de vela para los cálculos (por defecto 5 minutos).
- **Filtros**
  - Categoría: Intradía
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Trailing stop
  - Complejidad: Moderado
  - Marco temporal: Corto plazo
