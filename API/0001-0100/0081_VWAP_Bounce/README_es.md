# Estrategia de Rebote en VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Precio Promedio Ponderado por Volumen (VWAP) es un popular punto de referencia intradía. Cuando el precio se desvía significativamente del VWAP y luego imprime una vela de regreso hacia él, a menudo sigue un breve movimiento de reversión. Esta estrategia opera esos rebotes.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 130%. Funciona mejor en el mercado de acciones.

Para cada barra se calcula el VWAP actual. Si una vela alcista cierra por debajo del VWAP, el sistema va largo; si una vela bajista cierra por encima del VWAP, va corto. Un porcentaje de stop-loss fijo gestiona el riesgo, y las posiciones se mantienen típicamente solo hasta que se forma una señal opuesta o se alcanza el stop.

Dado que opera en contra de los extremos intradía, el método funciona mejor en mercados de rango que en tendencias fuertes.

## Detalles

- **Criterios de entrada**: Cierre por debajo del VWAP con vela alcista o por encima del VWAP con vela bajista.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o stop-loss.
- **Stops**: Sí, basados en porcentaje.
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: VWAP
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

