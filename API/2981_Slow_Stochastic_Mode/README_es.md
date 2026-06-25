# Estrategia Slow Stochastic Mode
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Slow Stochastic Mode** es una conversión del asesor experto de MetaTrader `Exp_Slow-Stoch.mq5` a la API de alto nivel de StockSharp. El sistema opera sobre el precio de cierre de velas finalizadas y utiliza un oscilador estocástico suavizado para detectar cambios de régimen. Hay tres modos de señal distintos disponibles para que el trader decida si reaccionar a rupturas de nivel, giros de momentum o cruces de líneas.

## Idea principal

La estrategia observa las líneas %K y %D de un oscilador estocástico lento que está adicionalmente suavizado por el parámetro `Slowing`. Dependiendo del *Modo de señal* seleccionado, el algoritmo evalúa el oscilador uno o más barras atrás (controlado por `SignalBar`) y abre una nueva posición o cierra el lado opuesto cuando aparece un evento calificado. Las órdenes siempre se colocan con ejecuciones de mercado.

## Modos de señal

- **Breakdown** – busca que %K rompa el nivel 50. Un cruce de abajo hacia arriba de 50 genera una entrada larga y cierra posiciones cortas. Un cruce de arriba hacia abajo de 50 produce una entrada corta y cierra posiciones largas.
- **Twist** – detecta un cambio de dirección de %K. Cuando el oscilador estaba cayendo dos barras atrás y gira hacia arriba en la barra evaluada, la estrategia abre o invierte a una operación larga. La situación inversa activa cortos.
- **CloudTwist** – rastrea el cambio de color de la "nube" estocástica observando un cruce de %K vs %D. Un cruce alcista (%K por encima de %D) abre o protege largos, mientras que un cruce bajista (%K por debajo de %D) hace lo contrario.

Todos los modos respetan los cuatro interruptores de permiso: las entradas largas/cortas y las salidas largas/cortas pueden habilitarse o deshabilitarse de forma independiente.

## Parámetros

| Parámetro | Valor predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | Marco temporal H4 | Tipo de vela utilizado para los cálculos del indicador. |
| `KPeriod` | 5 | Período de retrospectiva para la línea %K. |
| `DPeriod` | 3 | Longitud de la media móvil para %D. |
| `Slowing` | 3 | Suavizado adicional aplicado a %K antes de las comparaciones. |
| `SignalBar` | 1 | Número de barras cerradas hacia atrás utilizadas para evaluar las señales. |
| `StopLossPoints` | 1000 | Distancia de stop-loss en pasos del instrumento (poner 0 para deshabilitar). |
| `TakeProfitPoints` | 2000 | Distancia de take-profit en pasos del instrumento (poner 0 para deshabilitar). |
| `EnableLongEntries` | true | Permite a la estrategia abrir posiciones largas. |
| `EnableShortEntries` | true | Permite a la estrategia abrir posiciones cortas. |
| `EnableLongExits` | true | Permite cerrar posiciones largas cuando aparece una señal de reversión. |
| `EnableShortExits` | true | Permite cerrar posiciones cortas cuando aparece una señal de reversión. |
| `Mode` | Twist | Modo de señal seleccionado. |

La estrategia utiliza el indicador integrado de StockSharp `StochasticOscillator` y lo alimenta con las longitudes configuradas. El parámetro `SignalBar` permite reproducir el comportamiento de MetaTrader de referenciar la vela anterior (predeterminado = 1) o actuar sobre la última barra completada cuando se establece en 0.

## Gestión de operaciones

- Las órdenes se envían con llamadas `BuyMarket` y `SellMarket`. Los giros de posición se manejan automáticamente añadiendo el valor absoluto de la posición actual al volumen base de la orden.
- La protección opcional de stop-loss y take-profit se activa a través de `StartProtection`. Las distancias se interpretan como ticks/pasos, por lo que StockSharp las multiplica internamente por el tamaño del paso del instrumento.
- No se adjunta trailing stop; la protección permanece estática hasta que se ejecuta o la estrategia sale manualmente.

## Lógica de salida

- En modo **Breakdown**, la misma ruptura de umbral que abre un lado cierra el otro.
- En modo **Twist**, detectar una reversión de momentum cierra la posición opuesta antes de abrir la nueva.
- En modo **CloudTwist**, el cruce de %K con %D tanto activa la entrada como simultáneamente liquida el sesgo opuesto.

Cuando los permisos de entrada están deshabilitados, solo permanece activa la lógica de salida correspondiente, lo que permite a los usuarios ejecutar la estrategia en un modo de "gestionar exposición existente".

## Notas de implementación

- El historial del oscilador se rastrea en pequeños búferes en memoria para que la estrategia pueda inspeccionar los desplazamientos de barra requeridos por el asesor experto original.
- Todas las decisiones se evalúan solo en velas finalizadas (`candle.State == Finished`).
- El renderizado del gráfico dibuja las velas subyacentes y el oscilador estocástico cuando hay servicios de gráficos disponibles.

Esta conversión mantiene la intención del sistema MQL5 original aprovechando las vinculaciones de indicadores, los metadatos de parámetros y los controles de riesgo integrados de StockSharp.
