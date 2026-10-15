# Estrategia QQQ v2 ESL easy-peasy-x
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera QQQ usando el cruce del precio con una media móvil principal y filtros de tendencia. Compra cuando el precio de cierre cruza por encima de la MA principal mientras la MA sube y el precio está por encima de la MA de tendencia de largo plazo. Vende en corto cuando el cierre cruza por debajo de la MA principal mientras la MA baja y el precio está por debajo de la MA de tendencia de corto plazo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El cierre cruza por encima de la MA principal, la pendiente de la MA sube, precio por encima de la MA de tendencia larga.
  - **Corto**: El cierre cruza por debajo de la MA principal, la pendiente de la MA baja, precio por debajo de la MA de tendencia corta.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `Main MA Length` = 200
  - `Trend Long Length` = 100
  - `Trend Short Length` = 50
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Medias móviles
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
