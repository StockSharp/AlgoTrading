# Estrategia de Bollinger Bands Multi-Timeframe
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Aplica Bollinger Bands tanto en un marco temporal principal como en uno superior. Opera cuando el precio perfora las bandas del marco temporal superior y opcionalmente filtra entradas con una media móvil a largo plazo. El objetivo es desvanecerse en los extremos contra la tendencia más amplia.

La estrategia admite posiciones largas y cortas. Se puede habilitar un porcentaje de stop-loss para la gestión del riesgo. El uso de múltiples marcos temporales ayuda a evitar operaciones contra la estructura de mercado dominante.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Cierre por debajo de la banda inferior del marco temporal superior y por encima del filtro MA (si está habilitado).
  - **Corto**: Cierre por encima de la banda superior del marco temporal superior y por debajo del filtro MA (si está habilitado).
- **Criterios de salida**:
  - Largo: El precio cierra por encima de la banda superior del marco temporal actual.
  - Corto: El precio cierra por debajo de la banda inferior del marco temporal actual.
- **Indicadores**:
  - Bollinger Bands en dos marcos temporales (longitud 20, multiplicador 2)
  - Filtro EMA opcional (período 200)
- **Stops**: Stop-loss opcional via StartProtection (basado en %).
- **Valores predeterminados**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `UseMaFilter` = False
  - `MaLength` = 200
  - `SLPercent` = 2
- **Filtros**:
  - Contratendencia con contexto MTF
  - Marco temporal: principal 5m, MTF 60m por defecto
  - Indicadores: Bollinger Bands, EMA
  - Stops: Opcional
  - Complejidad: Moderado
