# Estrategia de Ruptura del Rango Lateral
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia detecta fases de mercado tranquilas donde tres medias móviles convergen dentro de una banda estrecha. Cuando el precio finalmente rompe por encima o por debajo de este rango, la estrategia entra en la dirección de la ruptura y busca capturar la tendencia emergente.

El sistema observa la diferencia entre las SMA Rápida, Media y Lenta. Si la diferencia máxima entre estas medias permanece por debajo del umbral configurado durante un número específico de barras, el mercado se considera "lateral". El máximo más alto y el mínimo más bajo de ese período definen los niveles de ruptura.

Las operaciones se abren cuando el precio cierra más allá de estos extremos. Las posiciones se protegen con condiciones inversas: si el precio regresa al rango o alcanza un múltiplo del ancho del rango en beneficio, la posición se cierra.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Tras un rango de `RangeLength` barras donde la diferencia de SMA esté por debajo de `ShakeThreshold`, entrar cuando el precio cierre por encima del máximo más alto del rango.
  - **Corto**: Bajo las mismas condiciones de rango, entrar cuando el precio cierre por debajo del mínimo más bajo del rango.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - **Largo**: Cerrar si el precio regresa por debajo del mínimo del rango o el beneficio supera `4 * (máximo del rango - mínimo del rango)`.
  - **Corto**: Cerrar si el precio regresa por encima del máximo del rango o el beneficio supera `4 * (máximo del rango - mínimo del rango)`.
- **Stops**: Salidas implícitas basadas en los límites del rango y el múltiplo de beneficio.
- **Valores predeterminados**:
  - `FastSma` = 38
  - `MidSma` = 140
  - `SlowSma` = 210
  - `ShakeThreshold` = 250
  - `RangeLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: SMA, Highest, Lowest
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
