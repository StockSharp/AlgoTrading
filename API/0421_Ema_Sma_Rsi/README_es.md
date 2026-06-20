# Estrategia de Cruce EMA/SMA + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia sigue tres medias móviles exponenciales (rápida, media y lenta) junto
con un filtro RSI para participar en tendencias emergentes. Se activa una operación
cuando la media rápida cruza la media en la dirección de la media lenta predominante,
indicando que el impulso está acelerando. Solo se consideran las velas que cierran en
la dirección del cruce para evitar señales falsas.

Una salida protectora puede opcionalmente cerrar posiciones después de un número
definido por el usuario de barras si permanecen rentables. El RSI actúa como guardia
de sobrecompra/sobreventa para salir cuando el impulso se estira demasiado.

Las pruebas retrospectivas muestran que la técnica funciona mejor en pares cripto
líquidos durante fases de tendencia donde las medias móviles ofrecen una separación
clara.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `EMA_fast > EMA_medium` y `EMA_fast(t-1) <= EMA_medium(t-1)` y `Close > EMA_slow` y `Close > Open`
  - **Corto**: `EMA_fast < EMA_medium` y `EMA_fast(t-1) >= EMA_medium(t-1)` y `Close < EMA_slow` y `Close < Open`
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: `RSI > 70` o `X barras con ganancias y Close > entry`
  - **Corto**: `RSI < 30` o `X barras con ganancias y Close < entry`
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `EMA_fast` = 10
  - `EMA_medium` = 20
  - `EMA_slow` = 100
  - `RSI_length` = 14
  - `X bars` = 24
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, RSI
  - Stops: Opcional basado en tiempo
  - Complejidad: Medio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
