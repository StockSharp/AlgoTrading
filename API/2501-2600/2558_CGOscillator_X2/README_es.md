# Estrategia CGOscillator X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia CGOscillator X2** es un sistema de seguimiento de tendencia multitemporal que usa el oscilador Center of Gravity para operar retrocesos. La estrategia evalúa la pendiente del oscilador en un marco temporal superior para determinar la tendencia dominante y espera un gancho correctivo en un marco temporal inferior antes de entrar en una operación en la dirección de la tendencia. Se pueden usar distancias opcionales de stop-loss y take-profit expresadas en unidades de precio absoluto para gestionar el riesgo después de que se abra una entrada.

## Lógica de trading

1. **Detección de tendencia (marco temporal superior)**
   - El oscilador Center of Gravity (CG) se calcula en el marco temporal de tendencia usando el `TrendLength` configurado.
   - Si el valor actual de CG está por encima de su señal (valor anterior), la estrategia considera el mercado alcista; si está por debajo de la señal, el mercado se considera bajista.
2. **Generación de señales (marco temporal inferior)**
   - Una segunda instancia del oscilador CG con su propia longitud trabaja en el marco temporal de señal.
   - La estrategia monitorea las dos velas finalizadas más recientes. Un gancho alcista (CG actual >= señal mientras el CG anterior < señal anterior) indica que un retroceso terminó dentro de una tendencia bajante. Un gancho bajista (CG actual <= señal mientras el CG anterior > señal anterior) resalta un retroceso dentro de una tendencia alcista.
3. **Entradas y salidas**
   - Las entradas largas solo están permitidas cuando el marco temporal superior muestra una tendencia alcista y el último swing del marco temporal inferior indica un gancho bajista (retroceso sobrevendido). Los cortos siguen la lógica reflejada para tendencias bajistas.
   - Las posiciones pueden cerrarse cuando la tendencia del marco temporal superior gira o cuando el último gancho va en contra de la posición abierta, dependiendo de los parámetros booleanos.
4. **Controles de riesgo**
   - Se aplican distancias opcionales absolutas de stop-loss y take-profit después de cada entrada a mercado. Cuando el precio cruza esos niveles dentro de la vela actual, la posición se cierra inmediatamente antes de que se procesen nuevas señales.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `TrendCandleType` | Tipo de vela (marco temporal) usado para el oscilador CG de mayor marco temporal. |
| `SignalCandleType` | Tipo de vela usado para el oscilador de señal de menor marco temporal. |
| `TrendLength` | Longitud del oscilador CG en el marco temporal de tendencia. |
| `SignalLength` | Longitud del oscilador CG en el marco temporal de señal. |
| `BuyOpen` | Habilita o deshabilita entradas largas alineadas con la tendencia del marco temporal superior. |
| `SellOpen` | Habilita o deshabilita entradas cortas alineadas con la tendencia del marco temporal superior. |
| `BuyClose` | Cierra posiciones largas cuando la tendencia del marco temporal superior se vuelve bajista. |
| `SellClose` | Cierra posiciones cortas cuando la tendencia del marco temporal superior se vuelve alcista. |
| `BuyCloseSignal` | Cierra posiciones largas cuando el último gancho del marco temporal inferior es bajista. |
| `SellCloseSignal` | Cierra posiciones cortas cuando el último gancho del marco temporal inferior es alcista. |
| `StopLoss` | Distancia de precio absoluta para el stop protector (0 deshabilita el stop). |
| `TakeProfit` | Distancia de precio absoluta para el objetivo de ganancia (0 deshabilita el objetivo). |

## Detalles del indicador

El **CenterOfGravityOscillatorIndicator** personalizado replica el Oscilador CG de MT5:
- El precio mediano `(máximo + mínimo) / 2` se usa como entrada.
- Una suma ponderada de los últimos `Length` medianos forma el valor CG.
- La línea de señal es simplemente el valor CG anterior, proporcionando un desfase de una barra para la detección de ganchos.

## Notas de uso

- Establecer la propiedad `Volume` de la estrategia para controlar el tamaño base de la orden. Las reversiones agregan automáticamente el valor absoluto de la posición actual para que el nuevo posición se abra en la dirección deseada.
- Dado que la estrategia trabaja solo con velas finalizadas, es resistente al ruido intrabarra pero reacciona al cierre de cada vela.
- Los parámetros de stop-loss y take-profit usan unidades de precio absoluto; ajustarlos al tamaño del tick y al perfil de volatilidad del instrumento.
- La estrategia puede adjuntarse a cualquier instrumento compatible con StockSharp una vez configurados los tipos de velas apropiados.
