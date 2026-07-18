# Estrategia de Ciclo de Tendencia Color Schaff RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de seguimiento de tendencia basado en el oscilador Color Schaff RSI Trend Cycle (STC). La estrategia reacciona a las transiciones de color del indicador STC para entrar y salir de posiciones largas y cortas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Color del indicador hace dos barras > 5 y última barra < 6.
  - **Corto**: Color del indicador hace dos barras < 2 y última barra > 1.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Las posiciones largas se cierran cuando el color del indicador hace dos barras < 2.
  - Las posiciones cortas se cierran cuando el color del indicador hace dos barras > 5.
- **Indicadores**: Color Schaff RSI Trend Cycle.
- **Valores predeterminados**:
  - `Fast RSI` = 23
  - `Slow RSI` = 50
  - `Cycle` = 10
  - `High Level` = 60
  - `Low Level` = -60
- **Marco temporal**: Velas de 4 horas por defecto.
- **Stops**: Ninguno.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
