# Estrategia ExFractals
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

La estrategia ExFractals es un sistema de ruptura que combina niveles fractales al estilo Williams con el filtro de momentum del cuerpo promedio ExVol. El algoritmo monitorea continuamente los máximos y mínimos fractales confirmados más recientes, los promedia en pares y abre operaciones cuando el precio cierra más allá de esos niveles promediados mientras la lectura ExVol confirma la dirección del movimiento.

## Lógica de Trading

1. **Detección de fractales**
   - Las velas se procesan una vez que cierran.
   - Los fractales ascendentes (bajistas) y descendentes (alcistas) se detectan una vez que la vela central en una ventana de cinco velas es un extremo estricto comparado con sus vecinas.
   - La estrategia almacena los dos últimos fractales confirmados por lado junto con sus marcas de tiempo.
   - Cada lado produce un nivel accionable igual al promedio de los últimos dos precios de fractal. Los marcas de tiempo duplicadas se ignoran para evitar usar el mismo fractal dos veces.
2. **Filtro ExVol**
   - El valor ExVol es igual al promedio simple del cuerpo de la vela (cierre menos apertura) expresado en pasos de precio durante el período de lookback seleccionado.
   - Un ExVol negativo indica velas alcistas persistentes (cierre positivo respecto a apertura), y un ExVol positivo indica velas bajistas persistentes.
3. **Condiciones de entrada**
   - **Largo:** el último cierre está por encima del nivel fractal superior promediado y ExVol es negativo. Cualquier posición corta activa se cierra y se abre una nueva posición larga.
   - **Corto:** el último cierre está por debajo del nivel fractal inferior promediado y ExVol es positivo. Cualquier posición larga activa se cierra y se abre una nueva posición corta.
4. **Reglas de riesgo y salida**
   - Los objetivos fijos de stop-loss y take-profit se colocan a distancias de pips configurables desde el precio de entrada.
   - Los trailing stops opcionales se mueven solo después de que la operación gane al menos `trailing stop + trailing step` pips. El stop se sube/baja para mantener una distancia de trailing constante mientras respeta el paso mínimo de trailing.
   - Si el precio toca el stop-loss o take-profit, la posición completa se cierra.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ------ | ----------- | -------------- |
| `Candle Type` | Tipo/marco temporal de datos de vela usado por la estrategia. | Marco temporal de 1 hora |
| `ExVol Period` | Número de velas cerradas usadas para promediar el cuerpo de la vela (ExVol). | 15 |
| `Stop Loss` | Distancia de stop-loss en pips desde el precio de entrada. Establecer en `0` para deshabilitar. | 40 |
| `Take Profit` | Distancia de take-profit en pips desde el precio de entrada. Establecer en `0` para deshabilitar. | 100 |
| `Trailing Stop` | Distancia de trailing stop en pips. Establecer en `0` para deshabilitar el trailing. | 30 |
| `Trailing Step` | Movimiento de precio adicional (en pips) requerido antes de mover el trailing stop. Debe ser positivo cuando el trailing está habilitado. | 5 |
| `Volume` | Volumen de orden predeterminado heredado de la clase base `Strategy`. | 1 |

## Notas Adicionales

- La lógica de trailing refleja la implementación MetaTrader: el stop no se ajusta hasta que la posición está al menos `TrailingStop + TrailingStep` pips en ganancia.
- Los cálculos ExVol dependen del `PriceStep` del instrumento; si el paso no está disponible se usa un valor predeterminado de 0.0001.
- La estrategia emite órdenes de mercado mediante `BuyMarket` y `SellMarket`, revirtiendo automáticamente cualquier posición existente antes de abrir una nueva.
- Asegurarse de que el feed de datos proporcione suficientes velas históricas para formar los pares iniciales de fractales (al menos cinco velas cerradas).
