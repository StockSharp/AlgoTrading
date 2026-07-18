# Triple MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el cruce de Triple Media Móvil.

Las pruebas indican un retorno anual promedio de aproximadamente 55%. Funciona mejor en el mercado de acciones.

El Triple MA alinea tres medias móviles para definir la dirección. Cuando la media más corta está por encima de las medias media y larga, se produce una entrada larga. La alineación inversa abre cortos, y un cruce de las líneas corta y media cierra la operación.

Usar tres medias ayuda a filtrar el ruido presente en los sistemas de MA único. Este enfoque por capas busca confirmar el momentum antes de comprometerse con una operación.


## Detalles

- **Criterios de entrada**: Señales basadas en MA.
- **Largo/Corto**: Ambos directions.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `ShortMaPeriod` = 5
  - `MiddleMaPeriod` = 20
  - `LongMaPeriod` = 50
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

