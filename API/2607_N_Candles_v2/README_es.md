# N Candles v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia busca un número configurable de velas consecutivas que cierran en la misma dirección. Una vez que se alcanza la longitud de la racha, abre una posición de mercado en la dirección del impulso detectado. La implementación refleja el asesor experto original de MetaTrader 5 "N- candles v2" y mantiene la lógica centrada en velas cerradas para evitar señales prematuras.

## Lógica de la estrategia
1. Suscribirse a la serie de velas seleccionada y esperar barras completamente cerradas.
2. Categorizar cada vela como alcista, bajista o neutral (doji). Las velas doji reinician la racha.
3. Mantener un contador acumulado de velas consecutivas con dirección idéntica.
4. Cuando el contador alcanza el umbral `CandlesCount`, enviar una orden de mercado en la misma dirección. El tamaño de la orden combina el `LotSize` solicitado con cualquier exposición contraria para que la posición neta final tenga el signo y la cantidad deseados.
5. Almacenar el precio de entrada e inicializar los niveles de protección usando las distancias configuradas de stop-loss y take-profit.
6. En cada nueva vela, actualizar el trailing stop (si está habilitado) y salir de posiciones cuando el precio toca el stop-loss, trailing stop o take-profit.

## Gestión de posición
- El stop-loss y take-profit iniciales se miden en pasos de precio (`Security.PriceStep`). Una distancia de cero desactiva el nivel correspondiente.
- El trailing stop es opcional. Cuando está habilitado, el stop se ajusta en `TrailingStopPips` una vez que el precio se mueve favorablemente al menos `TrailingStepPips` más allá de la última ubicación del stop.
- Cerrar una posición elimina todos los niveles en caché para que se requiera una nueva racha para la siguiente entrada.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandlesCount` | Número de velas consecutivas que deben cerrar en la misma dirección antes de operar. | 3 |
| `LotSize` | Tamaño de posición utilizado para cada entrada. La exposición contraria se cierra automáticamente. | 1 |
| `TakeProfitPips` | Distancia take-profit en pasos de precio desde el precio de entrada. | 50 |
| `StopLossPips` | Distancia stop-loss en pasos de precio desde el precio de entrada. | 50 |
| `TrailingStopPips` | Distancia del trailing stop en pasos de precio. Establecer en 0 para desactivar el trailing. | 10 |
| `TrailingStepPips` | Distancia adicional que debe recorrer el precio antes de ajustar el trailing stop. | 4 |
| `CandleType` | Marco temporal de velas usado para los cálculos de señal. | Velas de 1 hora |

## Notas
- La estrategia funciona con cualquier instrumento que proporcione un `PriceStep` válido. Si el instrumento reporta cero, se usa `1` como respaldo, igualando el comportamiento del script original.
- Las señales se generan solo en velas completadas, lo que mantiene un comportamiento consistente entre backtesting y entornos de trading en vivo.
