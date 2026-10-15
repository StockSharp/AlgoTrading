# Estrategia RSI + 1200
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia RSI + 1200** busca capturar reversiones de tendencia confirmadas por
fuerza relativa y un filtro de tendencia de marco temporal superior. Combina un Relative
Strength Index clásico de 14 períodos con una Media Móvil Exponencial calculada
sobre una serie de 120 minutos en múltiples marcos temporales ("1200" hace referencia al marco
temporal superior en el concepto original). Las señales de trading solo se toman cuando
el momentum y el filtro de tendencia se alinean.

Las pruebas retroactivas en pares de criptomonedas líquidas muestran que el método funciona mejor
en mercados direccionales sostenidos. Los períodos volátiles o de rango pueden producir señales
falsas, por lo que la estrategia incluye un pequeño margen de precio alrededor de la EMA y un
stop‑loss basado en porcentaje para ayudar a gestionar el riesgo.

Se abre una operación larga cuando RSI cruza hacia arriba desde territorio sobrevendido y
el precio está dentro del uno por ciento por encima de la EMA del marco temporal superior. La
configuración corta es la condición especular. Las posiciones se cierran cuando RSI alcanza el
extremo opuesto, señalando el agotamiento del movimiento. Un stop protector también se coloca a
`stopLossPercent` por ciento desde el precio de entrada.

## Detalles

- **Condiciones de entrada**
  - **Largo**: RSI cruza por encima de `rsiOversold` y el cierre es <= 1% por encima de la EMA.
  - **Corto**: RSI cruza por debajo de `rsiOverbought` y el cierre es >= 1% por debajo de la EMA.
- **Condiciones de salida**
  - **Largo**: RSI sube por encima de `rsiOverbought`.
  - **Corto**: RSI cae por debajo de `rsiOversold`.
- **Stops**: Stop‑loss porcentual opcional via `stopLossPercent`.
- **Parámetros predeterminados**
  - `rsiLength` = 14
  - `rsiOverbought` = 72
  - `rsiOversold` = 28
  - `emaLength` = 150
  - `mtfTimeframe` = 120 minutos
  - `stopLossPercent` = 0.10 (10%)
- **Filtros**
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: RSI, EMA
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Intradía / multi‑marco temporal
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
