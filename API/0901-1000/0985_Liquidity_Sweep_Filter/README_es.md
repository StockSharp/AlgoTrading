# Estrategia de Filtro de Barrida de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de seguimiento de tendencia usa bandas de Bollinger para detectar la dirección del mercado y monitorea el volumen para posibles barridas de liquidez. Se abre una posición cuando la tendencia cambia a alcista o bajista según el modo de operación seleccionado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La tendencia se vuelve alcista y el modo permite operaciones largas.
  - **Corto**: La tendencia se vuelve bajista y el modo permite operaciones cortas.
- **Largo/Corto**: Configurable mediante el modo de operación.
- **Criterios de salida**:
  - **Largo**: La tendencia se vuelve bajista o el modo prohíbe largos.
  - **Corto**: La tendencia se vuelve alcista o el modo prohíbe cortos.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 12.
  - `Multiplier` = 2.0.
  - `Major Sweep Threshold` = 50.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

