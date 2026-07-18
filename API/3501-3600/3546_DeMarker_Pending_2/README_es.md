# DeMarker Pendiente 2 Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia replica la lógica central del experto MetaTrader "DeMarker Pending 2" utilizando la API de alto nivel de StockSharp. Evalúa un oscilador DeMarker en el marco de tiempo de trabajo y prepara entradas de compra o venta pendientes cuando el indicador cruza umbrales configurables. Las órdenes se pueden crear como solicitudes de parada o límite con una sangría adicional del precio de mercado actual. Un filtro de sesión, una guardia de propagación y controles de distancia mantienen las nuevas entradas bajo control.

## Lógica de trading

1. Suscríbase a la serie de velas configuradas y calcule el indicador DeMarker con el período seleccionado.
2. Cuando el valor anterior está por encima del nivel inferior y el valor actual cruza por debajo de él, se pone en cola una orden pendiente larga. Cuando el valor anterior está por debajo del nivel superior y el valor actual cruza por encima de él, pone en cola una orden pendiente corta. Sólo se procesa una señal por vela.
3. Las órdenes pendientes se colocan como órdenes stop o limitadas utilizando la distancia de sangría expresada en puntos. Los pedidos existentes se pueden cancelar antes de la nueva solicitud si la opción de reemplazo está habilitada. La estrategia limita el número total de posiciones abiertas más órdenes pendientes y exige una distancia mínima desde el precio promedio actual de la posición.
4. Las posiciones largas y cortas utilizan lógica opcional de stop-loss, take-profit y trailing. Los niveles de protección se calculan en puntos de precio y se monitorean en cada vela cerrada. Los topes dinámicos se ajustan una vez que la posición obtiene la ganancia de activación y la distancia adicional del paso final.
5. Un filtro de diferencial evita nuevos pedidos si el mejor diferencial de oferta/demanda excede el umbral configurado. Los límites de sesión opcionales pueden deshabilitar nuevas entradas fuera de la ventana comercial permitida.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| Velas de trabajo | Plazo utilizado para señales y controles de protección. |
| Volumen de pedido | Volumen predeterminado para órdenes pendientes. |
| Stop Loss (pts) | Distancia inicial de stop-loss en puntos de precio. |
| Tomar ganancias (pts) | Distancia inicial de obtención de beneficios en puntos de precio. |
| Activación final (pts) | Beneficio necesario antes de que se active el trailing stop. |
| Trailing Stop (pts) | Distancia entre el precio y el trailing stop. |
| Paso final (pts) | Se requiere ganancia adicional para mover el trailing stop. |
| Sendero en cierre | Actualice el trailing stop solo en velas terminadas cuando esté habilitado. |
| Posiciones máximas | Número máximo de posiciones abiertas más órdenes pendientes. Zero desactiva el límite. |
| Distancia mínima (pts) | Distancia mínima desde el precio de la posición actual hasta nuevas entradas pendientes. |
| Utilice órdenes de parada | Coloque órdenes stop cuando sean verdaderas; de lo contrario, se utilizan órdenes limitadas. |
| Único Pendiente | Permita solo una orden pendiente activa a la vez. |
| Reemplazar Pendientes | Cancele las órdenes pendientes pendientes antes de realizar una nueva. |
| Compensación pendiente (pts) | Sangría para nuevos precios pendientes en relación con el mercado. |
| Spread máximo (pts) | Spread máximo permitido antes de omitir la colocación de la orden. |
| Usar filtro de sesión | Habilite o deshabilite el filtro de la ventana comercial. |
| Hora/minuto de inicio, Hora/minuto de finalización | Límites de la sesión cuando el filtro de sesión está activo. |
| Período de demarcación | Período de promediación del oscilador DeMarker. |
| Nivel superior | Umbral que desencadena configuraciones cortas. |
| Nivel inferior | Umbral que desencadena configuraciones largas. |

## Notas

* El vencimiento de la orden y el tamaño del riesgo de administración del dinero del experto original no se transfieren. En su lugar, se utiliza un parámetro de volumen fijo.
* Los niveles de stop-loss y take-profit se evalúan en velas cerradas utilizando precios altos/bajos, que pueden diferir de la ejecución intrabar en MetaTrader.
* La lógica de seguimiento se evalúa únicamente en velas cerradas. El seguimiento basado en ticks en tiempo real no se reproduce.
* Las órdenes pendientes se basan en las mejores cotizaciones de oferta y demanda proporcionadas por la fuente de datos. Asegúrese de que las suscripciones de nivel 1 estén disponibles.
