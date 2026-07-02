# Parabolic SAR Límite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Parabolic SAR El límite es un puerto directo del asesor experto MT4 **ytg_Parabolic_exp.mq4**. El sistema mantiene continuamente las órdenes limitadas de compra y venta pegadas al valor Parabolic SAR y permite que el mercado introduzca la orden en una operación. Una vez completada, la estrategia supervisa la posición abierta y realiza salidas de stop-loss o take-profit utilizando velas extremas, reflejando el comportamiento original de MQL.

## Lógica de la estrategia

1. La estrategia se suscribe a una serie de velas configurables (período de tiempo de 4 horas de forma predeterminada) y calcula el indicador Parabolic SAR con el mismo paso y valores máximos que el script MT4.
2. En cada vela terminada:
   - Si el punto SAR está *debajo* del mínimo de la barra y la mejor oferta está al menos `MinOrderDistancePoints` por encima del precio SAR, se coloca (o realinea) una orden de límite de compra exactamente al valor SAR.
   - Si el punto SAR está *arriba* del máximo de la barra y la mejor demanda está al menos `MinOrderDistancePoints` por debajo del precio SAR, se coloca (o realinea) una orden de límite de venta a ese precio SAR.
   - Sólo se mantiene una orden pendiente por lado. Cuando el SAR se mueve, la orden pendiente activa se cancela y se envía una nueva en el nivel actualizado.
3. Cuando se ejecuta una orden pendiente, las distancias de stop-loss y take-profit (expresadas en puntos) se convierten a precios absolutos utilizando el paso del precio del valor. Esos niveles se almacenan como límites protectores virtuales.
4. Cada nueva vela comprueba los límites registrados. Si el rango de velas toca el nivel de stop o take, la estrategia cierra inmediatamente la posición correspondiente y restablece el estado de protección.

## Parámetros

- **CandleType** – período de tiempo para velas de señal. El valor predeterminado es velas de 4 horas para coincidir con el parámetro de entrada MT4 `timeframe`.
- **SarStep** – Parabolic SAR factor de aceleración (`step` en MT4). Controla la rapidez con la que SAR alcanza el precio.
- **SarMaximum** – aceleración máxima (`maximum` en MT4). Limita la velocidad SAR.
- **StopLossPoints** – distancia en puntos entre el precio de entrada y el nivel de stop. Establezca en `0` para desactivar.
- **TakeProfitPoints** – distancia en puntos entre el precio de entrada y el nivel de obtención de beneficios. Establezca en `0` para desactivar.
- **MinOrderDistancePoints**: imita `MODE_STOPLEVEL` en MT4. Las órdenes pendientes se envían solo si el precio de mercado está más lejos que esta distancia del valor SAR.
- **OrderVolume** – lotes (volumen) para cada orden pendiente. Alinéelo con el `VolumeStep` del instrumento.

Todas las distancias basadas en puntos se convierten a precios utilizando el instrumento `PriceStep`, por lo que el comportamiento se mantiene constante en todos los mercados.

## Comportamiento comercial

- Funciona en ambas direcciones simultáneamente: una orden límite de compra y venta puede coexistir si el SAR cambia el precio.
- Las órdenes pendientes siempre están alineadas con la última lectura de SAR; Los pedidos obsoletos se cancelan antes de que se registre uno nuevo.
- Las salidas de stop-loss y take-profit se manejan virtualmente a través de máximos y mínimos de velas, porque las estrategias StockSharp de alto nivel no vinculan SL/TP directamente a las órdenes pendientes.
- La estrategia se basa en los mejores datos de oferta/demanda cuando están disponibles; de lo contrario, el precio de cierre de la vela se utiliza como alternativa para evaluar las condiciones de distancia.

## Notas de portabilidad

- `MinOrderDistancePoints` tiene como valor predeterminado `0`, pero puede establecerlo en el nivel de parada del corredor si el centro de negociación exige una distancia mínima.
- Los niveles de protección se restablecen automáticamente cuando se cierra la posición o cuando se cancela la orden pendiente, manteniendo la lógica idéntica a la del experto MT4.
- Los comentarios dentro del código C# explican el uso de alto nivel de API, el enlace del indicador y el ciclo de vida del pedido para facilitar el mantenimiento.

## Consejos de uso

- Proporcionar cotizaciones de Nivel 1 para una verificación precisa de la distancia; de lo contrario, asegúrese de que el precio de cierre de la vela sea un buen indicador del precio de mercado actual.
- Revise los `PriceStep` y `VolumeStep` de su símbolo para que las distancias de los puntos y el volumen del pedido se conviertan en precios y cantidades válidos.
- Debido a que las salidas se evalúan en velas completadas, considere usar períodos de tiempo más cortos si necesita una granularidad más fina para el monitoreo de stop-loss.
