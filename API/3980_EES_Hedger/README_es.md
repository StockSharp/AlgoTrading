# Cobertura EES (avanzada)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia refleja el comportamiento del clásico asesor experto MetaTrader "EES Hedger". Cada vez que un operador externo, un operador discrecional u otro sistema automatizado abre una posición en la misma cuenta, la estrategia crea inmediatamente una cobertura opuesta utilizando un volumen configurable. Luego gestiona la cobertura con reglas de límite de pérdidas, toma de ganancias, punto de equilibrio y límite dinámico para neutralizar la exposición mientras se protegen las ganancias de la cobertura.

A diferencia de las estrategias tradicionales basadas en señales, este módulo supone que las entradas se producen en otro lugar. Su única responsabilidad es observar las operaciones de la cuenta, reaccionar a los tickets coincidentes y proteger la posición de cobertura hasta que se cierre mediante órdenes de protección o manualmente.

## Lógica comercial

1. **Detección de transacciones externas**: se monitorea el flujo del conector de las transacciones de la cuenta. Las operaciones cuyo comentario coincida con `OriginalOrderComment` (o todas las operaciones cuando el campo esté vacío) se tratan como la fuente que debe cubrirse. Las operaciones producidas por la propia estrategia se filtran almacenando sus identificadores de transacción.
2. **Órdenes reflejadas**: una vez que se recibe una operación calificada, la estrategia envía una orden de mercado inmediata en la dirección opuesta con un volumen `HedgeVolume`. Un `HedgerOrderComment` opcional ayuda a las herramientas administrativas a separar las órdenes de cobertura de otras actividades.
3. **Gestión de riesgos**: una vez completada la cobertura, la estrategia coloca órdenes de limitación de pérdidas y toma de ganancias a distancias definidas por los parámetros de pip. Cuando se cumplen las condiciones de equilibrio, el stop se mueve al precio de entrada más un pip. Si el seguimiento está habilitado, el stop avanza aún más a medida que el mercado continúa moviéndose a favor de la cobertura.
4. **Limpieza del estado**: cuando la posición llega a cero (por ejemplo, después de un cierre manual), todas las órdenes de protección se cancelan y los indicadores internos se restablecen para que la siguiente operación externa pueda cubrirse desde cero.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `HedgeVolume` | Volumen utilizado para abrir la posición de cobertura opuesta. |
| `StopLossPips` | Distancia desde el precio de entrada hasta la orden protectora de stop-loss. |
| `TakeProfitPips` | Distancia desde el precio de entrada hasta la orden de toma de beneficios. |
| `TrailingStopPips` | Distancia mantenida por el trailing stop una vez superado el umbral de activación. Establezca en cero para desactivar el seguimiento. |
| `TrailingActivationPips` | Beneficio mínimo (en pips) requerido antes de que el trailing stop comience a moverse. |
| `BreakEvenPips` | Umbral de beneficio (en pips) tras el cual el stop-loss se mueve al precio de entrada más un pip. |
| `OriginalOrderComment` | Filtro de comentarios opcional que selecciona qué operaciones externas deben cubrirse. Déjelo vacío para cubrir todas las operaciones con el instrumento. |
| `HedgerOrderComment` | Comentario adjunto a las órdenes de cobertura y stop de protección generados por la estrategia. |

## Notas practicas

- Asigne la misma cartera/cuenta a la estrategia que el operador externo. Todas las posiciones creadas en esa cuenta serán visibles para el conector y, por lo tanto, podrán cubrirse.
- Cuando se utiliza con puentes MetaTrader, configure el asesor experto o el puente para copiar el comentario del pedido original para que el filtrado funcione como se esperaba.
- El tamaño del pip se deriva del paso del precio del instrumento. Para símbolos FX de cinco dígitos, la distancia traduce automáticamente los valores de pip especificados en compensaciones de precios correctas.
- La lógica de equilibrio y de seguimiento nunca aleja el stop más del precio de entrada. Solo se aplican mejoras, garantizando que una vez que se alcanza el punto de equilibrio, el stop nunca vuelva a un nivel de pérdidas.
- La estrategia no gestiona la posición original. Cerrarlo o modificarlo sigue siendo responsabilidad del sistema de comercio primario.

## Flujo de trabajo de uso

1. Configura los parámetros de la estrategia, prestando especial atención a los filtros de comentarios y al volumen de la cobertura.
2. Inicie la estrategia y confirme que esté conectada al feed del corredor. Permanecerá inactivo hasta que llegue un comercio exterior.
3. Tan pronto como aparezca una operación calificada, observe cómo se crea la orden de cobertura y cómo se colocan las órdenes de protección en el DOM.
4. Supervise el comportamiento de equilibrio y seguimiento para garantizar que las distancias de pips configuradas coincidan con las especificaciones del contrato del corredor.
5. Detenga la estrategia cuando ya no sea necesaria la cobertura. Todas las órdenes de protección de trabajo se cancelan durante el cierre.

## Limitaciones

- El módulo asume acceso al flujo comercial de la cuenta. No puede cubrir operaciones que sean completamente invisibles para el conector.
- Las reglas de redondeo de volumen son específicas de cada corredor. Asegúrese de que el `HedgeVolume` configurado sea compatible con el paso de lote del instrumento.
- Debido a que la estrategia coloca órdenes de mercado inmediatamente, el deslizamiento en los mercados rápidos puede resultar en coberturas imperfectas. Aumente las distancias de stop-loss para tener en cuenta esto cuando sea necesario.
