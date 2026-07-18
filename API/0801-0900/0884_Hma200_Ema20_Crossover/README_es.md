# Estrategia de Cruce HMA 200 + EMA 20
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia entra en largo cuando el precio está por encima de la Hull Moving Average de 200 períodos
y cruza por encima de la Exponential Moving Average de 20 períodos. Las posiciones cortas se
abren cuando el precio está por debajo de la HMA y cruza por debajo de la EMA. Las posiciones se revierten
ante señales contrarias.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close > HMA` y `Close` cruza por encima de `EMA`.
  - **Corto**: `Close < HMA` y `Close` cruza por debajo de `EMA`.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Revertir ante señal de cruce opuesto.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `HMA Length` = 200
  - `EMA Length` = 20
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: HMA, EMA
  - Stops: Ninguno
  - Complejidad: Simple
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
