# Estrategia MOC Delta MOO Entry v2 Reverse
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia invierte la lógica clásica de MOC Delta MOO Entry. Mide el delta de volumen de compra-venta en la sesión de la tarde (14:50–14:55) y almacena el delta como porcentaje del volumen del día. A la mañana siguiente a las 08:30 se abre una posición en la dirección opuesta al delta si supera un umbral, filtrada por dos medias móviles. Las posiciones se cierran con take profit y stop loss basados en ticks o a las 14:50.

## Detalles

- **Criterios de entrada**:
  - **Largo**: A las 08:30 cuando el porcentaje de delta guardado está por debajo de `-DeltaThreshold` y el precio de apertura está por encima de SMA15 y SMA30, con SMA15 por encima de SMA30.
  - **Corto**: A las 08:30 cuando el porcentaje de delta guardado está por encima de `DeltaThreshold` y el precio de apertura está por debajo de SMA15 y SMA30, con SMA15 por debajo de SMA30.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Take profit y stop loss en ticks.
  - Cierre de todas las posiciones abiertas a las 14:50.
- **Stops**:
  - `TpTicks` = 20 ticks de take profit.
  - `SlTicks` = 10 ticks de stop loss.
- **Valores predeterminados**:
  - `DeltaThreshold` = 2
  - `TpTicks` = 20
  - `SlTicks` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoría: Volumen
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
