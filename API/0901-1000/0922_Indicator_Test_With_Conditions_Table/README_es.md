# Estrategia de Prueba de Indicadores con Tabla de Condiciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compara el último precio de cierre con niveles definidos por el usuario y ejecuta órdenes de mercado cuando se cumplen las condiciones. Cada lado (largo y corto) tiene reglas de entrada y salida separadas controladas por parámetros.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La condición larga habilitada es verdadera.
  - **Corto**: La condición corta habilitada es verdadera.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: La condición habilitada de cierre largo es verdadera.
  - **Corto**: La condición habilitada de cierre corto es verdadera.
- **Stops**: No.
- **Valores predeterminados**:
  - `LongOperator` = `>`
  - `CloseLongOperator` = `<`
  - `ShortOperator` = `<`
  - `CloseShortOperator` = `>`
- **Filtros**:
  - Categoría: Otro
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
