# Estrategia de Trailing Stop NRTR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia sigue las tendencias del mercado usando el indicador **NRTR (Nick R's Trend Reverse)**. El algoritmo calcula un nivel de trailing stop derivado del rango promedio de velas recientes. Cuando el precio rompe el nivel de trailing, la posición se invierte en la dirección del rompimiento. El sistema funciona tanto en el lado largo como corto e incluye protecciones opcionales de stop-loss y take-profit.

La longitud del NRTR define la sensibilidad del trailing stop: un período más corto reacciona más rápido pero puede dar falsas señales, mientras que un período más largo filtra el ruido. Un parámetro adicional de desplazamiento de dígitos ajusta el indicador a instrumentos con diferentes escalas de precio. La estrategia se suscribe a velas del marco temporal elegido y calcula los valores NRTR en cada barra finalizada.

## Detalles

- **Lógica de entrada**:
  - **Largo**: El precio cruza por encima del nivel NRTR después de una tendencia bajista.
  - **Corto**: El precio cruza por debajo del nivel NRTR después de una tendencia alcista.
- **Lógica de salida**:
  - Las posiciones se invierten cuando ocurre un rompimiento opuesto.
- **Stops**: Stop-loss y take-profit opcionales mediante `StartProtection`.
- **Valores predeterminados**:
  - `Length` = 10
  - `DigitsShift` = 0
  - `TakeProfit` = 2000 puntos
  - `StopLoss` = 1000 puntos
  - `CandleType` = velas de 1 hora
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: NRTR, ATR
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Configurable
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
