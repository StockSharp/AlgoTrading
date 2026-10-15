# Estrategia de calculadora de volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de calculadora de volumen** reproduce la lógica del asesor experto original MetaTrader que calcula un volumen comercial recomendado en función de los niveles de límite de pérdidas y obtención de ganancias. Cuando se inicia la estrategia, lee los precios de parada configurados, evalúa el precio de mercado actual del valor seleccionado y deriva las métricas de riesgo utilizando el capital de cartera disponible.

La estrategia no realiza ningún pedido. Su único propósito es proporcionar estadísticas detalladas de administración de dinero en el registro y exponer los valores calculados a través de propiedades de solo lectura. Esto lo hace útil para los operadores manuales que desean validar las reglas de tamaño de posición antes de enviar una operación.

## Parámetros
- **Precio Stop Loss** – nivel de precio absoluto del stop protector utilizado para la posición planificada.
- **Precio de obtención de beneficios**: nivel de precio absoluto del objetivo de obtención de beneficios.
- **% de pérdida máxima**: proporción máxima del valor de la cartera que se puede arriesgar en una sola operación. La estrategia multiplica este porcentaje por el capital de la cartera para obtener la pérdida máxima aceptable en términos de moneda.
- **Es posición larga**: determina si la posición planificada es larga (`true`) o corta (`false`). La dirección es necesaria para calcular la distancia entre el precio actual y los niveles de parada/objetivo.

Todos los parámetros excepto *Max Loss %* están excluidos de la optimización para mantenerlos como entradas estrictamente manuales, reflejando el comportamiento del experto original.

## Detalles del cálculo
1. **Valor de la cartera**: la estrategia recupera `Portfolio.CurrentValue` (regresando a `Portfolio.BeginValue`) para estimar el capital disponible. Si no se proporciona el valor, el cálculo se detiene con una advertencia.
2. **Validación de pasos de precios**: los valores `Security.PriceStep` y `Security.StepPrice` deben definirse porque convierten las distancias de precios en pasos de contrato y montos en efectivo. Los metadatos faltantes impiden el cálculo.
3. **Detección de precio actual**: la estrategia busca el último precio comercial. Cuando no está disponible, aproxima el precio promediando las mejores cotizaciones de oferta y demanda y finalmente vuelve al último precio conocido.
4. **Distancia en pasos**: tanto la distancia de parada de pérdidas como la de toma de ganancias se miden en pasos de precio. Las distancias se redondean hacia arriba (`decimal.Ceiling`) para ser conservadoras, de la misma manera que el script MetaTrader se basa en `MathCeil`.
5. **Dinero en riesgo**: la pérdida máxima aceptable equivale a `PortfolioValue * MaxLoss% / 100`.
6. **Volumen sugerido**: la pérdida por paso es `MaxLoss / StopSteps`. Dividir este valor por `StepPrice` produce el volumen de posición que mantiene la pérdida bajo control.
7. **Beneficio esperado**: multiplicar los pasos de obtención de beneficios por `StepPrice` y el volumen sugerido produce la ganancia de efectivo proyectada si se alcanza el objetivo.
8. **Relación riesgo-recompensa**: relación entre los recuentos de pasos de obtención de beneficios y de limitación de pérdidas, equivalente al cálculo original basado en pips.

Cada valor calculado se almacena dentro de la estrategia y se imprime en el registro con mensajes informativos en inglés. Si la relación riesgo-recompensa es mayor o igual a 3, la estrategia indica "Puedes operar"; de lo contrario, imprime una advertencia de que la operación es demasiado arriesgada.

## Flujo de trabajo de uso
1. Adjunte la estrategia a la seguridad y cartera deseadas en el entorno StockSharp.
2. Configure los precios de stop-loss y take-profit que coincidan con la operación manual planificada.
3. Establecer el porcentaje de riesgo aceptable y la dirección prevista.
4. Inicie la estrategia: el resultado con todas las métricas aparecerá inmediatamente en el registro.
5. Revise el volumen sugerido y la relación riesgo-recompensa antes de ejecutar la operación manualmente.

## Notas
- Si falta alguno de los campos de metadatos de seguridad requeridos (escalón de precio o precio de escalón), solicítelo al intercambio o ajuste la configuración de seguridad manualmente.
- El cálculo es estático; no se actualiza automáticamente después del inicio. Reinicie la estrategia si las condiciones del mercado o los parámetros de riesgo cambian.
- Debido a que la estrategia no envía órdenes, es seguro ejecutarla tanto en entornos de backtesting como en vivo únicamente con fines analíticos.
