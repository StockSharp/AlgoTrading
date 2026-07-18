# Estrategia Contraria a la Tendencia Bollinger RSI SOL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema contratendencia para SOL que compra cuando el precio cruza por encima de la banda inferior de Bollinger con RSI bajo, y vende cuando el precio cruza por debajo de la banda superior con RSI alto. Solo en días de semana.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio cruza hacia arriba la banda inferior y `RSI` < `Long RSI` en días de semana.
  - **Corto**: El precio cruza hacia abajo la banda superior y `RSI` > `Short RSI` en días de semana.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Largo: el precio cruza hacia arriba la banda superior o stop loss bajo los mínimos recientes.
  - Corto: el precio cruza hacia arriba la banda media o alcanza el objetivo de beneficio.
- **Stops**: Stop largo por debajo de los mínimos recientes.
- **Valores predeterminados**:
  - `Bollinger Period` = 20
  - `Bollinger Width` = 2
  - `RSI Length` = 14
  - `Long RSI` = 25
  - `Short RSI` = 79
  - `Short Profit %` = 3.5
- **Filtros**:
  - Categoría: Mean Reversion
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: Sí (días de semana)
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
