# Estrategia JS Sistem 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
JS Sistem 2 es un sistema de seguimiento de tendencia originalmente escrito para MetaTrader 5. El port a StockSharp mantiene el bloque de confirmación multi-indicador del asesor experto y opera en velas cerradas del marco temporal seleccionado. Las órdenes tienen un volumen fijo y opcionalmente pueden bloquearse si el saldo de la cartera conectada cae por debajo de un umbral configurable. El riesgo se controla mediante distancias duras de stop-loss y take-profit expresadas en pips junto con un trailing stop adaptativo que sigue las sombras de las velas.

## Indicadores y filtros
- **EMA(55), EMA(89), EMA(144)** – forman un filtro direccional. Las configuraciones largas requieren la EMA rápida por encima de la media y la media por encima de la línea lenta, mientras que la distancia entre las curvas rápida y lenta debe mantenerse por debajo de `MinDifferencePips`.
- **Histograma MACD (OsMA)** – usa longitudes de EMA rápida, lenta y señal idénticas a la versión MQL. Una operación larga requiere que el histograma sea positivo, una operación corta requiere que sea negativo.
- **Índice de Vigor Relativo (RVI)** – calculado con período `RviPeriod` y suavizado por una media móvil simple adicional con `RviSignalLength`. Las operaciones largas necesitan que el RVI esté por encima de su línea de señal y por encima del umbral `RviMax`; las cortas necesitan lo inverso.
- **Envolventes de swing más alto/más bajo** – rastrean el máximo más alto y el mínimo más bajo durante `VolatilityPeriod` velas. Estos valores impulsan la lógica del trailing stop y replican el modo de trailing por sombras del asesor experto original.

## Lógica de trading
1. La estrategia procesa únicamente velas terminadas del `CandleType` configurado.
2. Antes de evaluar entradas actualiza el trailing stop para posiciones existentes usando los últimos extremos de swing y luego verifica si los niveles de stop-loss o take-profit fueron alcanzados durante la vela.
3. Condiciones de entrada larga:
   - El saldo de la cartera está por encima de `MinBalance`.
   - EMA55 > EMA89 > EMA144 y la diferencia entre EMA55 y EMA144 está por debajo de `MinDifferencePips` (convertida a unidades de precio a través del tamaño de pip del instrumento).
   - El histograma MACD (`macdLine`) es mayor que cero.
   - El RVI está por encima de su línea de señal y la línea de señal está en o por encima de `RviMax`.
   - No hay posición larga existente (`Position <= 0`). Cuando existe una posición corta se aplana antes de abrir la larga.
4. Las condiciones de entrada corta reflejan las reglas largas con comparaciones invertidas y usan el umbral `RviMin`.
5. Al entrar, la estrategia almacena el precio de cierre de la vela como referencia, coloca niveles virtuales de stop-loss y take-profit desplazando ese precio por `StopLossPips` y `TakeProfitPips`, y restablece el estado de trailing.

## Gestión de salida y trailing
- **Stop-loss / take-profit duro:** Siempre que el rango de la vela se superponga con el nivel de stop o objetivo almacenado, la estrategia cierra toda la posición inmediatamente.
- **Trailing stop:** Cuando `TrailingEnabled` es verdadero, la estrategia intenta mover el stop en la dirección del beneficio. Para largos, el stop se eleva al mínimo más bajo de las últimas `VolatilityPeriod` velas una vez que ese mínimo está por encima tanto del precio de entrada como del stop anterior en al menos `TrailingIndentPips`. Los cortos siguen la regla simétrica usando el máximo más alto. Esto reproduce el "trailing por sombras" del asesor MQL y evita que los stops se aprieten prematuramente.
- **Protección de saldo:** Si el valor actual de la cartera cae por debajo de `MinBalance`, la estrategia se abstiene de enviar nuevas órdenes pero sigue gestionando las operaciones abiertas y los trailing stops.

## Parámetros
| Parámetro | Descripción | Por defecto |
| --- | --- | --- |
| `MinBalance` | Saldo mínimo de cartera requerido para nuevas entradas. | 100 |
| `Volume` | Volumen de la orden enviado con cada operación. | 1 |
| `StopLossPips` | Distancia del stop-loss medida en pips. Establecer en 0 para deshabilitar. | 35 |
| `TakeProfitPips` | Distancia del take-profit medida en pips. Establecer en 0 para deshabilitar. | 40 |
| `MinDifferencePips` | Máximo diferencial permitido entre la EMA rápida y lenta en pips. | 28 |
| `VolatilityPeriod` | Número de velas usadas para calcular máximos y mínimos de swing para el trailing stop. | 15 |
| `TrailingEnabled` | Habilita o deshabilita la lógica del trailing stop. | true |
| `TrailingIndentPips` | Brecha mínima entre precio, entrada y stop al actualizar el trailing stop. | 1 |
| `MaFastPeriod` | Período para la EMA rápida. | 55 |
| `MaMediumPeriod` | Período para la EMA media. | 89 |
| `MaSlowPeriod` | Período para la EMA lenta. | 144 |
| `OsmaFastPeriod` | Longitud de EMA rápida para el histograma MACD. | 13 |
| `OsmaSlowPeriod` | Longitud de EMA lenta para el histograma MACD. | 55 |
| `OsmaSignalPeriod` | Longitud de suavizado de señal para el histograma MACD. | 21 |
| `RviPeriod` | Período del Índice de Vigor Relativo. | 44 |
| `RviSignalLength` | Longitud de la SMA aplicada al RVI para obtener su línea de señal. | 4 |
| `RviMax` | Límite superior que la señal RVI debe alcanzar antes de que se permitan entradas largas. | 0.04 |
| `RviMin` | Límite inferior que la señal RVI debe alcanzar antes de que se permitan entradas cortas. | -0.04 |
| `CandleType` | Marco temporal de las velas usadas para todos los cálculos. | Velas de 5 minutos |

## Notas de implementación
- La distancia de pip se deriva del paso de precio del instrumento. Los instrumentos cotizados con 3 o 5 decimales usan un pip igual a diez pasos de precio, coincidiendo con la lógica MQL original.
- La gestión de stop y objetivo ocurre dentro del bucle de la estrategia porque StockSharp no envía automáticamente órdenes del lado del servidor en esta plantilla.
- La estrategia llama a `StartProtection()` durante el inicio para que la clase base pueda monitorear desconexiones inesperadas y posiciones pendientes.
