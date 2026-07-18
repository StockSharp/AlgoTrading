# Estrategia Premarket Gap MomoTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera una única ruptura larga durante la sesión de premarket cuando la vela actual gana al menos un porcentaje especificado por encima del cierre anterior, imprime una vela alcista con volumen suficiente y el cuerpo de la vela ocupa una gran parte de su rango. El tamaño de la posición se escala según el tamaño del cuerpo.

Tras la entrada, la estrategia mantiene la posición mientras las siguientes velas permanezcan alcistas y su volumen aumente. Una vela roja o un volumen que no aumenta cierra la posición. Solo se permite una operación por día y el trading puede restringirse a la sesión 04:00–09:30.

## Detalles

- **Criterios de entrada**:
  - Ganancia de la vela actual ≥ `MinGainPct` comparada con el cierre anterior.
  - La vela es verde y `Volume` > `MinVolume`.
  - El porcentaje del cuerpo define el tamaño de la posición: ≥90% → 100%, ≥85% → 50%, ≥75% → 25%.
  - Filtro de sesión opcional 04:00–09:30 si `UseSession` está habilitado.
- **Criterios de salida**:
  - Primera vela roja o vela con volumen no creciente después de la entrada.
- **Stops**: No.
- **Valores predeterminados**:
  - `MinGainPct` = 5.
  - `MinVolume` = 15000.
  - `UseSession` = true.
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Intradía
