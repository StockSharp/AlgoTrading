# Estrategia de dinero promedio móvil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia es una conversión StockSharp del asesor experto MetaTrader "Moving Average Money". Evalúa velas completadas y reacciona cuando la barra anterior cruza una media móvil simple desplazada. El sistema admite operaciones tanto largas como cortas y mantiene cada decisión sincronizada con la suscripción de vela de alto nivel API.

## Lógica de trading
- A partir de los precios de cierre se calcula una media móvil simple con longitud configurable y desplazamiento visual.
- Sólo se procesan velas terminadas para evitar pedidos duplicados dentro de una barra.
- **Entrada corta:** cuando la vela anterior se abre por encima de la media móvil desplazada y cierra por debajo de ella.
- **Entrada larga:** cuando la vela anterior se abre por debajo de la media móvil desplazada y cierra por encima de ella.
- La estrategia no piramidaliza posiciones; cualquier exposición abierta en la dirección opuesta se cierra antes de establecer una nueva operación.

## Gestión del riesgo
- La distancia de stop-loss en unidades de precio se deriva de `MaximumRiskPercent`. El valor actual de la cartera, el escalón del precio del instrumento y el precio del escalón se utilizan para convertir el porcentaje de riesgo elegido en escalones de precio.
- El diferencial de oferta/demanda se resta de la distancia basada en el riesgo siempre que estén disponibles las mejores cotizaciones.
- Los niveles de obtención de beneficios se definen como `stopDistance * ProfitLossFactor`.
- Tanto el nivel de parada como el de objetivo se controlan en las velas completadas. Cuando se alcanza cualquiera de los niveles, la posición se aplana con una orden de mercado.

## Parámetros
- `CandleType`: período de tiempo utilizado para la detección de señales.
- `MovingPeriod` – longitud de la media móvil simple.
- `MovingShift`: número de velas completamente formadas utilizadas para desplazar la media móvil hacia la derecha.
- `MaximumRiskPercent`: porcentaje del valor actual de la cartera que define la pérdida máxima por operación.
- `ProfitLossFactor`: multiplicador aplicado a la distancia de parada para calcular la distancia de obtención de beneficios.
- `TradeVolume`: volumen de pedido base para nuevas entradas (las restricciones de paso de volumen se respetan automáticamente).

## Notas de implementación
- La estrategia realiza un seguimiento de las posiciones abiertas a través de controladores de eventos de alto nivel (`OnOwnTradeReceived`) para reinicializar paradas y objetivos después de completarse.
- Si los datos del mercado carecen de cotizaciones o valoración de la cartera, se omiten nuevas entradas para evitar órdenes sin un control de riesgo adecuado.
- El cambio de media móvil se emula con un búfer interno para que la lógica coincida con la versión MetaTrader.
