# Estrategia Bollinger Bands N Posiciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port de StockSharp del asesor experto MetaTrader **Bollinger Bands N positions**. Monitorea los precios de cierre relativos a un envelope de Bandas de Bollinger y entra en una posición siempre que el mercado finaliza una barra fuera del canal. La gestión de posición replica al experto original imponiendo un límite en la exposición total, colocando offsets fijos de stop-loss y take-profit, y activando un trailing stop una vez que la operación está suficientemente en ganancias.

## Lógica de trading

1. Suscribirse al tipo de vela configurado y calcular Bandas de Bollinger con el período y el ancho seleccionados.
2. En cada vela finalizada, la estrategia primero verifica si una posición existente debe cerrarse:
   - Las posiciones largas salen cuando el precio toca el stop-loss fijo, el take-profit fijo, o cuando el nivel de trailing stop es violado.
   - Las posiciones cortas aplican la lógica simétrica.
3. Si el trading está permitido y no se produjo ninguna salida en la barra actual, se evalúan las señales de entrada:
   - Cuando el precio de cierre está por encima de la banda superior, la estrategia aplana cualquier exposición corta y, si está dentro del límite de posición, abre una nueva posición larga con el volumen solicitado.
   - Cuando el precio de cierre está por debajo de la banda inferior, aplana cualquier exposición larga y abre una posición corta de la misma manera.
4. Los trailing stops se mueven en incrementos definidos por el parámetro de paso del trailing una vez que la operación está adelante por la distancia del trailing más el paso. El nivel del trailing se queda detrás del precio por la distancia del trailing y solo avanza cuando la ganancia aumenta al menos un paso del trailing.

## Gestión de posición

- **Max Positions** define la exposición neta máxima medida como `MaxPositions × Volume`. Dado que StockSharp opera en modo de netting, la estrategia puede mantener solo una posición neta a la vez. El parámetro actúa como un límite de seguridad que impide a la estrategia volver a entrar cuando la posición absoluta actual ya alcanza el límite configurado.
- Las distancias de stop-loss y take-profit se especifican en pips. La estrategia las convierte en precios usando el `PriceStep` de la seguridad. Si el instrumento usa precios de pip fraccionario, puede que necesite ajustar los valores en consecuencia.
- Los trailing stops requieren que tanto la distancia como el paso sean positivos. Cuando la distancia del trailing stop se establece en cero, el módulo de trailing se deshabilita.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `Volume` | Tamaño de orden en lotes usado para cada entrada. | `0.1` |
| `MaxPositions` | Límite de posición neta expresado en múltiplos de `Volume`. | `9` |
| `BollingerPeriod` | Período de retrospectiva para la media móvil de Bollinger. | `20` |
| `BollingerWidth` | Multiplicador de desviación estándar para las Bandas de Bollinger. | `2` |
| `StopLossPips` | Distancia stop-loss en pips. | `50` |
| `TakeProfitPips` | Distancia take-profit en pips. | `50` |
| `TrailingStopPips` | Distancia del trailing stop en pips. Establecer en `0` para deshabilitar. | `5` |
| `TrailingStepPips` | Incremento mínimo de ganancia requerido antes de que el trailing stop avance. | `5` |
| `CandleType` | Marco temporal o tipo de vela personalizado usado para construir las Bandas de Bollinger. | `Marco temporal de 1 minuto` |

## Diferencias del experto MQL5

- El experto original opera en el modo de hedging de MetaTrader y puede mantener posiciones largas y cortas simultáneas. Las estrategias de StockSharp están en modo netting, por lo que este port aplana la exposición contraria antes de entrar en una nueva operación. El parámetro `MaxPositions` por tanto limita el tamaño absoluto de la posición neta en lugar del número de tickets independientes.
- Los stops de órdenes se simulan dentro de la estrategia en lugar de enviarse como órdenes stop adjuntas. Esto coincide con la lógica de trailing de la implementación MQL pero significa que las salidas ocurren en la siguiente vela finalizada.
- La configuración del trailing es validada al inicio. Habilitar un trailing stop con un paso de trailing cero lanza una excepción para imitar la verificación de inicialización original.

## Notas de uso

1. Configure `Volume`, `MaxPositions` y los parámetros de riesgo para que coincidan con el tamaño del contrato del instrumento y el valor del tick.
2. Asegúrese de que la seguridad exponga un `PriceStep` válido. Si el paso es cero o falta, la estrategia usa `1` como respaldo, que puede no encajar en todos los mercados.
3. Inicie la estrategia solo después de que el período de precalentamiento del indicador (período de Bollinger) se haya completado para evitar actuar sobre datos incompletos.
4. Monitoree los registros en busca de errores de validación del paso de trailing cuando personalice los ajustes de riesgo.
