# Estrategia de Reversión Normalizada CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el Índice de Canal de Materias Primas (CCI) para detectar reversiones después de que el indicador sale de zonas extremas.

## Descripción general

El indicador se calcula sobre velas de 8 horas con un período configurable. Dos niveles de umbral definen las áreas de sobrecompra y sobreventa. Cuando el CCI vuelve dentro de estos límites tras alcanzar un extremo, la estrategia entra en posición en la dirección opuesta, esperando una reversión a la media.

## Reglas de operación

- **Entrada Largo**: Hace dos barras el CCI estaba por encima del nivel alto y la barra anterior cayó por debajo.
- **Entrada Corto**: Hace dos barras el CCI estaba por debajo del nivel bajo y la barra anterior subió por encima.
- **Cierre Largo**: El CCI de la barra anterior estaba por debajo del nivel medio.
- **Cierre Corto**: El CCI de la barra anterior estaba por encima del nivel medio.

## Parámetros

- `CciPeriod` – período de retrospección para el CCI.
- `HighLevel` – umbral superior del CCI considerado sobrecompra.
- `MiddleLevel` – umbral medio utilizado para salir de posiciones.
- `LowLevel` – umbral inferior del CCI considerado sobreventa.
- `CandleType` – serie de velas utilizada para los cálculos (predeterminado 8 horas).

## Notas

La estrategia abre como máximo una posición a la vez y utiliza órdenes de mercado. La gestión de riesgo predeterminada se habilita mediante `StartProtection`.
