# Estrategia PPO Nube
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de momentum opera los cruces entre el Oscilador de Precio Porcentual (PPO) y su línea de señal. Una posición larga se abre cuando el PPO cruza por encima de su línea de señal, mientras que una posición corta se abre en el cruce opuesto. Las posiciones existentes pueden cerrarse opcionalmente en la señal contraria. La estrategia opera en un único marco temporal.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `PPO cruza por encima de la señal`.
  - **Corto**: `PPO cruza por debajo de la señal`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - **Largo**: `PPO cruza por debajo de la señal` (opcional).
  - **Corto**: `PPO cruza por encima de la señal` (opcional).
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Fast Period` = 12.
  - `Slow Period` = 26.
  - `Signal Period` = 9.
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
