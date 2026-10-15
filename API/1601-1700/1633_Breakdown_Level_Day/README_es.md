# Estrategia de Rompimiento de Nivel Diario
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia coloca órdenes stop pendientes por encima y por debajo del rango intradía a una hora específica del día. Su objetivo es capturar rupturas cuando el precio se mueve más allá del máximo o mínimo de la sesión temprana. Las reglas opcionales de stop loss, take profit, punto de equilibrio y trailing stop gestionan la posición abierta.

## Detalles

- **Entrada**: A la hora `OrderTime` se coloca un buy stop por encima del máximo diario más `Delta` ticks y un sell stop por debajo del mínimo diario menos `Delta` ticks.
- **Salida**: Las órdenes de stop-loss y take-profit se colocan junto con la orden de ruptura. El punto de equilibrio y el trailing stop pueden actualizar el stop protector.
- **Indicadores**: Ninguno.
- **Marco temporal**: Velas de 1 minuto por defecto.
- **Riesgo**: El tamaño de la posición se toma de la propiedad `Volume` de la estrategia.

## Parámetros

- `OrderTime` — hora del día en que se envían las órdenes pendientes.
- `Delta` — distancia desde los límites del rango en ticks.
- `StopLoss` — distancia del stop protector en ticks.
- `TakeProfit` — distancia del objetivo de beneficio en ticks.
- `BreakEven` — mover el stop al precio de entrada después de este beneficio en ticks.
- `Trailing` — distancia del trailing stop en ticks.
- `CandleType` — tipo de vela utilizado para los cálculos.
