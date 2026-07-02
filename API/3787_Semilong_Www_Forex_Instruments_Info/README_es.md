# Estrategia de información de instrumentos Forex WWW semilargo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el comportamiento del experto "Semilong" MetaTrader. Supervisa la distancia entre el precio de oferta actual y dos precios de cierre históricos que están separados por cambios configurables. Cuando el mercado actual cotiza lo suficientemente por debajo (o por encima) del cierre anterior, mientras que el cierre anterior también se ha alejado de una referencia aún más antigua, la estrategia abre una posición larga (o corta). La gestión de posiciones refleja el script original con toma de ganancias configurable, stop loss, trailing stop opcional y un módulo de lote automático que reduce el tamaño después de pérdidas consecutivas.

## Generación de señal
- **Cambios históricos**: `ShiftOne` selecciona de cuántas velas terminadas se toma el primer cierre de comparación, mientras que `ShiftTwo` agrega una compensación adicional para el segundo cierre.
- **Filtros de desviación**: `MoveOnePoints` define hasta qué punto la oferta actual debe alejarse del primer cierre desplazado y `MoveTwoPoints` mide la distancia entre ambos cierres desplazados.
- **Configuración larga**: se activa cuando la oferta actual está al menos `MoveOnePoints` por debajo del primer cierre desplazado y el primer cierre desplazado está al menos `MoveTwoPoints` por encima del segundo cierre desplazado.
- **Configuración corta**: se activa cuando la oferta actual está al menos `MoveOnePoints` por encima del primer cierre desplazado y el primer cierre desplazado está al menos `MoveTwoPoints` por debajo del segundo cierre desplazado.
- La estrategia espera a que se completen las velas, ignora las señales cuando las órdenes ya están activas y requiere un margen libre positivo antes de negociar.

## Gestión Comercial
- **Órdenes de protección iniciales**: en lugar de registrar órdenes pendientes, la estrategia emula el comportamiento original rastreando el precio de entrada y saliendo del mercado una vez que el movimiento alcanza:
  - `ProfitPoints` (más el diferencial actual) a favor de la posición.
  - `LossPoints` contra la posición.
- **Trailing stop**: cuando `TrailingPoints` es mayor que cero, la estrategia registra el mejor precio alcanzado después de la entrada. Si el precio retrocede la distancia de seguimiento, la posición se cierra.
- **Política de posición única**: solo se permite una posición de mercado a la vez; Las nuevas señales se ignoran mientras se ejecuta una operación o mientras hay órdenes de cierre pendientes.

## Dimensionamiento de posiciones
- **Volumen fijo**: cuando `UseAutoLot` está deshabilitado, cada operación utiliza `FixedVolume` (ajustado al paso y los límites del volumen del instrumento).
- **Cálculo de lote automático**: cuando está habilitado, el margen libre se divide por `AutoMarginDivider * 1000` y se redondea al lote entero más cercano. Si se han producido al menos dos operaciones perdedoras consecutivamente, el volumen se reduce en `lossStreak / DecreaseFactor` proporcionalmente, imitando la lógica de disminución de MT4.
- El volumen se fija entre `FixedVolume` y 99 lotes y luego se ajusta a los límites de paso/mínimo/máximo de volumen del instrumento.

## Notas adicionales
- El diferencial se lee a partir de la mejor oferta/pedido actual y se utiliza para ampliar el objetivo de ganancias, coincidiendo con el EA original.
- El margen libre se aproxima a partir de la cartera conectada (`CurrentValue - BlockedValue`), recurriendo al capital actual si los datos del margen no están disponibles.
- Todos los enlaces de optimización, gráficos y registros en tiempo de ejecución se dejan en manos de la infraestructura estándar de StockSharp para que la estrategia pueda optimizarse a través del diseñador o ejecutarse directamente en el proyecto API.
