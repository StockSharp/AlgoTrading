# Estrategia de cuadrícula semanal RangeEA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

RangeEA Weekly Grid Strategy es un sistema de cuadrícula de orden límite convertido del asesor experto MetaTrader original. El algoritmo identifica la corriente.
rango de negociación semanal y lo completa con un número configurable de órdenes límite pendientes. Cada orden utiliza stop-loss dinámico y
compensaciones de toma de ganancias escaladas en relación con la distancia entre el precio límite y el precio de mercado actual, respetando al mismo tiempo
distancias mínimas expresadas en puntos. Las ganancias se pueden bloquear cerrando todo el libro una vez que el capital de la cartera crece en un
porcentaje predefinido.

La implementación aprovecha el alto nivel de StockSharp API: las velas impulsan la lógica de decisión, las órdenes pendientes se gestionan con el
Los métodos de ayuda estratégica y los controles de riesgo se exponen como parámetros listos para la optimización.

## Lógica de trading

1. Suscríbete a dos transmisiones de velas:
   - Un período de tiempo definido por el usuario (1 hora por defecto) que impulsa el mantenimiento de la red.
   - Velas semanales que se utilizan para estimar el rango de negociación actual.
2. Para cada vela semanal terminada, actualice el máximo más alto y el mínimo más bajo de las últimas dos semanas. Su diferencia se convierte
el rango de negociación activo.
3. En cada vela comercial terminada:
   - Respete la ventana comercial configurada (`StartTradeHour` a `EndTradeHour`).
   - Opcionalmente, restablezca la cuadrícula al comienzo de cada día de negociación.
   - Si no existen órdenes límite pendientes, distribuya las nuevas órdenes uniformemente entre el rango bajo y el rango alto.
   - Después de que ya se hayan ejecutado dos órdenes, reemplace la penúltima ejecución con una nueva orden al mismo precio cuando la cuadrícula
se reduce a `NumberOfOrders - 2` elementos.
   - Supervise continuamente el patrimonio de la cuenta y liquide todo cuando se alcance el porcentaje de ganancia configurado.
4. Cuando se cierre la ventana de negociación y `CloseAllAtEndTrade` esté habilitado, cancele todas las órdenes pendientes y salga de las posiciones existentes.

## Parámetros

| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `CandleType` | Plazo de negociación utilizado para activar el mantenimiento de la red. | velas de 1 hora |
| `WeeklyCandleType` | Plazo utilizado para derivar los límites del rango. | velas de 1 semana |
| `StartTradeHour` | Hora del día en la que se podrán realizar nuevos pedidos. | 0 |
| `EndTradeHour` | Hora del día en que se detiene la negociación. | 24 |
| `CloseAllAtEndTrade` | Cierre todas las órdenes y posiciones fuera de la ventana de negociación. | cierto |
| `MaxOpenOrders` | Número máximo de órdenes y posiciones simultáneas. | 5 |
| `NumberOfOrders` | Número de órdenes límite en la grilla. | 10 |
| `OrderVolume` | Volumen utilizado para cada pedido. | 0,01 |
| `ResetOrdersDaily` | Reconstruya la red al comienzo de cada día de negociación. | cierto |
| `StopLossPoints` | Distancia mínima de stop-loss en puntos. | 60 |
| `TakeProfitPoints` | Distancia mínima de toma de ganancias en puntos. | 60 |
| `StopLossMultiplier` | Multiplicador aplicado a la distancia dinámica de stop-loss. | 3 |
| `TakeProfitMultiplier` | Multiplicador aplicado a la distancia dinámica de toma de ganancias. | 1 |
| `TargetPercentage` | Porcentaje de ganancia patrimonial que desencadena la liquidación. | 8 |

## Gestión del riesgo

- La estrategia respeta el límite `MaxOpenOrders` para mantener bajo control el número de órdenes y posiciones activas.
- Los niveles de stop-loss y take-profit siempre están al menos a la cantidad configurada de puntos de la entrada y, opcionalmente, pueden ser
ampliado por los parámetros del multiplicador.
- La opción de reinicio diario evita que las órdenes obsoletas se transfieran a una nueva sesión.
- Un objetivo de capital a nivel de cartera permite que la estrategia asegure ganancias al aplanar el libro.

## Notas

- Asegúrese de que el valor seleccionado proporcione velas semanales; de lo contrario, la estrategia no puede calcular el rango.
- Cuando utilice instrumentos con niveles de precios no estándar, ajuste la configuración basada en puntos para que coincida con el tamaño del tick subyacente.
- La optimización de `NumberOfOrders`, `OrderVolume` y los multiplicadores de parada/toma ayudan a adaptar la cuadrícula a diferentes niveles de
volatilidad.
