# Estrategia de reequilibrio de la red
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de reequilibrio de Grid es una versión StockSharp de alto nivel del asesor experto "Grid" de Mission Automate. La estrategia alterna entre ciclos de grilla largos y cortos y siempre mantiene una escalera de órdenes límite en la dirección activa. Una vez que la posición agregada alcanza un nivel común de toma de ganancias, el ciclo se cierra, se eliminan todas las órdenes pendientes y el siguiente ciclo comienza en la dirección opuesta.

## como funciona
1. **Inicio del ciclo** – Cuando no hay posiciones ni órdenes pendientes, la estrategia abre una posición de mercado en la dirección definida por `FirstTradeSide` usando `StartVolume` lotes.
2. **Colocación de la cuadrícula**: después de cada orden ejecutada en la dirección activa, el algoritmo coloca una nueva orden límite a una distancia de `GridStepPoints` (convertida en precio por el instrumento `PriceStep`). El volumen del siguiente pedido es igual al volumen del último pedido ejecutado multiplicado por `LotMultiplier`.
3. **Obtención de ganancias basada en el promedio**: para cada orden ejecutada, se recalcula el precio de entrada promedio ponderado. La obtención de beneficios para toda la cesta se establece en el precio medio más/menos `TargetPoints` (también convertido mediante `PriceStep`). Los máximos y mínimos de las velas se utilizan para modelar el comportamiento de activación del corredor.
4. **Finalización del ciclo**: cuando se alcanza el nivel de obtención de beneficios, la estrategia cierra toda la posición con una orden de mercado, cancela las órdenes pendientes restantes, recuerda la dirección del ciclo finalizado y cambia la dirección del siguiente.

## Parámetros
- `FirstTradeSide` – dirección del primer ciclo (`Buy` o `Sell`). Cada ciclo completado cambia automáticamente la dirección.
- `StartVolume` – tamaño de lote de la orden de mercado inicial en cada ciclo.
- `LotMultiplier`: multiplicador aplicado al volumen de pedido ejecutado más reciente al preparar el siguiente nivel de la cuadrícula. Los valores mayores que uno crean una progresión similar a una martingala.
- `GridStepPoints` – distancia entre niveles de la cuadrícula expresada en puntos. La estrategia lo multiplica por `Security.PriceStep` para obtener la diferencia de precio absoluta.
- `TargetPoints` – distancia de obtención de beneficios desde el precio de entrada medio ponderado, medida en puntos.
- `CandleType`: serie de velas utilizadas para monitorear los extremos de precios para activar salidas.

## Gestión de riesgos y comportamiento.
- No se utiliza ningún límite de pérdidas explícito; la red sigue agregando exposición mientras el mercado se mueve en contra de la posición.
- Sólo hay una orden pendiente activa a la vez. Cuando se completa el pedido, se programa inmediatamente el siguiente nivel.
- El ciclo no puede comenzar hasta que tanto la posición como la cola pendiente estén vacías y el instrumento tenga un `PriceStep` válido.
- La conversión mantiene todos los cálculos dentro de la estrategia sin tocar colecciones globales o buffers de indicadores, siguiendo las reglas del proyecto.
- Las órdenes pendientes se cancelan cada vez que finaliza un ciclo, lo que evita límites huérfanos de ciclos anteriores.

## Notas
- Todas las configuraciones basadas en puntos se convierten a precios con `Security.PriceStep`. Si el paso es cero la estrategia espera hasta que el instrumento lo proporcione.
- La implementación se basa exclusivamente en el nivel alto API (`SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket`, `BuyLimit`, `SellLimit`) según sea necesario.
- En esta tarea no se incluye intencionalmente una versión de Python.
