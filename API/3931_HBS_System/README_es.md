# Estrategia del sistema HBS (versión StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia del sistema HBS** es una conversión de alto nivel StockSharp del asesor experto MetaTrader 4 "HBS system.mq4" (ForTrader.ru). El EA original combina el filtrado de media móvil exponencial con órdenes stop pendientes que se redondean a niveles de precios fijos. Se despliegan dos órdenes stop en la dirección de la tendencia: la primera apunta a un nivel redondeado cercano y la segunda busca una ruptura prolongada. Ambas operaciones comparten la misma lógica protectora de parada y seguimiento, lo que produce una estructura de ruptura en capas.

Este puerto StockSharp mantiene el comportamiento de múltiples órdenes mientras adopta el nivel alto API. Las órdenes se envían a través de los asistentes de órdenes pendientes (`BuyStop`, `SellStop`, `SellLimit`, `BuyLimit`) y el riesgo se controla mediante paradas de protección mantenidas dinámicamente. El código está completamente comentado en inglés para facilitar su mantenimiento.

## Lógica de trading

1. **Filtro de tendencias**: una media móvil exponencial (EMA) calculada sobre el precio medio (`(High + Low) / 2`) de velas completadas define la tendencia activa. Solo se procesan velas completamente formadas, lo que refleja el comportamiento de `iMA(..., shift=1)` de MetaTrader.
2. **Redondeo de niveles**: el precio de cierre de la vela anterior se redondea hacia arriba y hacia abajo utilizando un multiplicador configurable (predeterminado `100`, es decir, dos decimales). Estos valores redondeados emulan las llamadas `MathCeil`/`MathFloor` originales.
3. **Construcción de entrada**: cuando la vela anterior se abre y cierra por encima del EMA, se colocan dos órdenes stop de compra:
   - **Pedido principal** en `roundedHigh - entryOffset` con una obtención de beneficios igual al nivel redondeado.
   - **Pedido secundario** al mismo precio de entrada pero con una toma de ganancias desplazada aún más en `secondaryTakeProfitPoints`.
   - Ambas órdenes comparten un límite de pérdidas común (`entry - stopLossPoints`).

La lógica se refleja en los cortos cuando la vela se abre y cierra por debajo del EMA. Las órdenes pendientes opuestas se cancelan automáticamente para evitar superposiciones.
4. **Gestión de posición**: cuando se ejecuta una orden pendiente, la estrategia registra una orden límite de obtención de ganancias dedicada y actualiza el stop-loss compartido. La lógica del trailing stop refuerza el stop cuando el precio se mueve a favor de la posición abierta, respetando las distancias de seguimiento configuradas.
5. **Limpieza**: los pedidos completados o cancelados se eliminan del registro interno. Cuando la posición neta vuelve a estabilizarse, todas las órdenes de protección se cancelan para restablecer el estado.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `EMA Period` | Longitud del filtro de media móvil exponencial. | 200 |
| `Buy Stop-Loss (points)` | Distancia (en puntos) entre la entrada larga y su tope de protección. | 50 |
| `Buy Trailing (points)` | Distancia de seguimiento para posiciones largas. | 10 |
| `Sell Stop-Loss (points)` | Distancia (en puntos) entre la entrada corta y su tope de protección. | 50 |
| `Sell Trailing (points)` | Distancia de seguimiento para posiciones cortas. | 10 |
| `Order Volume` | Volumen aplicado a **cada** orden pendiente. Con las dos órdenes predeterminadas, la exposición máxima equivale al doble de este valor. | 0.1 |
| `Entry Offset (points)` | Compensación (en puntos) restada/suma del nivel redondeado para obtener el precio de entrada pendiente. | 15 |
| `Second Take-Profit (points)` | Distancia adicional utilizada por el objetivo secundario de obtención de beneficios. | 15 |
| `Rounding Factor` | Multiplicador utilizado para la lógica de redondeo (por ejemplo, 100 → dos decimales). | 100 |
| `Candle Type` | Tipo de datos para agregación de velas. El valor predeterminado es un período de tiempo de 1 hora. | `TimeFrame(1h)` |

## Notas de uso

- Asegúrese de que `Security.PriceStep` (o `Security.Decimals`) esté configurado; de lo contrario, la estrategia vuelve a caer a un valor de 0,0001 puntos.
- Cada orden pendiente gestiona su propia obtención de beneficios, por lo que la posición total puede ampliarse en dos etapas.
- Los trailingstops solo se activan después de que el precio se haya movido a favor en la distancia configurada (`TrailingStop{Buy/Sell}Points`).
- La estrategia asume precios tradicionales al estilo Forex, donde es significativo redondear a dos decimales. Ajuste el `RoundingFactor` si se requiere una precisión diferente.
- No se incluyen reglas automatizadas de administración de dinero; establezca `OrderVolume` según las preferencias de riesgo.

## Aspectos destacados de la conversión

- Todos los comentarios fueron reescritos en inglés y la estructura sigue la guía de estilo del repositorio (pestañas, espacio de nombres, nombres).
- Los ayudantes de alto nivel StockSharp se utilizan para la suscripción de datos, la gestión de pedidos pendientes y el manejo de órdenes de protección.
- La coordinación de trailing stop y take-profit reproduce la arquitectura de orden dual del experto MetaTrader original sin dejar de ser idiomática para StockSharp.

## Referencias

- Secuencia de comandos MT4 original: `MQL/8134/HBS_system.mq4`
- StockSharp documentación: [https://doc.stocksharp.com/](https://doc.stocksharp.com/)
