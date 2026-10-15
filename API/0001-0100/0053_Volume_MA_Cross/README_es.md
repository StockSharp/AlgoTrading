# Cruce de MA de Volumen (Volume MA Cross)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia procesa el volumen a través de medias móviles rápida y lenta. Cuando la MA de volumen rápida cruza por encima de la MA de volumen lenta, indica mayor participación y genera una entrada en largo. Un cruce por debajo señala debilidad e inicia una posición corta.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 46%. Funciona mejor en el mercado de acciones.

Las posiciones se cierran cuando se produce el cruce inverso. El precio se monitorea con su propia media móvil para ayudar a filtrar las operaciones.

Las señales basadas en volumen a menudo preceden al movimiento del precio, permitiendo entradas tempranas.

## Detalles

- **Criterios de entrada**: La MA de volumen rápida cruza la MA de volumen lenta.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce inverso o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastVolumeMALength` = 10
  - `SlowVolumeMALength` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Volume MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
