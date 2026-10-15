# Estrategia Fisher Cyber Cycle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica la Transformada de Fisher al indicador Cyber Cycle de Ehlers. Se abre una posición larga cuando la línea Fisher cruza por encima de su línea de disparo, mientras que se abre una posición corta en un cruce descendente. Las posiciones se cierran o revierten en el cruce opuesto.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Fisher > Trigger` && `Fisher anterior <= Trigger anterior`
  - **Corto**: `Fisher < Trigger` && `Fisher anterior >= Trigger anterior`
- **Criterios de salida**:
  - Cruce opuesto de Fisher y Trigger
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `Alpha` = 0.07
  - `Length` = 8
  - `Candle Type` = marco temporal de 8 horas
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: Fisher Transform, Cyber Cycle
  - Stops: Ninguno
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
