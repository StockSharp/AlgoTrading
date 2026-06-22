# Estrategia RobotPower M5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina los indicadores Bulls Power y Bears Power en un gráfico de 5 minutos.
Abre posiciones cuando el impulso combinado de toros y osos cruza cero y gestiona las salidas con objetivos fijos y un trailing stop.

## Cómo funciona
- **Indicadores**: Bulls Power y Bears Power con un período compartido `BullBearPeriod`.
- **Marco temporal**: velas de 5 minutos por defecto (`CandleType`).

### Reglas de entrada
- **Entrada larga**: Cuando `BullsPower + BearsPower > 0` y no hay posición abierta, comprar a mercado.
- **Entrada corta**: Cuando `BullsPower + BearsPower < 0` y no hay posición abierta, vender a mercado.

### Reglas de salida
- **Take Profit**: Cerrar la posición cuando el precio se mueve `TakeProfit` unidades en la dirección de la operación.
- **Stop Loss**: Cerrar la posición si el precio se mueve contra la posición `StopLoss` unidades.
- **Trailing Stop**: Después de la entrada, el stop loss sigue al precio por `TrailingStep` una vez que el precio avanza más del doble de esa distancia.

### Parámetros
- `BullBearPeriod` – período para los cálculos de Bulls Power y Bears Power.
- `TrailingStep` – tamaño del paso al ajustar el trailing stop.
- `TakeProfit` – distancia desde la entrada hasta el nivel de take profit.
- `StopLoss` – distancia desde la entrada hasta el nivel de stop loss.
- `CandleType` – marco temporal de las velas para el cálculo de señales.

### Tamaño de posición
Utiliza la propiedad `Volume` de la estrategia para el tamaño de la orden.

## Notas
Diseñada con fines educativos y sirve como ejemplo de conversión de una estrategia MQL a la API de StockSharp.
