# Estrategia de Biblioteca de Herramientas de Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia simple de cruce de SMA con filtro RSI y tiempo de enfriamiento entre entradas.

## Detalles
- **Criterios de entrada**:
  - **Largo**: SMA rápida cruza por encima de la SMA lenta y RSI por debajo de `RsiUpper`
  - **Corto**: SMA rápida cruza por debajo de la SMA lenta y RSI por encima de `RsiLower`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Señal inversa
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `ShortLength` = 10
  - `LongLength` = 30
  - `RsiLength` = 14
  - `CooldownBars` = 3
  - `RsiUpper` = 60
  - `RsiLower` = 40
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA, RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
