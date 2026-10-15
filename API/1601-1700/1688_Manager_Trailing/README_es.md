# Estrategia de Gestión con Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre una única posición larga y luego la gestiona mediante varios controles de riesgo:

- **Take profit** y **stop loss** basados en porcentaje.
- **Trailing** de beneficios que se activa tras una ganancia configurable.
- **Cierre parcial** en niveles de beneficio personalizados.

El algoritmo demuestra cómo gestionar una posición existente con StockSharp utilizando únicamente datos de velas.

## Detalles

- **Criterios de entrada**: Compra a mercado en la primera vela completada.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Porcentaje de take profit.
  - Porcentaje de stop loss.
  - Disparador de trailing de beneficios.
  - Porciones de cierre parcial.
- **Stops**: Sí, mediante porcentajes.
- **Filtros**: Ninguno.
