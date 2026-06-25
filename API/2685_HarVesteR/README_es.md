# Estrategia HarVesteR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia HarVesteR combina el momentum del MACD con dos medias móviles simples y un filtro opcional de fuerza de tendencia ADX.
Busca situaciones en las que el precio se adhiere a las medias móviles mientras el MACD ha cruzado recientemente la línea cero, señalando un posible rompimiento de la consolidación.
Los stops se colocan en los máximos o mínimos oscilantes, se toma la mitad de la posición en un múltiplo fijo de recompensa, y el resto se protege con una salida de punto de equilibrio impulsada por la media móvil rápida.

## Detalles

- **Criterios de entrada**:
  - Largo: `MACD > 0 && MACD history contains negative value && Close < SlowSMA && Close + Indentation > FastSMA && Close + Indentation > SlowSMA && ADX ≥ AdxBuyLevel (if enabled)`
  - Corto: `MACD < 0 && MACD history contains positive value && Close > SlowSMA && Close - Indentation < FastSMA && Close - Indentation < SlowSMA && ADX ≥ AdxSellLevel (if enabled)`
- **Stop Loss**: Último mínimo/máximo oscilante en `StopLookback` velas completadas.
- **Salida parcial**: Cierra la mitad de la posición cuando el precio se mueve `HalfCloseRatio` veces la distancia entre entrada y stop, luego mueve el stop al punto de equilibrio.
- **Salida final**:
  - Largo: cierra el resto si el precio cae por debajo de `FastSMA + Indentation` después de que el stop esté en punto de equilibrio.
  - Corto: cierra el resto si el precio sube por encima de `FastSMA + Indentation` después de que el stop esté en punto de equilibrio.
- **Largo/Corto**: Ambas direcciones soportadas.
- **Filtros**: Filtro opcional de fuerza de tendencia ADX; establezca `UseAdxFilter` en `false` para desactivarlo.
- **Gestión de posición**: Revierte la posición compensando el volumen de la señal opuesta más la exposición actual.

## Parámetros

| Nombre | Predeterminado | Descripción |
|--------|----------------|-------------|
| `MacdFast` | 12 | Período EMA rápido para la línea de diferencia del MACD. |
| `MacdSlow` | 24 | Período EMA lento para la línea de diferencia del MACD. |
| `MacdSignal` | 9 | Período EMA de señal para suavizado del MACD. |
| `MacdLookback` | 6 | Número de velas recientemente completadas verificadas para un cambio de signo del MACD. |
| `SmaFastLength` | 50 | Longitud de la media móvil simple rápida. |
| `SmaSlowLength` | 100 | Longitud de la media móvil simple lenta. |
| `MinIndentation` | 10 | Desplazamiento en pips aplicado alrededor de las medias móviles antes de entrar o salir. |
| `StopLookback` | 6 | Retroceso de máximo/mínimo oscilante utilizado para inicializar el nivel de stop inicial. |
| `UseAdxFilter` | false | Habilita el filtro de fuerza ADX para ambas direcciones. |
| `AdxBuyLevel` | 50 | Nivel mínimo de ADX requerido para permitir entradas largas cuando el filtro está habilitado. |
| `AdxSellLevel` | 50 | Nivel mínimo de ADX requerido para permitir entradas cortas cuando el filtro está habilitado. |
| `AdxPeriod` | 14 | Período utilizado para el cálculo del ADX. |
| `HalfCloseRatio` | 2 | Multiplicador aplicado a la distancia entrada-stop antes de tomar ganancias parciales. |
| `Volume` | 1 | Volumen de orden para nuevas entradas (compensando cualquier exposición opuesta). |
| `CandleType` | 1 hour | Marco temporal principal utilizado para construir velas e indicadores. |

## Notas

- `MinIndentation` se convierte en distancia de precio usando el tamaño del tick del instrumento. Los instrumentos cotizados con tres o cinco decimales reciben un ajuste de diez veces para aproximar las unidades de pip.
- Cuando `UseAdxFilter` está desactivado, la estrategia acepta señales en ambas direcciones sin verificar el valor del ADX.
- La toma de ganancias parciales y las salidas de punto de equilibrio se ejecutan en cada vela completada para proteger las posiciones abiertas incluso cuando no se permiten nuevas operaciones.
