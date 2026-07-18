# Estrategia Viva Las Vegas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Viva Las Vegas es un divertido experto en administración de dinero que compra o vende aleatoriamente el instrumento adjunto y luego deja que uno de los cinco sistemas de apuestas decida el tamaño de la siguiente apuesta. El puerto StockSharp mantiene el comportamiento original MetaTrader al:
- Elegir una dirección comercial mediante un lanzamiento de moneda pseudoaleatorio en cada nuevo intento.
- Colocar inmediatamente protecciones simétricas de stop-loss y take-profit expresadas en pips.
- Actualizar la secuencia de progresión tan pronto como se cierre la posición anterior y abrir una nueva posición de inmediato.

Por lo tanto, la estrategia permanece constantemente expuesta (una posición abierta a la vez) y muestra cómo se comportan varios sistemas de apuestas clásicos dentro del marco comercial de StockSharp.

## Módulos de administración de dinero
El parámetro `MoneyManagement` selecciona uno de los siguientes modelos de participación, todos los cuales utilizan `BaseVolume` como tamaño de lote ancla:

1. **Martingale**: duplica el tamaño del lote después de cada operación perdedora y restablece el volumen base después de una operación rentable.
2. **Pirámide negativa**: duplica el tamaño del lote después de una pérdida, pero reduce el volumen a la mitad después de una ganancia (nunca por debajo del volumen base).
3. **Labouchere**: mantenga una secuencia numérica (predeterminada `1-2-3`), apueste la suma del primer y último número, elimínelos después de una victoria y agregue su suma después de una pérdida.
4. **Oscar's Grind**: aumenta la apuesta en el lote base después de cada victoria hasta que se haya acumulado un lote base de ganancias, luego reinicia; las pérdidas sólo disminuyen el resultado de la carrera.
5. **Sistema 31**: recorre la serie `1,1,1,2,2,4,4,8,8`, duplicando el elemento actual después de la primera victoria y reiniciando al principio después de la segunda victoria consecutiva.

Todos los módulos siguen de cerca la implementación original de MQL, incluida cómo reaccionan las progresiones de volumen a los empates (las operaciones sin ganancias se tratan como pérdidas).

## Flujo de trabajo comercial
1. Al iniciar, la estrategia genera el generador pseudoaleatorio (basado en el tiempo cuando `Seed = 0`) y habilita el motor protector de StockSharp con paradas y objetivos simétricos.
2. Cuando no hay ninguna posición abierta y no hay ninguna orden pendiente, la estrategia solicita al módulo de apuesta activo el siguiente tamaño de lote, lo redondea al `VolumeStep` del instrumento y lanza una moneda para elegir entre `BuyMarket` y `SellMarket`.
3. Una vez establecida la posición, el módulo de protección gestiona la salida utilizando la distancia de pips configurada.
4. Cuando la posición vuelve a ser plana, se evalúa el delta de PnL realizado:
   - Beneficio > 0 → el módulo recibe una notificación **ganar**.
   - Beneficio ≤ 0 → el módulo recibe una notificación de **pérdida**.
5. El proceso se repite inmediatamente, por lo que la cuenta siempre está en una operación o esperando a que se vuelva a llenar.

Debido a que solo existe una posición en un momento dado, la estrategia es fácil de seguir en un gráfico y refleja perfectamente el comportamiento de ticket único del asesor experto original.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `StopTakePips` | `int` | `50` | Distancia (en pips) aplicada a las órdenes stop-loss y take-profit a través de `StartProtection`. |
| `BaseVolume` | `decimal` | `1` | El tamaño del lote ancla influyó en la progresión de la gestión del dinero. |
| `MoneyManagement` | `MoneyManagementMode` | `Martingale` | Algoritmo de apuesta que controla cómo se calcula el tamaño del siguiente pedido. |
| `Seed` | `int` | `0` | Semilla generadora pseudoaleatoria. Un valor de cero cambia a una semilla dependiente del tiempo, por lo que cada ejecución es diferente. |

## Notas de implementación
- Los volúmenes se normalizan según el `VolumeStep` del instrumento y se comparan con `MinVolume` / `MaxVolume` para evitar pedidos rechazados.
- Las distancias de parada/toma se convierten en pasos de precio utilizando la regla clásica MetaTrader (`Digits` igual a 3 o 5 implica diez ticks por pip).
- Las ganancias obtenidas se miden a través de la propiedad `PnL` de la estrategia, lo que garantiza que las salidas protectoras y los cierres manuales influyan en la secuencia de apuestas exactamente como en el código original.
- Los comentarios en línea en inglés resaltan los puntos de decisión, lo que facilita la adaptación de la plantilla con fines educativos o experimentos de riesgo controlado.

## Consejos de uso
- Elija un conector de demostración o un entorno de reproducción; El algoritmo es intencionalmente arriesgado y está destinado a la experimentación.
- Ajuste `BaseVolume` para que coincida con el tamaño del contrato del instrumento antes de comenzar la estrategia.
- Combine la estrategia con gráficos StockSharp para observar cómo cada sistema de apuestas aumenta o contrae el tamaño de la posición con el tiempo.
