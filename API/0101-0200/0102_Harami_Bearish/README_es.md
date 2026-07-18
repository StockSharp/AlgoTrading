# Estrategia de Harami Bajista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El Harami Bajista es la versión inversa del alcista y aparece después de un movimiento al alza.
Una vela pequeña se forma completamente dentro de la barra alcista anterior, sugiriendo que el impulso ascendente está perdiendo fuerza.

Las pruebas indican un rendimiento anual promedio de aproximadamente 43%. Funciona mejor en el mercado de acciones.

La estrategia vende en corto cuando esa vela interior cierra, apostando por una reversión a medida que los compradores pierden convicción.

Un stop porcentual por encima del máximo del patrón limita el riesgo y la operación sale si el precio rompe a nuevos máximos.

## Detalles

- **Criterios de entrada**: coincidencia de patrón
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basados en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

