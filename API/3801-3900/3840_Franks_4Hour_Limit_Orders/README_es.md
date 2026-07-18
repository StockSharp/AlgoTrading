# Estrategia de órdenes con límite de 4 horas de Franks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de órdenes con límite de 4 horas de Franks** traslada el MetaTrader 4 asesor experto de `MQL/7684/Franks_4hour_limit_orders.mq4` al API de alto nivel de StockSharp. El EA original combina las ideas de Triple Pantalla de Alexander Elder: evalúa el impulso en un gráfico de cuatro horas utilizando el histograma MACD (OsMA) junto con el índice de fuerza, y luego coloca órdenes límite contrarias alrededor de los extremos de las velas anteriores. La implementación StockSharp mantiene esta lógica de indicadores múltiples mientras sigue las pautas del repositorio (pestañas, API de alto nivel, sin colecciones personalizadas) y agrega extensos comentarios en línea en inglés para mayor claridad.

## Lógica de trading
1. **Fuente de datos**: la estrategia se suscribe a un tipo de vela configurable cuyo valor predeterminado son velas de cuatro horas. Todos los cálculos se realizan únicamente en velas completadas para coincidir con el comportamiento del experto en MT4.
2. **Indicadores** – Se utilizan dos indicadores gestionados:
   - `MovingAverageConvergenceDivergenceSignal(12, 26, 9)` proporciona tanto la línea MACD como la línea de señal. Su diferencia recrea el histograma OsMA utilizado en EA.
   - `ForceIndex(24)` mide la fuerza de la vela anterior. Sólo se consideran los valores finales de los indicadores.
3. **Contexto histórico** – El EA requiere dos velas completas para determinar las pendientes del indicador. El puerto almacena los valores OsMA anteriores, el valor del índice de fuerza anterior y el máximo/mínimo de la vela anterior para reflejar este requisito.
4. **Configuración de venta**: cuando el histograma OsMA aumenta (`OsMA[1] > OsMA[2]`) y el valor del índice de fuerza anterior es negativo, el robot planifica una orden de límite de venta contraria:
   - El precio base es el máximo de la vela anterior más un punto.
   - Se aplica un colchón de seguridad de 16 pips (configurable) en relación con la oferta actual. El precio objetivo pasa a ser el máximo entre el precio base y `Bid + buffer`.
   - Los precios de stop-loss y take-profit están alineados con el paso del precio del instrumento utilizando las distancias de pips configuradas (35 pips y 150 pips por defecto).
5. **Configuración de compra** – Cuando el histograma de OsMA disminuye (`OsMA[1] < OsMA[2]`) y el índice de fuerza anterior es positivo, la estrategia prepara una orden de límite de compra por debajo del mercado:
   - El precio base es el mínimo de la vela anterior menos un punto.
   - El algoritmo aplica el mismo buffer de 16 pips en relación con la demanda actual, eligiendo el mínimo entre el precio base y `Ask - buffer`.
6. **Mantenimiento de orden pendiente**: si la pendiente de OsMA cambia en la dirección opuesta antes de la ejecución, la orden pendiente correspondiente se cancela. Cuando un lado se completa, la orden pendiente opuesta se elimina para evitar la doble exposición.
7. **Gestión de posición**: tras la ejecución, el precio de cumplimiento se almacena y se activan los niveles de stop-loss y take-profit precalculados. La estrategia también implementa un trailing stop basado en pips (30 pips por defecto) que mueve el stop protector sólo en la dirección favorable cuando el precio avanza más allá de la entrada más la distancia de seguimiento.
8. **Salidas**: las órdenes de protección se monitorean en cada vela completada. Una posición larga se cierra si el mínimo de la vela toca el stop o el máximo de la vela alcanza el objetivo. Las posiciones cortas utilizan las reglas reflejadas.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `OrderVolume` | 1 | Volumen fijo utilizado para órdenes límite pendientes. |
| `StopLossPips` | 35 | Distancia, en pips, entre el precio de entrada y el stop de protección. |
| `TakeProfitPips` | 150 | Distancia, en pips, entre el precio de entrada y el nivel de toma de ganancias. |
| `TrailingStopPips` | 30 | Distancia, en pips, para el trailing stop que asegura las ganancias una vez que el precio se mueve lo suficiente. |
| `EntryBufferPips` | 16 | Diferencia mínima, en pips, entre el precio de mercado actual y la orden pendiente. |
| `PipSize` | 0.0001 | Tamaño de pip utilizado para conversiones de precios; El valor predeterminado es 0,0001, pero se puede alinear con símbolos exóticos. |
| `CandleType` | plazo de 4h | Serie de velas procesadas por la estrategia. |

## Archivos
- `CS/Franks4HourLimitOrdersStrategy.cs` – Implementación principal de C# con comentarios detallados en inglés.
- `README.md`: esta descripción en inglés del algoritmo.
- `README_ru.md` – Documentación rusa.
- `README_zh.md` – Documentación china.

## Notas de implementación
- La estrategia se basa únicamente en API de alto nivel (`SubscribeCandles`, enlaces de indicadores y ayudantes de pedidos de conveniencia).
- Todos los cálculos de precios están alineados con el paso de precio del instrumento para evitar niveles no válidos.
- Las variables de estado almacenan solo los datos históricos necesarios, cumpliendo con la regla del repositorio que prohíbe colecciones personalizadas.
- La gestión de stop-loss, take-profit y trailing stop se realiza dentro de la rutina de procesamiento de velas para emular el comportamiento de seguimiento de MT4.
