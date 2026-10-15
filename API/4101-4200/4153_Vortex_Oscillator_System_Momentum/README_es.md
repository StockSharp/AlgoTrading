# Sistema oscilador de vórtice 4153
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia reproduce el experto MetaTrader 4 "Vortex Oscillator System" utilizando la estrategia de alto nivel de StockSharp API. Deriva un oscilador Vortex normalizado combinando los componentes estándar del indicador Vortex y reacciona cada vez que el impulso escapa de una banda neutral configurable. El algoritmo opera con un solo símbolo y siempre funciona con posiciones completamente cerradas o invertidas.

## Reglas comerciales
- Una suscripción de vela definida por **CandleType** alimenta un indicador Vortex con un período **VortexLength**. El oscilador se calcula como `(VI+ - VI-) / (VI+ + VI-)`, lo que mantiene las lecturas en el rango `[-1, 1]`.
- Una configuración larga se activa cuando el oscilador cae por debajo de **BuyThreshold** y, si **UseBuyStopLoss** está habilitado, permanece por encima de **BuyStopLossLevel**. Se activa una configuración corta cuando el oscilador se eleva por encima de **SellThreshold** y, si **UseSellStopLoss** está habilitado, permanece por debajo de **SellStopLossLevel**.
- Siempre que el oscilador regresa dentro de la banda neutral delimitada por **BuyThreshold** y **SellThreshold**, ambas configuraciones se borran, por lo que la siguiente ruptura debe ocurrir desde un estado neutral.
- Si una configuración larga está activa mientras la posición actual es plana o corta, la estrategia envía una compra de mercado para lotes de **Volumen** más cualquier cantidad necesaria para cubrir una posición corta existente. Las configuraciones cortas reflejan ese comportamiento al vender lotes de **volumen** más la cantidad larga pendiente.
- Las posiciones abiertas se pueden cerrar sin una configuración opuesta: si **UseBuyStopLoss** está habilitado y el oscilador toca **BuyStopLossLevel**, la operación larga se liquida; **UseBuyTakeProfit** sale de una posición larga una vez que el oscilador excede **BuyTakeProfitLevel**. Los controles equivalentes que utilizan **SellStopLossLevel** y **SellTakeProfitLevel** administran posiciones cortas cuando sus respectivos conmutadores están habilitados.

## Parámetros
- **VortexLength**: número de velas utilizadas para calcular los valores VI+ y VI-.
- **CandleType**: período de tiempo o tipo de datos solicitados de la fuente de datos del mercado.
- **Volumen**: tamaño de pedido base para nuevas entradas; Las órdenes de reversión agregan automáticamente la cantidad necesaria para aplanar la posición actual.
- **BuyThreshold**: nivel del oscilador que arma una configuración larga una vez superado.
- **UseBuyStopLoss**: requiere que el oscilador permanezca por encima de **BuyStopLossLevel** antes de que se pueda activar una entrada larga.
- **BuyStopLossLevel**: nivel del oscilador que cierra inmediatamente una posición larga cuando el filtro de parada está habilitado.
- **UseBuyTakeProfit**: alterna la obtención de beneficios basada en el oscilador para operaciones largas.
- **BuyTakeProfitLevel**: nivel del oscilador que obtiene ganancias en posiciones largas cuando el filtro de toma de ganancias está activo.
- **SellThreshold**: nivel del oscilador que arma una configuración corta una vez superado.
- **UseSellStopLoss**: requiere que el oscilador permanezca por debajo de **SellStopLossLevel** antes de que se pueda activar una entrada corta.
- **SellStopLossLevel**: nivel del oscilador que cierra inmediatamente una posición corta cuando el filtro de parada está habilitado.
- **UseSellTakeProfit**: alterna la obtención de beneficios basada en el oscilador para operaciones cortas.
- **SellTakeProfitLevel**: nivel del oscilador que obtiene ganancias en posiciones cortas cuando el filtro de toma de ganancias está activo.

## Notas adicionales
- La estrategia dibuja velas y ejecuta operaciones en el gráfico automáticamente; la lógica del oscilador interno no agrega paneles personalizados.
- Debido a que el oscilador está normalizado, los umbrales predeterminados `-0.75`, `0.75`, `-1.00` y `1.00` se traducen directamente del asesor experto original y se pueden optimizar utilizando el sistema de parámetros de StockSharp.
- La lógica nunca mantiene posiciones largas y cortas simultáneas; cada reversión cierra primero la exposición actual y luego abre el lado opuesto en una única orden de mercado.
