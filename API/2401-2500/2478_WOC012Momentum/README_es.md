# Estrategia de Momentum WOC 0.1.2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una versión StockSharp de alto nivel del Expert Advisor de MetaTrader "WOC.0.1.2". Escucha actualizaciones de mejor bid/ask de Nivel 1 y busca rachas de precio rápidas en el lado del ask. Cuando el precio del ask imprime un número configurable de ticks consecutivamente más altos o más bajos dentro de una ventana de tiempo limitada, la estrategia abre una posición de mercado en la dirección de la ruptura. Solo puede haber una posición abierta en cualquier momento, lo que refleja el comportamiento de posición única del código original.

## Datos y ejecución
- **Datos de mercado**: Mejor bid y mejor ask de Nivel 1. El algoritmo no requiere velas ni indicadores.
- **Ejecución**: Órdenes de mercado. Las salidas de protección se emulan dentro de la estrategia comprobando las actualizaciones de bid/ask.

## Lógica de señal
1. Rastrear el último precio del ask y medir cuántos nuevos máximos consecutivos (racha alcista) o nuevos mínimos (racha bajista) han sido impresos.
2. Cuando una racha alcista o bajista alcanza `SequenceLength`, comprobar que la duración de la racha es menor o igual a `SequenceTimeoutSeconds` segundos.
3. Si la racha bajista es más larga que la alcista, enviar una orden de venta; de lo contrario, enviar una orden de compra. La verificación reproduce la lógica original de MetaTrader donde la racha con el contador más alto define la dirección.
4. Restablecer todos los contadores de rachas después de cada intento de entrada para asegurarse de que la siguiente señal comience desde cero.

## Gestión de posición
- **Stop inicial**: Después de una entrada, la estrategia registra inmediatamente un precio de stop-loss que está `StopLossTicks` pasos de precio alejado del bid actual (para largos) o del ask (para cortos).
- **Stop móvil**: Cuando el precio se mueve a favor del trade más de `TrailingStopTicks` pasos de precio, el stop se ajusta a `TrailingStopTicks` detrás del último bid/ask, siempre que el stop permanezca al menos el doble de la distancia de trailing alejado del precio actual. Esto reproduce la condición de trailing de dos pasos del Expert MQL.
- **Ejecución de salida**: Cuando el bid/ask rastreado cruza el nivel de stop almacenado, la posición se cierra mediante una orden de mercado. Después de la salida, el estado interno se restablece para aceptar nuevas rachas.

## Gestión de volumen
Se admiten dos modos de dimensionamiento de posición:
- **Lote fijo**: Usar el parámetro `LotSize` como volumen de orden absoluto.
- **Lotes automáticos**: Habilitar `UseAutoLotSizing` para mapear el saldo de la cuenta a niveles de volumen. El saldo se toma de `Portfolio.CurrentValue` y recurre a `Portfolio.BeginValue` si el valor actual no está disponible.

| Saldo (mayor que) | Volumen |
| ------------------- | ------- |
| 0 (predeterminado)  | `LotSize`
| 200                 | 0.04
| 300                 | 0.05
| 400                 | 0.06
| 500                 | 0.07
| 600                 | 0.08
| 700                 | 0.09
| 800                 | 0.10
| 900                 | 0.20
| 1 000               | 0.30
| 2 000               | 0.40
| 3 000               | 0.50
| 4 000               | 0.60
| 5 000               | 0.70
| 6 000               | 0.80
| 7 000               | 0.90
| 8 000               | 1.00
| 9 000               | 2.00
| 10 000              | 3.00
| 11 000              | 4.00
| 12 000              | 5.00
| 13 000              | 6.00
| 14 000              | 7.00
| 15 000              | 8.00
| 20 000              | 9.00
| 30 000              | 10.00
| 40 000              | 11.00
| 50 000              | 12.00
| 60 000              | 13.00
| 70 000              | 14.00
| 80 000              | 15.00
| 90 000              | 16.00
| 100 000             | 17.00
| 110 000             | 18.00
| 120 000             | 19.00
| 130 000             | 20.00

## Parámetros
- `StopLossTicks` – distancia del stop-loss medida en pasos de precio.
- `TrailingStopTicks` – distancia de trailing medida en pasos de precio (puede ser cero para deshabilitar el trailing).
- `SequenceLength` – número de movimientos consecutivos del ask requeridos antes de entrar en un trade.
- `SequenceTimeoutSeconds` – duración máxima de la racha en segundos.
- `LotSize` – tamaño de orden fijo usado cuando el dimensionamiento automático está deshabilitado.
- `UseAutoLotSizing` – habilita la tabla de volumen basada en saldo mostrada arriba.

## Notas de uso
- Funciona mejor en instrumentos rápidos donde el mejor ask se actualiza con frecuencia; considere probar con feeds de datos a nivel de tick.
- La estrategia requiere cuentas de cobertura porque nunca mantiene posiciones opuestas simultáneamente.
- Asegúrese de que `Security.PriceStep` esté configurado; de lo contrario, los cálculos de stop-loss y trailing recurren a una distancia de 1 unidad monetaria por tick.
- Solo se admite una posición abierta a la vez, reflejando el comportamiento MQL original.
