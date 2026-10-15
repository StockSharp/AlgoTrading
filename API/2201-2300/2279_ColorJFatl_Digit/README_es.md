# Estrategia ColorJFatl Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza la dirección de la pendiente de una Media Móvil Jurik (JMA) para generar operaciones. La JMA aproxima el indicador "ColorJFatl_Digit" del experto MQL5 original. Se abre una posición larga cuando la JMA se vuelve ascendente, mientras que se abre una posición corta cuando la JMA se vuelve descendente. Las posiciones opuestas se cierran cuando la pendiente se revierte.

El sistema opera en ambas direcciones y no emplea stops duros por defecto. Es adecuado para instrumentos donde los cambios de tendencia pueden capturarse con una media móvil adaptativa suavizada.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La pendiente de la JMA cambia de negativa a positiva.
  - **Corto**: La pendiente de la JMA cambia de positiva a negativa.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: La pendiente de la JMA se vuelve negativa.
  - **Corto**: La pendiente de la JMA se vuelve positiva.
- **Stops**: Ninguno por defecto.
- **Valores predeterminados**:
  - `JMA Length` = 5
  - `Timeframe` = 4 horas
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
