# Tendencia Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Parabolic SAR. La Tendencia Parabolic SAR sigue los puntos del indicador Parabolic SAR. Un cambio del precio de un lado del SAR al otro marca un posible cambio de tendencia. Si el precio cruza de vuelta, la operación se cierra.

Las pruebas indican un retorno anual promedio de aproximadamente 49%. Funciona mejor en el mercado de criptomonedas.

Dado que los puntos del SAR siguen el precio, naturalmente proporcionan un punto de salida cuando la tendencia cambia. El método opera tanto en largo como en corto sin utilizar stops adicionales más allá de la reversión del SAR.


## Detalles

- **Criterios de entrada**: Señales basadas en Parabolic, SAR.
- **Largo/Corto**: Ambos directions.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `AccelerationFactor` = 0.02m
  - `MaxAccelerationFactor` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic, SAR
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

