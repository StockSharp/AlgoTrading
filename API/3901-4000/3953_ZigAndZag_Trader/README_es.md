# Estrategia comercial ZigAndZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **ZigAndZag Trader Strategy** es la StockSharp versión del MetaTrader experto *ZigAndZag_trader.mq4*. El sistema dispone de dos detectores de oscilación inspirados en ZigZag:

1. Un **ZigZag** a largo plazo (configurado por `TrendDepth`) rastrea la tendencia principal marcando máximos y mínimos importantes.
2. Un **ZigZag** a corto plazo (configurado por `ExitDepth`) identifica el último pivote dentro de esa tendencia y monitorea el precio ponderado (`(5×Close + 2×Open + High + Low) / 9`).

El robot abre operaciones solo cuando el precio se aleja del último giro en dirección a la tendencia dominante y cierra posiciones cuando el precio ponderado vuelve a atravesar ese giro contra la tendencia. Esto reproduce el comportamiento del experto MetaTrader original que lee los buffers 4 a 6 del indicador personalizado `ZigAndZag`.

## Lógica de trading
- **Detección de tendencias** – cuando el ZigZag a largo plazo confirma un nuevo mínimo, la tendencia se considera *alcista*; un nuevo máximo lo pone *abajo*.
- **Seguimiento del swing**: cada pivote a corto plazo restablece el estado interno y almacena el precio ponderado de ese swing.
- **Condiciones de entrada**
  - La tendencia alcista + último pivote es un mínimo: compre cuando el precio ponderado suba por encima del pivote almacenado en al menos un pip.
  - Tendencia bajista + último pivote es un máximo: vender cuando el precio ponderado cae por debajo del pivote almacenado en al menos un pip.
- **Condición de salida**: si el precio retrocede a través del pivote almacenado mientras la tendencia no está de acuerdo con la oscilación activa, todas las posiciones abiertas se cierran.
- **Limitación de pedidos**: el tamaño total absoluto de la posición está limitado a `MaxOrders × Volume`. Las señales adicionales se ignoran una vez que se alcanza ese límite.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | `1 Minute` | Tipo de vela utilizado para ambas evaluaciones de ZigZag. |
| `Lots` | `0.1` | Tamaño de operación solicitado en lotes. El volumen final se alinea con el paso de volumen del instrumento. |
| `TrendDepth` | `3` | Lookback (en velas) del ZigZag de largo plazo que define la tendencia. |
| `ExitDepth` | `3` | Lookback (en velas) del ZigZag de corto plazo que produce entradas y salidas oscilantes. |
| `MaxOrders` | `1` | Número máximo de órdenes/unidades de posición simultáneas. |
| `StopLossPips` | `0` | Distancia protectora de stop-loss en pips (`0` desactiva el stop). |
| `TakeProfitPips` | `0` | Distancia de obtención de beneficios en pips (`0` desactiva el objetivo). |

## Gestión del riesgo
`StartProtection` se habilita automáticamente. Cuando la distancia de stop-loss o take-profit se establece en un valor mayor que cero, se adjuntan órdenes de protección fijas a cada orden de mercado utilizando la distancia de pip proporcionada y el tamaño del tick del instrumento.

## Visualización
La estrategia dibuja velas y ejecuta operaciones en el área del gráfico predeterminada. No se traza ningún indicador personalizado porque la lógica de entrada y salida utiliza rastreadores ZigZag internos.

## Notas
- La fórmula del precio ponderado es idéntica al indicador MetaTrader y evita el acceso directo al búfer del indicador.
- El umbral de ruptura es igual a un pip del instrumento, lo que refleja el código original que requería que el movimiento superara el diferencial actual.
- El puerto mantiene todos los comentarios y registros en inglés según lo exigen las pautas del proyecto.
