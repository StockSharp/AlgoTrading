# Estrategia de Canal de Redondeo de MA Exponencial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia redondea una media móvil a un paso de tick fijo y construye un canal basado en ATR a su alrededor. Cuando la vela anterior cierra por encima de la banda superior, la estrategia abre una posición larga. Cuando la vela anterior cierra por debajo de la banda inferior, abre una posición corta. Las señales opuestas cierran las posiciones existentes. El stop loss y el take profit se definen en ticks y se gestionan automáticamente.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El cierre anterior está por encima de la banda superior redondeada.
  - **Corto**: El cierre anterior está por debajo de la banda inferior redondeada.
- **Criterios de salida**:
  - **Largo**: El cierre anterior está por debajo de la banda inferior.
  - **Corto**: El cierre anterior está por encima de la banda superior.
- **Indicadores**:
  - Media Móvil Exponencial.
  - Average True Range para el ancho del canal.
- **Stops**: Sí, stop loss y take profit fijos en ticks.
- **Valores predeterminados**:
  - `MA period` = 12.
  - `ATR period` = 12.
  - `ATR factor` = 1.
  - `MA round` = 500 ticks.
  - `Stop loss` = 1000 ticks.
  - `Take profit` = 2000 ticks.
  - `Timeframe` = 4 horas.

## Filtros

- Categoría: Seguimiento de tendencia
- Dirección: Ambos
- Indicadores: Múltiples
- Stops: Sí
- Complejidad: Moderado
- Marco temporal: Medio plazo
- Estacionalidad: No
- Redes neuronales: No
- Divergencia: No
- Nivel de riesgo: Moderado
