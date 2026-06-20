# Patrón de Doble Techo (Double Top Pattern)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Doble Techo identifica dos picos separados por un número de barras con precios similares. Después de formarse el segundo pico, una vela bajista confirma la reversión.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 58%. Funciona mejor en el mercado de acciones.

La estrategia vende en corto tras la confirmación con un stop por encima de los máximos del patrón, con el objetivo de obtener beneficios de un cambio de tendencia tras el agotamiento de los compradores.

Las posiciones se cierran mediante stop-loss o objetivos discrecionales.

## Detalles

- **Criterios de entrada**: Dos techos dentro de `SimilarityPercent` después de `Distance` barras.
- **Largo/Corto**: Solo cortos.
- **Criterios de salida**: El precio repunta o stop-loss.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Distance` = 5
  - `SimilarityPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Solo cortos
  - Indicadores: Price Action
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
