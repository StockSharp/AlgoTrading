# Estrategia de Flujo de Tendencia Adaptativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Flujo de Tendencia Adaptativa construye un canal basado en volatilidad a partir de EMA rápidas y lentas del precio típico. Cuando el precio cruza los límites del canal, la tendencia interna cambia. Las posiciones largas se abren cuando la tendencia gira hacia arriba y los filtros opcionales de SMA y MACD lo confirman. Las posiciones se cierran cuando la tendencia se invierte hacia abajo.

## Detalles

- **Criterios de entrada**:
  - La tendencia cambia de bajista a alcista y los filtros lo confirman.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - La tendencia cambia de alcista a bajista.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 2
  - `SmoothLength` = 2
  - `Sensitivity` = 2.0
  - `UseSmaFilter` = true
  - `SmaLength` = 4
  - `UseMacdFilter` = true
  - `MacdFastLength` = 2
  - `MacdSlowLength` = 7
  - `MacdSignalLength` = 2
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: EMA, SMA, MACD, Standard Deviation
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
