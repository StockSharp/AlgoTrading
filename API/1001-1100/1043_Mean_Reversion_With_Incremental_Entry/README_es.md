# Estrategia de Reversión a la Media con Entrada Incremental
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en operaciones cuando el precio se desvía de una media móvil simple en un porcentaje definido. Se colocan órdenes adicionales de forma incremental a medida que el precio se aleja más de la media.

Las posiciones se cierran una vez que el precio regresa a la media móvil.

## Detalles

- **Criterios de entrada:**
  - **Largo:** `Low < SMA` y la diferencia porcentual entre `Low` y `SMA` ≥ `Initial Percent`.
  - **Corto:** `High > SMA` y la diferencia porcentual entre `High` y `SMA` ≥ `Initial Percent`.
- **Entradas incrementales:** Se añaden nuevas órdenes cada `Percent Step` adicional desde la entrada anterior.
- **Criterios de salida:**
  - **Largo:** `Close ≥ SMA`.
  - **Corto:** `Close ≤ SMA`.
- **Indicadores:** SMA.
- **Valores predeterminados:**
  - `MA Length` = 30.
  - `Initial Percent` = 5.
  - `Percent Step` = 1.
