# Estrategia de Operador de Divergencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compara dos medias móviles simples (SMA) y opera basándose en la divergencia entre ellas.

Utiliza la diferencia entre la SMA rápida y la SMA lenta de la vela anterior como medida de divergencia. Si esta divergencia es positiva pero dentro de un rango especificado, la estrategia abre una posición larga. Si la divergencia es negativa y dentro del rango espejado, abre una posición corta. El riesgo se gestiona mediante niveles opcionales de stop-loss y take-profit.

## Detalles

- **Criterios de entrada**:
  - **Largo**: SMA rápida anterior - SMA lenta anterior >= `DvBuySell` y <= `DvStayOut`.
  - **Corto**: SMA rápida anterior - SMA lenta anterior <= `-DvBuySell` y >= `-DvStayOut`.
- **Criterios de salida**: Las posiciones se cierran mediante stop-loss o take-profit si están configurados.
- **Stops**: Soportados a través de `StartProtection` con desplazamientos de precio absolutos.
- **Valores predeterminados**:
  - `FastPeriod` = 7
  - `SlowPeriod` = 88
  - `DvBuySell` = 0.0011
  - `DvStayOut` = 0.0079
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Opcional
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
