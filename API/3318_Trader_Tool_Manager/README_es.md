# Panel manual TraderToolEA (port StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen

El asesor experto original de MetaTrader 4 **TraderToolEA v1.8** no es un robot autónomo, sino un panel de control que ayuda a traders discrecionales a gestionar órdenes, grids y niveles protectores. Este port recrea el panel dentro del framework StockSharp. En lugar de botones en el gráfico, la estrategia expone parámetros booleanos que se comportan como interruptores: póngalos en `true` en la GUI o en scripts para disparar la acción correspondiente.

Capacidades clave traducidas:

* Atajos de órdenes de mercado para abrir o cerrar exposición larga/corta.
* Colocación automática de grids simétricos formados por órdenes pendientes stop o limit.
* Cancelación selectiva de órdenes pendientes (compra/venta/todas) con limpieza opcional de huérfanas.
* Gestión virtual de stop-loss, take-profit, trailing stop y break-even impulsada por cotizaciones Level1.
* Opción de auto-tamaño que imita el cálculo de lotes de MetaTrader (`AccountBalance / LotSize * RiskFactor`).

Toda la lógica usa exclusivamente la API de alto nivel: suscripciones Level1, métodos auxiliares de órdenes (`BuyStop`, `SellLimit`, `CancelOrder`...) y las funciones integradas de logging.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `Use Auto Volume` | Si es `true`, la estrategia calcula el lote desde el balance de la cartera y `Risk Factor`; si no, usa `Order Volume` fijo. |
| `Risk Factor` | Multiplicador aplicado al balance de cartera durante el cálculo automático. Equivale a la entrada MT4 `RiskFactor`. |
| `Order Volume` | Lote manual usado para cada orden de mercado o pendiente cuando el auto-tamaño está desactivado. |
| `Distance (pips)` | Separación (en pips MetaTrader) entre órdenes pendientes en capas. Aplica a grids stop y limit. |
| `Layers` | Número de órdenes pendientes adicionales por comando. `1` refleja una pulsación del EA; valores mayores emulan varias pulsaciones. |
| `Delete Orphans` | Cuando está activado, la estrategia cancela automáticamente órdenes pendientes sin pareja para mantener balanceados los grids de compra/venta tras ejecuciones parciales. |
| `Enable Stop Loss` / `Stop Loss (pips)` | Activa la vigilancia de stop-loss fijo medido en pips respecto al precio medio de entrada. |
| `Enable Take Profit` / `Take Profit (pips)` | Activa la vigilancia de take-profit fijo medido en pips. |
| `Enable Trailing` / `Trailing (pips)` | Activa la gestión virtual de trailing stop. El trailing solo se arma cuando el precio avanza al menos `Trailing` pips a favor. |
| `Enable Break-Even` / `Break-Even Trigger` / `Break-Even Lock` | Cuando el precio avanza la distancia de activación, el stop se mueve al precio de entrada más el bloqueo (largos) o menos el bloqueo (cortos). |
| Conmutadores de comando (`Open Buy`, `Place Buy Stops`, `Delete Sell Limits`, ...) | Parámetros booleanos que emulan los botones del EA. Al ponerlos en `true` se ejecuta la acción y la estrategia los reinicia a `false`. |

## Flujo de órdenes

1. **Fuente de datos:** la estrategia solo se suscribe a `DataType.Level1`. Las actualizaciones de mejor bid/ask impulsan la lógica de protección y las colocaciones de grid.
2. **Normalización de volumen:** antes de enviar cualquier orden, el volumen solicitado se redondea al `VolumeStep` del instrumento y se limita entre `MinVolume` y `MaxVolume`. Si faltan metadatos, se usa el valor bruto.
3. **Órdenes pendientes:** los grids stop y limit se construyen alrededor del bid/ask más reciente. Los precios se alinean al paso de precio del instrumento para evitar rechazos del motor de matching.
4. **Control de huérfanas:** cuando `Delete Orphans` está activado, la estrategia mantiene simétrico el número de órdenes pendientes de compra y venta cancelando el lado sobrante tras ejecuciones o cancelaciones manuales. La misma lógica se aplica independientemente a grids stop y limit.
5. **Protección virtual:** stop-loss, take-profit, trailing stop y break-even se implementan como guardas *virtuales*. Cuando se vulnera un umbral, la estrategia envía una orden de mercado de cierre por el volumen restante y reinicia el estado interno de trailing/break-even.

## Diferencias frente a MetaTrader

* Componentes gráficos (botones, cajas de texto, colores, sonidos) se reemplazan por parámetros StockSharp y logs. Cada acción escribe una entrada informativa mediante `AddWarningLog` o el logger predeterminado.
* La lógica protectora opera sobre actualizaciones Level1 y cierra posiciones directamente en lugar de modificar stops en órdenes individuales. Esto conserva comportamiento consistente entre brokers que no admiten stops estilo MetaTrader.
* Los modos `ManageOrders` de MT4 (ID/manual/all/own) se reducen al alcance de la estrategia: solo se rastrean y gestionan órdenes creadas por esta estrategia.
* El tamaño automático usa la valoración de cartera en lugar de `AccountBalance()`, pero conserva la fórmula y reglas de redondeo.

## Consejos de uso

1. Configure metadatos del instrumento (`PriceStep`, `VolumeStep`, `MinVolume`, `LotSize`, ...) en su conexión para que conversión de pips y redondeo de volumen coincidan con las reglas del broker.
2. Vincule los parámetros booleanos de comando a atajos o botones UI en el terminal StockSharp para replicar la experiencia original. Las propiedades vuelven a `false` tras cada invocación correcta.
3. Active `Delete Orphans` al trabajar con grids simétricos para limpiar automáticamente stops/limits sobrantes cuando se activa un lado.
4. Supervise el log informativo: si la estrategia omite una acción (por ejemplo, porque no hay bid/ask o el volumen calculado es cero), se emite una advertencia con el motivo.
5. Como la protección es virtual, mantenga la estrategia corriendo mientras haya posiciones abiertas: cierra operaciones enviando órdenes de mercado, no confiando en stops del servidor.

## Notas de port

* El tamaño de pip replica MetaTrader: instrumentos con 3 o 5 decimales multiplican el paso de precio por 10 para convertir puntos en pips.
* Trailing stops y break-even siguen el flujo del código MQL: solo se arman cuando el precio entra en beneficio y usan variables de estado que se reinician con nuevas operaciones, cancelaciones o reversión de posición.
* El EA permitía pulsar botones varias veces para extender grids. El parámetro `Layers` emula esto creando varios niveles pendientes en una llamada.
* Todos los controles manuales mantienen `SetCanOptimize(false)` para que campañas de optimización no disparen acciones accidentalmente.
