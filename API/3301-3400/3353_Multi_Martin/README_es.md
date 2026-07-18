# Estrategia MultiMartin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

`MultiMartinStrategy` es la conversión StockSharp del asesor experto MQL5 **MultiMartin**. El robot original es una martingala multidivisa que alterna operaciones largas y cortas en señales de reversión y aumenta el tamaño de la orden después de perder operaciones. Este puerto mantiene la lógica central de administración de dinero mientras utiliza API de alto nivel de StockSharp para enrutamiento de órdenes, monitoreo de posiciones, paradas finales opcionales y manejo de rechazos de corredores.

La estrategia abre continuamente una posición de mercado única en el instrumento configurado. Después de cada salida, mantiene la dirección (si la operación fue rentable) o invierte la dirección (si la operación perdió dinero). Las operaciones perdedoras desencadenan un paso de martingala que multiplica el siguiente volumen de orden hasta alcanzar un techo configurable.

## Lógica comercial

1. **Selección de entrada**
   - La estrategia utiliza un filtro de tiempo para limitar las operaciones a una ventana intradiaria. Fuera de esta ventana no se envían nuevas entradas.
   - Cuando no hay ninguna posición abierta y el corredor no está en estado de enfriamiento, la estrategia envía una orden de mercado en la dirección actual. La primera dirección está definida por el usuario (comprar o vender).
2. **Martingale tamaño**
   - Después de cada pérdida, el volumen del siguiente pedido se multiplica por el parámetro `Factor`.
   - La multiplicación está limitada por `Limit`, que define el número máximo de duplicaciones consecutivas. Una vez que se excede el límite, el volumen se restablece a la base `Volume`.
   - Las operaciones rentables siempre restablecen el volumen al tamaño base y mantienen la dirección de la operación.
3. **Gestión de salida**
   - Las distancias de stop-loss y take-profit se expresan en puntos de precio y se convierten a distancias absolutas utilizando el instrumento `PriceStep`.
   - Los modos de seguimiento opcionales mueven el stop-loss hasta el punto de equilibrio o lo siguen linealmente detrás del precio.
   - Las salidas se manejan mediante órdenes de mercado una vez que los extremos de las velas superan el umbral de parada o de toma.
4. **Manejo de rechazos del corredor**
   - Si se rechaza una orden, la estrategia entra en un período de recuperación controlado por `SkipBadTime`. Durante el tiempo de reutilización no se intentan nuevas entradas. La opción `Forever` desactiva el comercio durante el resto de la sesión.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `UseTimeFilter` | Habilite o deshabilite la ventana de negociación intradiaria. |
| `HourStart` | Hora incluida (0-23) en la que se activa la negociación. |
| `HourEnd` | Hora exclusiva (1-24) en la que se detiene la negociación. Admite ventanas nocturnas (por ejemplo, 22-2). |
| `Volume` | Volumen base de pedidos en lotes o contratos. |
| `Factor` | Multiplicador aplicado al siguiente volumen de orden después de una operación perdedora. |
| `Limit` | Número máximo de multiplicaciones consecutivas antes de que se reinicie el volumen. |
| `StopLossPoints` | Distancia de stop-loss expresada en puntos del instrumento. Establezca en 0 para desactivar la parada. |
| `TakeProfitPoints` | Distancia de toma de ganancias expresada en puntos del instrumento. Establezca en 0 para desactivar el objetivo. |
| `StartDirection` | Primera dirección comercial (`Buy` o `Sell`). |
| `SkipBadTime` | Intervalo de recuperación aplicado después de una orden de mercado rechazada. `Forever` bloquea más entradas. |
| `TrailMode` | Modo de seguimiento: `None`, `Breakeven` o `Straight` (seguimiento lineal). |
| `CandleType` | Serie de velas utilizadas para gestionar salidas y filtrado de tiempo. |

## Diferencias versus la versión MQL5

- El puerto StockSharp intercambia un único valor por instancia de estrategia. Inicie múltiples instancias para cubrir múltiples símbolos.
- La gestión de stop-loss y take-profit se basa en velas; Los rellenos se ejecutan con órdenes de mercado tan pronto como el rango de velas toca los umbrales.
- Los rechazos del corredor utilizan la devolución de llamada `OnOrderFailed` de StockSharp para activar el tiempo de reutilización de `SkipBadTime` en lugar del temporizador global de MQL5.
- Las opciones de trailing stop se reimplementaron utilizando lógica a nivel de estrategia en lugar de llamadas directas de modificación de órdenes.

## Notas de uso

- Configure el `Security` y el `Portfolio` antes de iniciar la estrategia.
- Asegúrese de que `Volume` sea compatible con las reglas de tamaño de lote y volumen fraccionario del instrumento.
- Establezca `StopLossPoints`/`TakeProfitPoints` en cero para desactivar las respectivas órdenes de protección.
- Al realizar una prueba retrospectiva, elija un tipo de vela que coincida con el conjunto de datos históricos (por ejemplo, velas de 1 minuto para pares de divisas).
- Para simular el comportamiento original de múltiples símbolos, implemente múltiples instancias de estrategia con diferentes valores y parámetros.

## Advertencias de riesgo

Martingale la administración del dinero es inherentemente riesgosa. Las rachas perdedoras pueden aumentar exponencialmente la exposición y consumir rápidamente el margen disponible. Utilice configuraciones de volumen conservadoras, pruebe con datos históricos y aplique controles de riesgo estrictos antes de utilizar la estrategia en producción.
