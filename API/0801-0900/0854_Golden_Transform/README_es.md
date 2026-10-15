# Estrategia de Transformación Dorada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el indicador Rate of Change con un TRIX triple basado en Hull, un filtro de Hull MA y un Fisher Transform suavizado. Las operaciones largas se abren cuando ROC cruza por encima de TRIX mientras TRIX está por debajo de cero y el precio de apertura está por encima de Hull MA. Las operaciones cortas ocurren en la señal opuesta. Las posiciones se cierran en cruces opuestos o cuando el Fisher suavizado supera los umbrales y se revierte.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `ROC crosses above TRIX` && `TRIX < 0` && `Open > Hull MA`
  - **Corto**: `ROC crosses below TRIX` && `TRIX > 0` && `Open < Hull MA`
- **Largo/Corto**: Largo y Corto
- **Criterios de salida**:
  - Largo: `ROC crosses below TRIX` O (`Fisher HMA > 1.5` && `Fisher HMA crosses below previous Fisher`)
  - Corto: `ROC crosses above TRIX` O (`Fisher HMA < -1.5` && `Fisher HMA crosses above previous Fisher`)
- **Stops**: No
- **Valores predeterminados**:
  - `ROC Length` = 50
  - `Hull TRIX Length` = 90
  - `Hull Entry Length` = 65
  - `Fisher Length` = 50
  - `Fisher Smooth Length` = 5
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ROC, Hull MA, Fisher Transform
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
