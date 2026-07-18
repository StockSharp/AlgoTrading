# Enorme estrategia de ingresos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una StockSharp versión del MetaTrader 4 asesores expertos "Huge Income". El robot original busca movimientos intradiarios que se alejan de la apertura diaria y entra en una única posición en la dirección de la ruptura. La versión StockSharp mantiene la misma idea al reconstruir el rango máximo/mínimo diario a partir de velas intradiarias, abriendo solo una posición a la vez y forzando una salida justo antes del cierre del mercado configurado.

## Datos y entorno
- **Instrumentos**: Cualquier símbolo que proporcione un paso de precio confiable (`PriceStep`). La lógica fue diseñada para pares de divisas, pero funciona en otros instrumentos después de ajustar los parámetros de los pips.
- **Período de tiempo**: De forma predeterminada, la estrategia se suscribe a velas de 15 minutos para reconstruir la apertura diaria, el máximo y el mínimo. Puede cambiar a un tipo de vela diferente si su fuente de datos ofrece una mejor resolución.
- **Sesiones**: Se espera que el tiempo del gráfico siga el reloj del corredor/servidor exactamente como el script MetaTrader. Establezca las horas límite según esa zona horaria.

## Lógica comercial
1. Reconstruya las estadísticas del día actual cada vez que llegue una nueva vela. La primera vela del día proporciona el precio de apertura e inicializa el máximo/mínimo actual.
2. Sólo se permite una posición (larga o corta) en cualquier momento. Las órdenes pendientes no se utilizan; la estrategia se basa en órdenes de mercado.
3. **Configuración larga**:
   - El cierre actual está por encima de la apertura diaria.
   - La distancia entre la apertura y el mínimo del día actual es mayor que `MinimumRangePips` (convertida a unidades de precio hasta `PriceStep`).
   - La hora actual es estrictamente inferior a `BuyCutoffHour`.
4. **Configuración breve**:
   - El cierre actual está por debajo de la apertura diaria.
   - La distancia entre el máximo del día actual y la apertura es mayor que `MinimumRangePips`.
   - La hora actual es estrictamente inferior a `SellCutoffHour`.
5. Cuando se cumple cualquiera de las configuraciones, la estrategia envía una orden de mercado con tamaño `TradeVolume` y deja de evaluar nuevas entradas hasta que la posición vuelve a estabilizarse.
6. Una vez alcanzado el `MarketCloseHour`, cualquier posición abierta se cierra con una orden de mercado. Esto refleja la lógica MetaTrader que liquida las operaciones cerca del cierre del fin de semana.

## Gestión de riesgos y dinero.
- `TradeVolume` es el tamaño de pedido fijo. No existe un comportamiento de promedio o martingala en el script original, por lo tanto, el puerto StockSharp mantiene un volumen constante.
- No existen niveles explícitos de stop-loss o take-profit. El asesor experto se basa en el filtro de rango diario y el cierre forzado cerca del final de la sesión para controlar el riesgo. Puede ampliar la estrategia agregando paradas o lógica de seguimiento si es necesario.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Tamaño de posición utilizado al enviar pedidos `BuyMarket` o `SellMarket`. |
| `MinimumRangePips` | Distancia mínima (en pips) entre la apertura diaria y el extremo opuesto antes de que se permita una operación. Convertido a una diferencia de precio absoluta usando `Security.PriceStep`. |
| `BuyCutoffHour` | Última hora (0–23) en la que se pueden abrir nuevas entradas largas. La comparación es estricta (`currentHour < BuyCutoffHour`). |
| `SellCutoffHour` | Última hora (0–23) en la que se pueden abrir nuevas entradas cortas. |
| `MarketCloseHour` | Hora del día en que se liquidan todas las posiciones abiertas. Configúrelo en 23 para que coincida con el comportamiento de cierre original de EA los viernes. |
| `CandleType` | Marco de tiempo utilizado para suscribirse a velas y reconstruir estadísticas diarias. |

## Diferencias con la versión MT4
- StockSharp recibe datos de velas en lugar de ticks individuales. Si el feed MetaTrader de su corredor se basó en actualizaciones paso a paso, elija un intervalo de vela lo suficientemente pequeño para emular la misma capacidad de respuesta.
- El filtro `MinimumRangePips` se desactiva automáticamente cuando el instrumento carece de un `PriceStep`. En ese caso, se acepta toda ruptura por encima o por debajo de la apertura.
- Todas las operaciones se ejecutan con órdenes de mercado y se aplanan inmediatamente en `MarketCloseHour`, replicando el bucle `OrderClose` del código original sin órdenes pendientes.

## Consejos de uso
- Ajuste el período de tiempo de la vela para que coincida con su velocidad de ejecución preferida. Las velas más cortas rastrean el máximo/mínimo diario con mayor precisión, pero requieren más datos.
- Revise el horario de negociación del instrumento. Si el mercado cierra antes de su `MarketCloseHour` configurado, la salida forzada se activará el siguiente día de negociación.
- Combine la estrategia con protecciones a nivel de cartera o cuenta (por ejemplo, `StartProtection`) si necesita límites de stop-loss o de reducción más allá del diseño original.
