# Robot comercial AIS1 (MQL/8700 Conversión)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El **Robot comercial AIS1** es una conversión directa de C# del MetaTrader 4 asesor experto de `MQL/8700/AIS1.MQ4`. El sistema original está diseñado para las rupturas diarias del EURUSD y utiliza rangos de múltiples períodos de tiempo para los cálculos de stop, target y trailing. Esta implementación StockSharp preserva la estructura del robot heredado al tiempo que expone cada elemento configurable como parámetros de estrategia.

## Lógica de trading
- **Plazos**
  - Velas primarias: barras de 1 día para condiciones de entrada, distancias de stop loss y toma de ganancias.
  - Velas secundarias: barras de 4 horas para cálculos dinámicos de trailing stop.
- **Condiciones de entrada**
  - Ruptura larga: el cierre diario de ayer está por encima del punto medio de la barra y la demanda actual perfora el máximo diario anterior.
  - Ruptura corta: el cierre diario de ayer está por debajo del punto medio y la oferta actual cae por debajo del mínimo diario anterior.
  - Sólo puede haber una posición abierta a la vez; las señales opuestas se ignoran hasta que se cierra la operación actual.
- **Riesgo y recompensa inicial**
  - Stop loss = máximo/mínimo diario anterior ± `StopFactor × daily range`.
  - Take Profit = precio de entrada ± `TakeFactor × daily range`.
  - Ambos niveles se validan con el `StopBufferTicks` opcional para respetar las restricciones de distancia de parada del corredor.
- **Parada final**
  - Utiliza el rango de la última vela de 4 horas multiplicado por `TrailFactor`.
  - Las actualizaciones finales requieren que el precio se mueva al menos `TrailStepMultiplier × spread` más allá del tope existente y se mantenga alejado del objetivo en el búfer configurado.
  - La protección contra retiros desactiva las actualizaciones finales cuando el capital cae por debajo del umbral de reserva.
- **Gestión de riesgos**
  - El tamaño del lote se deriva de `OrderReserve × equity` dividido por el riesgo monetario entre la entrada y la parada.
  - Los volúmenes están sujetos a los límites de intercambio (`MinVolume`, `MaxVolume`, `VolumeStep`).
  - El monitoreo del capital realiza un seguimiento del máximo actual y bloquea nuevas entradas una vez que el capital cae por debajo de `AccountReserve - OrderReserve` de ese pico.
- **Protección de tiempo**
  - Las acciones (entradas o actualizaciones finales) están separadas por una pausa obligatoria de cinco segundos, replicando el acelerador EA original.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `AccountReserve` | 0,20 | Fracción del patrimonio que debe permanecer intacta. Se utiliza para calcular la reducción permitida. |
| `OrderReserve` | 0,04 | Fracción de capital asignada a cada operación y base para el tamaño de la posición. |
| `PrimaryCandleType` | Diariamente | Tipo de vela utilizado para lógica de ruptura y objetivos estáticos. |
| `SecondaryCandleType` | 4 horas | Tipo de vela utilizado para derivar distancias de seguimiento. |
| `TakeFactor` | 0,8 | Multiplicador del rango diario aplicado para tomar ganancias. |
| `StopFactor` | 1.0 | Multiplicador del rango diario aplicado al stop loss. |
| `TrailFactor` | 5.0 | Multiplicador del rango de 4 horas aplicado a los trailingstops. |
| `TrailStepMultiplier` | 1.0 | Multiplicador de diferencial que controla cuánto debe avanzar el precio antes de que se establezca un nuevo trailing stop. |
| `StopBufferTicks` | 0 | Se agregaron pasos de precios adicionales como márgenes de seguridad alrededor de paradas y objetivos. |

## Notas de uso
1. Asigne el **valor** deseado (EURUSD por defecto) y la **cartera** antes de iniciar la estrategia.
2. Asegúrese de que tanto la fuente de velas diaria como la de 4 horas estén disponibles; de lo contrario, los módulos de ruptura y seguimiento no se pueden activar.
3. La estrategia se suscribe al libro de órdenes para obtener los precios de oferta y demanda actuales. En los mercados sin alimentación en profundidad, el último precio negociado se utiliza como alternativa.
4. Las salidas de posición se realizan mediante órdenes de mercado cuando se cumplen las condiciones de parada o objetivo, coincidiendo con el comportamiento del MetaTrader EA que modificó las órdenes de protección en el lado del servidor.
5. El limitador de reducción, el temporizador de pausa y la lógica de dimensionamiento del riesgo se pueden ajustar a través de los parámetros expuestos para adaptar el robot a diferentes corredores o especificaciones de contrato.

## Diferencias vs. Original MQL
- Las paradas y objetivos de protección se emulan mediante cierres de posición manuales cuando los precios cruzan los niveles almacenados (MT4 manejó esto mediante la modificación de la orden).
- La conversión de riesgo se basa en `PriceStep` y `StepPrice` del objeto `Security`. Cuando faltan dichos metadatos, el código recurre a una conversión monetaria 1:1, por lo que los usuarios deben verificar las especificaciones del contrato.
- Se agregaron comentarios extensos y descripciones de parámetros para mayor claridad y para una mejor integración con las herramientas de optimización de StockSharp.

## Requisitos
- StockSharp API de alto nivel con acceso a suscripciones de velas y datos del libro de pedidos.
- Conexión comercial correctamente configurada para la colocación de pedidos y valoración de carteras.
