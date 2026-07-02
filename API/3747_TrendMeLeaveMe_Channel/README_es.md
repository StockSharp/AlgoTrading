# TrendMeLeaveMe Estrategia de canal pendiente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta implementación de StockSharp recrea el asesor experto original MetaTrader "TrendMeLeaveMe". La idea es seguir manualmente un canal de tendencia dinámico y utilizar órdenes stop pendientes para detectar rupturas cada vez que el precio se acerque a la línea de tendencia. Debido a que StockSharp no funciona con objetos de gráficos dibujados por el usuario, la estrategia reconstruye el centro del canal automáticamente con un indicador de regresión lineal y luego reproduce la misma lógica de compensación que la versión MQL aplicó a las líneas guía superior e inferior.

El enfoque está diseñado tanto para entradas largas como cortas. Una vez que se activa una orden de parada, la posición se protege inmediatamente con órdenes estáticas de parada de pérdidas y toma de ganancias que reflejan las distancias configuradas en EA. Las órdenes pendientes se actualizan constantemente para que los niveles de activación sigan el último valor de la línea de regresión.

## Cómo funciona la estrategia

1. Una suscripción de vela impulsa un indicador `LinearRegression` que actúa como línea de tendencia media.
2. El usuario define cuatro compensaciones (superior/inferior para escenarios de compra y venta) en los pasos del precio del instrumento. La estrategia los traduce en precios por encima o por debajo de la línea de regresión.
3. Cuando la última vela se cierra entre la línea de tendencia y el desplazamiento inferior configurado, se coloca una parada de compra en el desplazamiento superior. Simétricamente, cuando el precio cierra entre la línea y el desplazamiento superior, se coloca un stop de venta en el límite inferior.
4. Si el mercado se sale de esas zonas de activación, la orden pendiente correspondiente se cancela para que la estrategia no abarrote el libro.
5. Después de que se ejecuta una orden de parada, la operación se envuelve con un stop loss estático y una toma de ganancias que utiliza las mismas distancias de puntos que el asesor experto original.

## Señales

- **Configuración de compra**: el cierre de la vela está por debajo o igual a la línea de regresión, pero aún por encima de la compensación inferior de compra. Se coloca una orden stop de compra en el desplazamiento superior y sigue la línea mientras la condición sigue siendo válida.
- **Configuración de venta**: el cierre de la vela está por encima o igual a la línea de regresión, pero aún por debajo del desplazamiento superior de venta. Se coloca una orden de stop de venta en el desplazamiento inferior y sigue la línea de tendencia.
- **Sin configuración**: cuando el precio está fuera del corredor de activación, las órdenes pendientes existentes se eliminan.

## Gestión de riesgos

- Las operaciones de compra utilizan `BuyStopLossSteps` y `BuyTakeProfitSteps` para calcular los niveles fijos de stop-loss y take-profit a partir del precio de entrada.
- Las operaciones de venta utilizan `SellStopLossSteps` y `SellTakeProfitSteps` para el mismo propósito.
- Las órdenes de protección se recalculan solo cuando la posición neta cambia, imitando cómo MetaTrader adjunta niveles de parada directamente a cada orden pendiente.

## Parámetros

- `CandleType`: agregación de velas utilizada para calcular la línea de tendencia.
- `TrendLength` – número de velas en la ventana de regresión lineal.
- `BuyStepUpper` / `BuyStepLower`: compensaciones (en incrementos de precio) que definen el activador superior y el umbral de activación inferior para configuraciones largas.
- `SellStepUpper` / `SellStepLower`: compensaciones (en incrementos de precios) que definen el corredor de activación para configuraciones cortas.
- `BuyTakeProfitSteps` / `BuyStopLossSteps` – distancias para salidas de posiciones largas, expresadas en incrementos de precio.
- `SellTakeProfitSteps` / `SellStopLossSteps` – distancias para salidas de posiciones cortas.
- `BuyVolume` / `SellVolume`: volumen utilizado para órdenes pendientes en cada lado.

## Notas

- Como no hay líneas de tendencia manuales, el indicador de regresión reemplaza los objetos del gráfico de la estrategia MQL. Los usuarios pueden experimentar con la longitud de la regresión para aproximarse a su análisis de tendencias manual.
- La estrategia solo opera cuando la conexión de intercambio está activa (`IsFormedAndOnlineAndAllowTrading`).
- Las órdenes pendientes se cancelan automáticamente cuando ya existe una posición en la misma dirección, reproduciendo el comportamiento de orden única del EA original.
