# Estrategia ForexLine
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia ForexLine es un sistema de seguimiento de tendencia derivado del indicador MetaTrader "ForexLine". Aplica dos etapas de medias móviles ponderadas al precio para construir líneas rápidas y lentas. Los cruces entre estas líneas doblemente suavizadas se utilizan para determinar las señales de entrada.

La estrategia compra cuando la línea rápida cruza por encima de la línea lenta y vende cuando la línea rápida cruza por debajo de la línea lenta. Cada media móvil utiliza un proceso de suavizado en dos pasos que ayuda a filtrar el ruido del mercado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La WMA doblemente suavizada rápida cruza por encima de la WMA doblemente suavizada lenta.
  - **Corto**: La WMA doblemente suavizada rápida cruza por debajo de la WMA doblemente suavizada lenta.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - El cruce opuesto cierra la posición existente.
- **Stops**: No incluidos; se pueden añadir externamente.
- **Valores predeterminados**:
  - `FastLength1` = 5
  - `FastLength2` = 10
  - `SlowLength1` = 20
  - `SlowLength2` = 20
  - `CandleType` = marco temporal de 8 horas
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Medias móviles ponderadas
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
