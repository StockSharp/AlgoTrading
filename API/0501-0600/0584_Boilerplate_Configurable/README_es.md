# Estrategia Boilerplate Configurable
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Boilerplate Configurable puede cambiar entre dos modos: un cruce de medias móviles simples o un rompimiento por compresión de Bandas de Bollinger. Incluye filtros de día de operación y sesión, un rango de fechas, una ventana de noticias y gestión de riesgos mediante ATR o ratio riesgo/beneficio estático.

## Detalles

- **Criterios de entrada**:
  - En modo `SmaCross`, ir largo cuando la SMA rápida cruza por encima de la SMA lenta y corto en el cruce opuesto.
  - En modo `Squeeze`, entrar cuando el precio rompe la banda de Bollinger exterior mientras permanece dentro de la banda más estrecha.
- **Largo/Corto**: Configurable para largo, corto o ambos con inversión opcional.
- **Criterios de salida**:
  - Stop loss y take profit basados en ATR o porcentajes estáticos.
  - El período de salida diario y la ventana de noticias cierran todas las posiciones.
- **Stops**: Stop loss y take profit por operación con protección de drawdown.
- **Valores predeterminados**:
  - `Length` = 20
  - `WideMultiplier` = 1.5
  - `NarrowMultiplier` = 2
  - `MaxLossPerc` = 0.02
  - `AtrMultiplier` = 1.5
  - `StaticRr` = 2
  - `NewsWindow` = 5
  - `MaxDrawdown` = 0.1
- **Filtros**:
  - Categoría: Modular
  - Dirección: Largo y Corto
  - Indicadores: SMA, Bollinger Bands, ATR
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Cualquiera
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
