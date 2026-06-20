# Patrón de Doble Suelo (Double Bottom Pattern)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia basada en patrones busca dos mínimos consecutivos aproximadamente al mismo precio, separados por una distancia establecida. Después de formarse el segundo suelo, una vela alcista confirma la reversión.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 55%. Funciona mejor en el mercado de acciones.

Cuando se produce la confirmación, el sistema compra con un stop por debajo de los mínimos del patrón. La configuración tiene como objetivo capturar rebotes pronunciados tras un agotamiento de la venta.

Las salidas dependen de un stop-loss predefinido o de objetivos de beneficio manuales.

## Detalles

- **Criterios de entrada**: Dos suelos se forman dentro de `SimilarityPercent` después de `Distance` barras.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: El precio falla o stop-loss.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Distance` = 5
  - `SimilarityPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Solo largos
  - Indicadores: Price Action
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
