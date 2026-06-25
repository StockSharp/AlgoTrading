# Estrategia Above Below MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Above Below MA replica el asesor experto de MetaTrader *Above Below MA (barabashkakvn's edition)*. Monitorea qué tan lejos se encuentran los precios actuales con respecto a una media móvil configurable y permite operaciones únicamente cuando el precio está en el lado "incorrecto" de la media por al menos una distancia definida, mientras que la propia media sigue la dirección anticipada. La lógica ha sido portada a la API de alto nivel de StockSharp y se ejecuta exclusivamente en velas completadas.

## Descripción general

- **Régimen de mercado**: Funciona mejor en instrumentos que frecuentemente retestean una media móvil antes de retomar la tendencia.
- **Instrumentos**: Cualquier instrumento compatible con su conexión StockSharp. Los pares de Forex se benefician más porque el script original medía la distancia en pips.
- **Marco temporal**: Ajustable mediante el parámetro *Candle Type* (marco temporal de 1 minuto por defecto).
- **Dirección de posición**: Se admiten operaciones largas y cortas, pero solo puede existir una posición neta en un momento dado.

## Lógica de la estrategia

1. Se calcula una media móvil sobre la serie de velas seleccionada. El método de promediado (SMA, EMA, SMMA, WMA), el precio aplicado (close, open, high, low, median, typical, weighted) y el desplazamiento hacia adelante replican las entradas de MetaTrader.
2. Se convierte la distancia mínima expresada en pips en un desplazamiento de precio real usando el `PriceStep` del instrumento. Si el broker no publica un paso de precio, el filtro de distancia se omite automáticamente.
3. En cada vela finalizada:
   - **Configuración larga**:
     - La apertura y el cierre de la vela deben estar al menos a la distancia configurada por debajo de la media móvil desplazada.
     - La media móvil debe estar subiendo comparada con la vela anterior.
   - **Configuración corta**:
     - La apertura y el cierre de la vela deben estar al menos a la distancia configurada por encima de la media móvil desplazada.
     - La media móvil debe estar bajando comparada con la vela anterior.
4. La estrategia cierra cualquier posición opuesta antes de enviar una nueva orden de mercado en la dirección de la señal. No se permite exposición simultánea larga/corta.

Todas las decisiones de trading se toman en velas completadas para evitar entradas repetidas dentro de una barra en formación. Las órdenes se ejecutan mediante `BuyMarket` o `SellMarket` con el volumen configurado.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `MaPeriod` | Longitud de la media móvil. Por defecto 6.
| `MaShift` | Número de velas para desplazar la media móvil hacia adelante. Un valor de 0 usa la barra actual, `n` usa el valor de hace `n` barras. Por defecto 0.
| `MaMethod` | Tipo de media móvil: `Simple`, `Exponential`, `Smoothed` o `Weighted`. Por defecto `Exponential`.
| `AppliedPrice` | Fuente de precio: close, open, high, low, median, typical o weighted. Por defecto `Typical`.
| `MinimumDistancePips` | Distancia requerida en pips entre los precios de la vela y la media móvil. Se convierte usando `PriceStep`. Por defecto 5.
| `CandleType` | Tipo de vela que impulsa las actualizaciones del indicador. Marco temporal de 1 minuto por defecto.
| `TradeVolume` | Volumen de la orden para nuevas entradas. Por defecto 1.

## Notas adicionales

- No se incluye lógica de stop-loss ni take-profit. La gestión del riesgo debe implementarse mediante la configuración de la cartera o módulos externos.
- El búfer de desplazamiento de la media móvil se mantiene mínimo y respeta la directriz de "sin colecciones" almacenando solo los valores requeridos para el desplazamiento especificado.
- Cuando `PriceStep` no está disponible, el filtro de distancia mínima no puede evaluarse, por lo que las entradas dependen únicamente de las condiciones de la media móvil.
- La estrategia dibuja la serie de velas, el indicador de media móvil y sus operaciones en el área del gráfico predeterminada cuando hay un contenedor de gráfico disponible.
