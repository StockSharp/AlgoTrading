# Estrategia CloseProfit V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
CloseProfit V2 replica el comportamiento de la utilidad original de MetaTrader que fuerza el cierre de toda la exposición de trading activa una vez que se alcanza un umbral configurable de beneficio o pérdida. El port de StockSharp actúa como un módulo de protección de cuenta: monitorea el PnL flotante en cada vela completada y, cuando se exceden los límites, cancela órdenes pendientes y liquida posiciones. La estrategia está diseñada para ejecutarse junto con entradas discrecionales o automatizadas que dependen del mismo portafolio.

A diferencia de los sistemas generadores de señales, CloseProfit V2 nunca abre posiciones por sí misma. Simplemente observa las métricas de beneficios y pérdidas en tiempo real, permitiendo a los traders automatizar la lógica del "botón de pánico" usada en la versión MQL. La frecuencia de monitoreo se controla mediante una suscripción de velas, lo que hace que el componente sea compatible tanto con backtesting histórico como con entornos de trading en vivo.

## Cómo funciona
1. Cuando la estrategia comienza, captura el valor actual del portafolio como la última instantánea de capital en posición plana y lanza la suscripción de velas configurada.
2. Cada vez que una vela finaliza, la estrategia almacena el precio de cierre y evalúa el beneficio flotante:
   - Si `AllSymbols` está desactivado, solo se rastrea el instrumento principal. El beneficio flotante se calcula como `Position * (lastClose - averagePrice)` por lo que solo se usa el PnL no realizado, reflejando la lógica MQL que suma operaciones abiertas.
   - Si `AllSymbols` está activado, el módulo compara el valor actual del portafolio con la última instantánea de capital en posición plana. Esto mide la ganancia/pérdida no realizada combinada en todos los instrumentos gestionados por la estrategia.
3. Cuando el beneficio flotante supera `ProfitClose` o cae por debajo de `-LossClose`, la estrategia solicita una liquidación completa. Inmediatamente cancela órdenes activas y envía instrucciones de mercado para liquidar todos los instrumentos afectados.
4. Después de que la liquidación se completa y todas las posiciones llegan a cero, la instantánea de capital en posición plana se actualiza. Esto asegura que el monitoreo posterior comience desde el nuevo saldo de cuenta y evite volver a activarse en beneficios realizados.

La implementación refleja el comportamiento del EA MQL original: ignora el PnL histórico realizado y reacciona puramente a posiciones abiertas. Un bloque de protección integrado garantiza que la rutina de cierre se ejecute solo una vez por señal y no envíe spam de solicitudes de cancelación.

## Parámetros
- **ProfitClose (por defecto 10)** – Umbral de beneficio flotante en moneda de cuenta. Cuando las ganancias no realizadas alcanzan este nivel, la estrategia liquida todas las posiciones monitoreadas.
- **LossClose (por defecto 1000)** – Umbral de pérdida flotante. Una vez que el drawdown no realizado supera este valor absoluto, se cierran todas las posiciones para detener pérdidas adicionales.
- **AllSymbols (por defecto false)** – Si es `false`, solo se observa el `Security` principal asignado a la estrategia. Si es `true`, el módulo agrega el PnL flotante de todos los instrumentos del conjunto de posiciones de la estrategia y los liquida todos simultáneamente.
- **CandleType (por defecto marco temporal de 1 minuto)** – Serie de velas usada para la evaluación. El precio de cierre de la vela impulsa los cálculos de beneficio cuando `AllSymbols` está desactivado. Un marco temporal más corto proporciona reacciones más rápidas, mientras que marcos más largos reducen la carga computacional durante los backtests.

## Notas prácticas
- Inicie el componente junto con otras estrategias de trading que compartan el mismo portafolio. Una vez alcanzados los umbrales, CloseProfit V2 cancelará sus órdenes pendientes y cerrará sus posiciones abiertas.
- Los ajustes de comisión y swap no están disponibles en la API de alto nivel de StockSharp, por lo que el PnL flotante se basa puramente en diferencias de precio. Si esos costos importan, aumente los umbrales en consecuencia.
- Debido a que la liquidación depende de órdenes de mercado, asegúrese de que haya suficiente liquidez o buffers de slippage al configurar `ProfitClose` y `LossClose`.
- La suscripción de velas también se usa durante el backtesting para garantizar puntos de evaluación deterministas. En trading en vivo puede cambiar a marcos más rápidos si se requiere monitoreo intra-barra.
- La estrategia llama a `StartProtection()` al inicio para que las comprobaciones de seguridad integradas de StockSharp (por ejemplo, manejo de reconexión) permanezcan activas mientras la utilidad está en ejecución.

## Diferencias de la implementación MQL original
- El filtro de "magic number" de MetaTrader es innecesario: StockSharp identifica órdenes por estrategia, por lo que el módulo ya aísla las posiciones que controla. Por lo tanto `AllSymbols` se aplica a todos los instrumentos manejados por la misma instancia de estrategia.
- El EA MQL gestionaba etiquetas de gráfico para mostrar balance, capital y conteos de tickets. La versión C# usa mensajes de log porque los gráficos de StockSharp son opcionales y no siempre están disponibles en ejecuciones automatizadas.
- El scaffolding de depuración/tester que auto-creaba operaciones de demostración en MQL fue eliminado. La estrategia StockSharp se enfoca puramente en monitoreo y liquidación.

## Cuándo usar
Implemente CloseProfit V2 siempre que se necesite un stop duro en el PnL flotante—ya sea para proteger cuentas financiadas, hacer cumplir políticas de riesgo propietarias, o automatizar objetivos de beneficio basados en sesión. Ajuste el período de velas para alinearse con la velocidad de reacción requerida por su flujo de trabajo de trading.
