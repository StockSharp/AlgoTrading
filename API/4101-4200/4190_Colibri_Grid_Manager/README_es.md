# Estrategia de Colibri Grid Manager
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Colibri Grid Manager es un puerto StockSharp del asesor experto MetaTrader 4 `Colibri.mq4` (carpeta original `MQL/9713`). La estrategia se centra en el comercio discrecional en red: prepara órdenes pendientes en capas según la demanda, dimensiona cada orden utilizando el presupuesto de riesgo configurado, adjunta salidas protectoras y aplica un límite de reducción diario antes de deshabilitar el comercio adicional.

## Lógica de trading
1. Cuando comienza la estrategia, se suscribe a la serie de velas y al libro de órdenes seleccionados para realizar un seguimiento de los precios de referencia, restablece la base de ganancias diaria y borra las órdenes anteriores.
2. Si `EnableGrid` es verdadero y no existen posiciones ni órdenes de cuadrícula activas, la estrategia crea una cuadrícula nueva para cada dirección permitida (`AllowBuy`, `AllowSell`). Las órdenes se pueden distribuir en torno a un precio central manual o en relación con anclajes de entrada de compra/venta explícitos.
3. El tipo de orden (`OrderType`) controla si la cuadrícula utiliza entradas de mercado limitadas, de parada o inmediatas. La distancia entre niveles se establece en puntos mediante `LevelSpacingPoints` y se convierte en incrementos de precio utilizando el tamaño del tick del instrumento.
4. El volumen es fijo (`FixedOrderVolume`) o se deriva de `RiskPercent`. El dimensionamiento basado en el riesgo asigna el porcentaje configurado del capital de la cartera actual en todos los niveles en una dirección y lo divide por el riesgo monetario que implica el tope protector.
5. Una vez que se completa una orden de entrada, la estrategia coloca automáticamente órdenes de protección emparejadas: las paradas se derivan de `StopLossPrice` o `StopDistancePoints`, mientras que las tomas de ganancias dependen de `TakeProfitDistancePoints` o, de forma predeterminada, a un paso de la cuadrícula. Los pedidos pendientes pueden caducar después de `ExpirationHours` horas.
6. La estrategia monitorea continuamente el PnL realizado y flotante. Si la pérdida del día de negociación actual supera `DailyLossLimitPercent`, cancela todas las órdenes, cierra posiciones abiertas y suspende la creación de una nueva red hasta que comience el día siguiente.
7. Los cambios manuales (`CloseAllPositions`, `CloseLongPositions`, `CloseShortPositions`, `CancelOrders`) permiten al comerciante aplanar o limpiar el libro instantáneamente sin tocar el código.

## Parámetros
- **EnableGrid**: interruptor maestro que habilita o deshabilita el mantenimiento automático de la red.
- **OrderType**: tipo de orden de entrada (`Limit`, `Stop`, `Market`) utilizado al crear niveles.
- **AllowBuy / AllowSell**: elija los lados que pueden participar en la grilla.
- **UseCenterLine / CenterPrice**: cuando está habilitado, distribuye los niveles de compra/venta simétricamente alrededor de un precio central; un centro cero utiliza el precio medio.
- **LevelSpacingPoints**: espacio entre niveles consecutivos, medido en puntos y convertido a diferencias de precio absolutas mediante el tamaño del tick del instrumento.
- **LevelsCount** – número de niveles por dirección. Para el modo mercado solo se envía una orden independientemente de este valor.
- **BuyEntryPrice / SellEntryPrice**: anclajes explícitos para cuadrículas largas y cortas cuando el modo central está deshabilitado (el valor predeterminado es cero para la oferta/demanda actual).
- **StopLossPrice**: nivel de parada absoluto aplicado a cada orden. Deje cero para derivar la parada de `StopDistancePoints`.
- **StopDistancePoints**: distancia de parada alternativa en puntos cuando no se proporciona un precio de parada absoluto.
- **TakeProfitDistancePoints** – distancia opcional de obtención de beneficios en puntos. Cuando es cero, la estrategia utiliza un paso de la cuadrícula como objetivo predeterminado.
- **UseRiskSizing / RiskPercent**: habilite el tamaño basado en porcentaje y defina la porción de capital asignada a cada cuadrícula direccional. El valor se divide equitativamente en todos los niveles de esa dirección.
- **FixedOrderVolume**: tamaño del pedido que se utiliza cuando el dimensionamiento basado en riesgos está deshabilitado o no logra producir un volumen válido.
- **ExpirationHours**: vida útil opcional para pedidos de cuadrícula pendientes.
- **DailyLossLimitPercent**: umbral de parada de negociación expresado como una fracción del capital de la cartera capturado al inicio del día de negociación.
- **CloseAllPositions / CloseLongPositions / CloseShortPositions / CancelOrders**: comandos de mantenimiento manual accesibles desde la interfaz de usuario.
- **CandleType**: serie de velas utilizadas para eventos de mantenimiento, como reinicios diarios.

## Notas de implementación
- La estrategia se basa exclusivamente en StockSharp API de alto nivel: `SubscribeCandles`, `SubscribeOrderBook`, `BuyLimit`, `SellStop`, etc. No se requiere lógica de conector directo ni acceso al indicador.
- El tamaño de la orden utiliza `Security.PriceStep` y `Security.StepPrice` para traducir distancias basadas en puntos del script MQL en riesgo monetario.
- Las salidas de protección se implementan mediante órdenes de parada/límite separadas en lugar de modificar la orden de entrada original, lo que coincide con la forma en que StockSharp maneja las órdenes de protección vinculadas.
- El filtro de pérdida diaria se reinicia cuando cambia el día calendario y se registra nuevamente el valor de la cartera. Los operadores pueden reanudar las operaciones manualmente alternando `EnableGrid` si desean anular el bloqueo de seguridad.
- Las variables globales de MT4, los indicadores de cierre de emergencia y las rutinas de limpieza gráfica del script fuente fueron reemplazados por parámetros fuertemente tipados y alternancias manuales.

## Consejos de uso
1. Defina si la grilla debe estar centrada o anclada a precios específicos antes de habilitarla. Para cuadrículas centradas, proporcione un `CenterPrice` significativo; para rejillas ancladas déjelo deshabilitado y complete los precios de entrada de compra/venta.
2. Calibre `LevelSpacingPoints`, `StopDistancePoints` y `TakeProfitDistancePoints` para que coincidan con la volatilidad del instrumento. Recuerde que los tres son valores basados ​​en puntos.
3. Cuando utilice un dimensionamiento basado en el riesgo, verifique que el instrumento tenga `PriceStep` y `StepPrice` válidos; de lo contrario, la estrategia volverá al volumen fijo.
4. Utilice los parámetros de control manual para cancelar o aplanar posiciones rápidamente antes de modificar los parámetros de configuración.
5. Combina el límite de pérdidas diario con la gestión de riesgos externa si varias estrategias comparten la misma cartera.

## Diferencias frente al Expert Advisor original
- La versión StockSharp se centra en una interfaz de parámetros limpia en lugar de variables globales MT4 y lógica de números mágicos basada en comentarios.
- Los indicadores de cierre de emergencia, los ajustes automáticos del tamaño de la cuadrícula y la limpieza de objetos gráficos del código original se reducen a alternancia manual y validación de parámetros sencilla.
- Los ayudantes de trailing stop del script MQL no se replican; utilice los módulos finales existentes de StockSharp si es necesario.
- La lógica de dependencia MQL entre órdenes (ejecutar/cancelar basada en órdenes "madre") no se reproduce. Cada nivel opera de forma independiente con sus propias órdenes de protección.

Estos ajustes mantienen el espíritu del asesor experto original de Colibri (entradas estructuradas de varios niveles con una estricta administración del dinero) al tiempo que alinean la implementación con patrones idiomáticos StockSharp.
