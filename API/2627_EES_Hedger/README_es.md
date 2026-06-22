# Estrategia de Cobertura EES Hedger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia EES Hedger replica el comportamiento del asesor experto clásico de MetaTrader que cubre automáticamente posiciones creadas por otro sistema de trading o por traders manuales. Siempre que la cuenta monitorizada abre una posición que coincide con el filtro configurado, la estrategia abre inmediatamente una posición opuesta con sus propios parámetros. De este modo neutraliza la exposición direccional permitiendo que la operación original continúe.

El algoritmo está construido sobre la API de alto nivel de StockSharp. Escucha las operaciones de la cuenta, abre posiciones de cobertura y gestiona órdenes protectoras a través de lógica de stop-loss, take-profit y trailing stop. La gestión del trailing sigue de cerca la implementación original, avanzando el stop solo cuando el movimiento del precio supera tanto la distancia del stop como el incremento del trailing.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `HedgeVolume` | Volumen fijo para la orden de cobertura. No depende del tamaño de la operación externa. |
| `StopLossPips` | Distancia en pips para el stop-loss de protección de la cobertura. Establecer en cero para omitir el stop inicial. |
| `TakeProfitPips` | Distancia en pips para la orden de take-profit. Establecer en cero para omitir el objetivo. |
| `TrailingStopPips` | Distancia en pips utilizada para el trailing una vez que el precio se mueve favorablemente. |
| `TrailingStepPips` | Movimiento mínimo en pips requerido antes de mover nuevamente el trailing stop. Debe ser positivo cuando el trailing está activo. |
| `OriginalOrderComment` | Filtro de comentario opcional. Solo se cubrirán las operaciones cuyo comentario coincida con este valor (sin distinción de mayúsculas). Dejar vacío para reaccionar a todas las operaciones. |
| `HedgerOrderComment` | Comentario opcional usado para reconocer las propias operaciones de cobertura de la estrategia. Cuando se suministra, las operaciones con el mismo comentario se ignoran para evitar re-cobertura. |

## Comportamiento

1. **Detección de operaciones** – la estrategia se suscribe a los eventos `NewMyTrade` del conector. Cada operación que proviene del instrumento seleccionado y pasa los filtros de comentario se trata como una señal de entrada externa.
2. **Ejecución de cobertura** – tan pronto como se ve una operación calificada, la estrategia envía una orden de mercado en dirección opuesta usando `HedgeVolume`.
3. **Configuración de protección** – después de cada ejecución propia, el algoritmo cancela las órdenes protectoras existentes y registra nuevas de stop-loss y take-profit de acuerdo con el precio promedio de posición actual.
4. **Trailing stop** – cada tick de operación entrante se usa para evaluar las reglas de trailing. Una vez que el precio se ha movido al menos `TrailingStopPips + TrailingStepPips` a favor de la cobertura, el stop se acerca al precio. Para posiciones largas el stop va por debajo del mercado, para cortos por encima.
5. **Restablecimiento de posición** – cuando la posición de cobertura está totalmente cerrada (por ejemplo, por stop o por objetivo), la estrategia cancela automáticamente las órdenes protectoras restantes y espera la siguiente operación externa.

## Notas de uso

- La estrategia asume que el conector de cuenta reporta todas las operaciones de la cuenta, incluyendo las generadas por otros sistemas.
- El cálculo de pips se adapta al paso de precio del instrumento y multiplica por diez para cotizaciones de 3 o 5 dígitos, imitando el ajuste de punto MQL.
- Establecer `OriginalOrderComment` para que coincida con el comentario del sistema primario si solo se deben reflejar operaciones específicas. Al cubrir operaciones manuales, dejarlo vacío.
- Asegurarse de que `TrailingStepPips` sea mayor que cero siempre que el trailing esté habilitado para evitar la terminación prematura al inicio.
- Dado que el cobertor siempre usa un volumen fijo, puede ser conveniente ajustar `HedgeVolume` para que la cobertura cubra la exposición promedio generada por el sistema principal.
