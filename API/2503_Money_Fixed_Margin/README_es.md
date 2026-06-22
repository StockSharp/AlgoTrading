# Estrategia de Margen Fijo de Dinero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el ejemplo de MetaTrader "Money Fixed Margin" utilizando la API de alto nivel de StockSharp. Muestra cómo dimensionar posiciones arriesgando un porcentaje fijo del portafolio mientras convierte la distancia del stop-loss expresada en pips a un desplazamiento de precio absoluto. La estrategia solo opera posiciones largas y se enfoca en demostrar la lógica de gestión del dinero en lugar de una señal de entrada predictiva.

## Detalles

- **Criterios de entrada**:
  - **Largo**: ejecuta una compra a mercado después de cada recuento de velas completado especificado por `Check Interval` (por defecto cada 980ª barra). La orden utiliza el precio de cierre de la vela desencadenante como referencia para los cálculos de riesgo.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - El stop-loss protector se adjunta automáticamente mediante `StartProtection` a una distancia derivada del parámetro `Stop Loss (pips)`.
  - No se utiliza objetivo de beneficio; las posiciones cierran únicamente por el stop-loss o intervención manual.
- **Stops**: Solo Stop Loss.
- **Valores predeterminados**:
  - `Stop Loss (pips)` = 25
  - `Risk Percent` = 10
  - `Check Interval` = 980
  - `Candle Type` = marco temporal de 1 minuto
- **Filtros**:
  - Categoría: Gestión de riesgos
  - Dirección: Largo
  - Indicadores: Ninguno
  - Stops: Sí (stop-loss)
  - Complejidad: Básico
  - Marco temporal: Intradía (configurable a través de `Candle Type`)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio (escala con `Risk Percent`)

## Lógica de Dimensionamiento de Posición

1. La estrategia lee `Security.PriceStep` y `Security.Decimals` para inferir el tamaño del pip. Los pares con 3 o 5 decimales usan un multiplicador décuplo para coincidir con la definición de pip de MetaTrader.
2. `Stop Loss (pips)` se multiplica por el tamaño del pip para obtener una distancia de precio absoluta (`ExtStopLoss`) idéntica al código MQL5.
3. El valor actual del portafolio (prefiriendo `Portfolio.CurrentValue` luego `Portfolio.BeginValue`) se multiplica por `Risk Percent / 100` para determinar el capital expuesto por operación.
4. El riesgo por lote individual se calcula mediante el producto de la distancia del stop-loss, el número de pasos de precio dentro de esa distancia y `Security.StepPrice` cuando esté disponible. Si `StepPrice` es desconocido, la distancia de precio en sí se usa como respaldo.
5. Dividir el monto de riesgo por el riesgo por lote produce el volumen deseado. El resultado se normaliza al `VolumeStep` del instrumento, se limita a los límites mínimos y máximos de volumen, y se registra para transparencia. También se registra un valor de comparación con distancia de stop-loss cero para ilustrar por qué el gestor de dinero rechaza operaciones sin un stop protector.

## Flujo de Trabajo

1. Al iniciar, la estrategia se suscribe a la serie de velas configurada, calcula el tamaño del pip y habilita `StartProtection` con la distancia de stop-loss absoluta calculada.
2. Cada vela completada incrementa un contador interno. Cuando el contador alcanza el `Check Interval` elegido, la estrategia evalúa el tamaño de la posición, imprime información de diagnóstico y restablece el contador.
3. Si el volumen calculado es positivo, se coloca una orden de compra a mercado. La protección incorporada adjunta el stop-loss en `Close - ExtStopLoss`. Cualquier error (por ejemplo, por datos insuficientes o instrumentos con precio cero) impide el envío de la orden.
4. No se realizan más operaciones hasta que el contador complete otro intervalo, manteniendo el enfoque en la gestión del dinero en lugar de la frecuencia de señales.

## Notas de Uso

- Establezca `Risk Percent` en un valor conservador al conectarse a una cuenta en vivo; el riesgo predeterminado del 10% refleja el ejemplo MQL pero es agresivo para el trading real.
- Asegúrese de que el instrumento proporcione metadatos significativos de `PriceStep` y `StepPrice`. Cuando no estén disponibles, la estrategia sigue operando pero interpreta el riesgo en unidades de precio bruto.
- La estrategia evita intencionalmente las operaciones cortas para mantenerse fiel a la demostración original. Adapte las llamadas `BuyMarket`/`SellMarket` si se desea trading bilateral.
- Combine este módulo de gestión del dinero con otros generadores de señales reutilizando el helper `CalculateFixedMarginVolume` del código de la estrategia.
