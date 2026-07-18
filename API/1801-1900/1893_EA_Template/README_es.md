# Estrategia EA Template
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia se origina en una plantilla de EA de MetaTrader. Analiza la vela finalizada anterior y abre una posición en la dirección del cuerpo de la vela. Una vela alcista activa una operación larga, mientras que una vela bajista activa una corta. El modo de reversión invierte la interpretación de la vela para que la estrategia opere contra el color de la barra.

La estrategia admite tamaño de posición fijo o cálculo basado en el capital. Los niveles de stop-loss y take-profit se establecen en puntos desde el precio de entrada. La operación se omite cuando el spread supera el umbral permitido.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el cierre de la vela anterior > apertura y `ReverseTrade` deshabilitado.
  - **Corto**: el cierre de la vela anterior < apertura y `ReverseTrade` deshabilitado.
  - Cuando `ReverseTrade` está habilitado, las señales se invierten.
  - El spread debe estar por debajo de `SpreadLimit` puntos.
- **Criterios de salida**:
  - Color de vela opuesto o activación de stop-loss/take-profit.
- **Tamaño de posición**:
  - Tamaño fijo `Lots` o tamaño basado en capital usando `RiskPercent` cuando `UseMoneyManagement` es true.
- **Stops**:
  - `StopLoss` y `TakeProfit` en puntos relativos al precio de entrada.
- **Largo/Corto**: Ambas direcciones.
- **Indicadores**: Ninguno.
- **Nivel de riesgo**: Medio.

Los parámetros permiten ajustar el tipo de vela, el modo de reversión, las reglas de gestión del dinero y los límites de riesgo.
