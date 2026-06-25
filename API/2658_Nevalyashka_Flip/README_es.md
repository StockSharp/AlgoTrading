# Estrategia Nevalyashka Flip
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un port directo de StockSharp del experto de MetaTrader "Nevalyashka". La estrategia siempre alterna entre trades largos y cortos: comienza con una orden de venta a mercado, espera a que la posición se cierre por stop loss o take profit, y luego abre inmediatamente una orden a mercado en la dirección opuesta. Las órdenes protectoras se recrean para cada entrada usando los mismos offsets basados en pips que en el código original.

## Lógica de la estrategia

1. **Inicialización**
   - Detecta el paso de precio del instrumento y los decimales para derivar un tamaño de pip idéntico a la versión MQL (los pares de 3/5 dígitos se multiplican por 10).
   - Multiplica el `MinVolume` del exchange por el parámetro `LotMultiplier` para obtener el tamaño de la orden y lo redondea al step de volumen si es necesario.
2. **Manejo de cotizaciones**
   - Se suscribe a actualizaciones del libro de órdenes para capturar los últimos precios bid/ask, imitando la llamada `RefreshRates()` del experto.
3. **Flujo de órdenes**
   - Coloca una orden de venta a mercado inicial una vez que las mejores cotizaciones bid/ask están disponibles.
   - Después de que una posición se cierra, invierte el lado (compra después de venta, venta después de compra) y emite una nueva orden a mercado con el mismo volumen.
   - Para cada entrada ejecutada la estrategia coloca órdenes separadas de stop-loss y take-profit usando los parámetros de distancia en pips.

## Gestión de riesgos

- **Stop Loss**: Opcional. Cuando `StopLossPips` es mayor que cero, la estrategia envía una orden stop protectora (`SellStop` para posiciones largas, `BuyStop` para posiciones cortas) en `entrada ± StopLossPips * pip`.
- **Take Profit**: Opcional. Cuando `TakeProfitPips` es mayor que cero, la estrategia envía una orden límite protectora (`SellLimit` para posiciones largas, `BuyLimit` para posiciones cortas) en `entrada ± TakeProfitPips * pip`.
- Ambas órdenes protectoras se cancelan siempre que la posición esté plana para evitar órdenes pendientes antes del siguiente flip.

## Parámetros

| Nombre | Descripción | Valor predeterminado |
| ---- | ----------- | ------- |
| `LotMultiplier` | Multiplicador aplicado al volumen mínimo del instrumento. El resultado se redondea al step de volumen del exchange. | `1` |
| `StopLossPips` | Distancia de stop-loss en pips. Establecer en `0` para deshabilitar el stop. | `50` |
| `TakeProfitPips` | Distancia de take-profit en pips. Establecer en `0` para deshabilitar el objetivo. | `50` |

## Notas operativas

- El enfoque alterna continuamente la exposición y por lo tanto se adapta a mercados de reversión a la media donde es probable que un movimiento completado se revierta.
- Funciona con cualquier símbolo que proporcione cotizaciones del libro de órdenes; los cálculos de pips se adaptan automáticamente según la precisión del precio.
- El manejo del deslizamiento se delega al exchange—las órdenes se envían a mercado sin verificaciones adicionales como en el experto original.
- La estrategia no incluye filtros de horario de trading, filtros de noticias o trailing stops. Tal lógica puede añadirse extendiendo `TryOpenNextPosition` o `RegisterProtectionOrders`.
