# Estrategia de Trailing Stop con Punto de Equilibrio por Ticks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Gestor de trailing stop basado en ticks convertido del asesor experto de MetaTrader `e_Breakeven_v4`.
- Monitorea cada tick de operación para mover un stop-loss virtual una vez que el precio se desplaza suficientemente desde la entrada.
- Cierra posiciones largas o cortas a mercado cuando se alcanza el nivel de trailing, replicando el comportamiento de punto de equilibrio más paso del EA original.
- Incluye un modo demo opcional que abre posiciones aleatorias durante las pruebas para demostrar la lógica de trailing sin una fuente de señal externa.

## Cómo funciona
1. La estrategia se suscribe a ticks de operación (`DataType.Ticks`) para emular el callback `OnTick` usado en MQL5.
2. Cuando existe una posición y el trailing stop (en pips) más el paso de trailing han sido superados, el nivel de stop se desplaza más cerca del precio.
3. Para posiciones largas, el stop se coloca en `precio actual - trailing stop` si el movimiento desde la entrada supera `trailing stop + trailing step`.
4. Para posiciones cortas, el stop se coloca en `precio actual + trailing stop` cuando el precio se mueve hacia abajo la misma distancia.
5. Si el precio en vivo toca o cruza el nivel de stop almacenado, la estrategia sale de toda la posición a mercado y reinicia el estado de trailing.
6. Una conversión interna de pips multiplica el paso de precio del broker por 10 cuando el instrumento tiene 3 o 5 dígitos decimales, coincidiendo con el ajuste punto-a-pip de MQL5.
7. Cuando el modo demo está habilitado, la estrategia abre una operación larga o corta aleatoria (usando el `Volume` configurado) la primera vez que llega un nuevo tick después de que la entrada anterior se cierra.

## Parámetros
| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `TrailingStopPips` | Distancia en pips entre el precio actual y el trailing stop. | 10 | Establecer en `0` para deshabilitar el trailing completamente. |
| `TrailingStepPips` | Distancia adicional en pips requerida antes de que el stop avance nuevamente. | 1 | Debe ser mayor que cero cuando el trailing stop está activo, reproduciendo la regla de validación del EA. |
| `EnableDemoEntries` | Habilita entradas aleatorias para backtests sin una señal externa. | `false` | Cuando `true`, la estrategia lanza una moneda en cada tick mientras está plana para decidir la dirección. |

## Reglas de gestión de posición
- La estrategia no abre posiciones por sí misma a menos que `EnableDemoEntries` esté en `true`.
- El trailing es simétrico para posiciones largas y cortas y funciona con cualquier tamaño de volumen.
- Los niveles de stop se gestionan internamente (virtuales) y se aplican con salidas a mercado, evitando órdenes stop explícitas que pueden no ser compatibles con todos los conectores.
- Cualquier operación manual o estrategia externa puede suministrar las entradas; este componente solo gestionará el trailing stop.

## Notas de uso
- Funciona mejor con instrumentos que proporcionan ticks de operación para que el trailing reaccione inmediatamente.
- Asegúrese de que `Volume` esté configurado al tamaño de lote que corresponde a las posiciones entrantes si se usa el modo demo.
- La conversión de pips asume precios estilo FX donde los símbolos con 3 o 5 decimales necesitan un multiplicador ×10 para convertir puntos en pips.
- La salida se activa en el primer tick que cruza el precio de stop almacenado, coincidiendo con el flujo inmediato de modificación y cierre de la lógica MQL.

## Diferencias respecto al experto MQL5 original
- Usa stops virtuales con salidas a mercado en lugar de modificar órdenes stop-loss del lado del broker porque las estrategias StockSharp típicamente gestionan las salidas a través de la lógica de la estrategia.
- Reemplaza el bloque de entrada aleatoria del tester de MetaTrader con el flag configurable `EnableDemoEntries`.
- Convierte la lógica punto-a-pip usando `Security.PriceStep` y conteo de decimales en lugar de `Symbol().Digits()`.
- Todos los comentarios y registros son ahora en inglés de acuerdo con las pautas del repositorio.
