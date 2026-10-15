# Estrategia SimpleTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión en C# del asesor experto de MetaTrader 5 "SimpleTrade (edición de barabashkakvn)". Compara el precio de apertura de la barra actual con el precio de apertura de tres barras atrás. Si la apertura actual es mayor, la estrategia va larga; de lo contrario va corta. Cada posición se mantiene durante solo una vela completada y está asegurada con una distancia de stop-loss fija expresada en pips.

La implementación en StockSharp se suscribe a la serie de velas seleccionada a través de la API de alto nivel y reacciona solo a las barras terminadas, asegurando que las decisiones se basan en datos de precio completos. Las posiciones se cierran en la siguiente transición de barra o antes si el nivel de stop es tocado dentro del rango de la barra.

## Lógica de trading
- **Entrada**
  - En cada barra completada, almacenar su precio de apertura y mantener un historial deslizante de las últimas cuatro aperturas.
  - Cuando no hay posición abierta y hay al menos cuatro precios de apertura disponibles, comparar la apertura más reciente con la registrada tres barras atrás.
  - Entrar largo si la apertura actual está por encima de la apertura de tres barras atrás; de lo contrario entrar corto.
- **Salida**
  - Cada operación está protegida por un nivel de stop calculado como *StopLossPips × tamaño de pip* desde el precio de apertura de entrada.
  - En la siguiente barra la posición se cierra independientemente del resultado, replicando el asesor experto original que nunca mantiene una operación más de una vela.
  - Si el máximo de la barra (para cortos) o el mínimo (para largos) penetra el nivel de stop, la estrategia intenta cerrar la posición inmediatamente a mercado.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `StopLossPips` | 120 | Distancia desde el precio de apertura de entrada al stop protector, medida en pips. El código reproduce el comportamiento de MetaTrader multiplicando el paso de precio por 10 para símbolos cotizados con 3 o 5 decimales. |
| `TradeVolume` | 1 | Volumen de orden usado para entradas de mercado. Ajústalo para alinearlo con el tamaño del contrato del instrumento negociado. |
| `CandleType` | Marco temporal de 1 hora | Especifica la serie de velas a la que se suscribe la estrategia. Selecciona el marco temporal que corresponde al gráfico usado en MetaTrader. |

Todos los parámetros están expuestos como objetos `StrategyParam<T>` para que puedan optimizarse o cambiarse a través de la interfaz gráfica.

## Notas de implementación
- El historial deslizante de cuatro precios de apertura se mantiene sin colecciones para cumplir con las directrices del repositorio.
- Los stops no se envían como órdenes separadas; en cambio, la lógica verifica los rangos de velas y emite una salida a mercado cuando el nivel de stop habría sido activado.
- Dado que StockSharp procesa posiciones de forma asíncrona, la estrategia sale de una operación existente antes de evaluar una nueva señal de entrada. En trading en vivo, esto refleja la secuencia original de "cerrar y luego reabrir" al tiempo que evita órdenes superpuestas.
- El tamaño de pip se deriva de `Security.PriceStep`. Para símbolos de 5 o 3 dígitos, el paso se multiplica por diez para que un pip coincida con la definición de MetaTrader.

## Consejos de uso
- Ejecuta la estrategia en instrumentos con tamaños de tick consistentes donde los stops basados en pips sean significativos (por ejemplo, pares de Forex principales).
- Optimiza el valor de `StopLossPips` por instrumento; valores grandes amplían el buffer protector, mientras que valores más pequeños hacen la estrategia más sensible al ruido intrabarra.
- Asegúrate de que la conexión con el broker envíe actualizaciones de velas con estados finales para que la estrategia reciba los precios de apertura correctos.

## Riesgos y limitaciones
- Mantener operaciones durante solo una barra significa que la estrategia depende en gran medida del marco temporal elegido. Es esencial hacer backtesting con diferentes duraciones de velas.
- Usar los extremos de la vela para emular la ejecución de stops introduce deslizamiento en mercados volátiles en comparación con las órdenes stop nativas.
- La estrategia siempre permanece en el mercado (ya sea larga o corta) después de las primeras cuatro barras de datos, lo que puede generar operaciones frecuentes en mercados laterales.
