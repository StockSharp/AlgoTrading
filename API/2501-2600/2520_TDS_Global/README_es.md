# Estrategia TDS Global
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el experto original de MetaTrader "TDSGlobal" basado en el concepto Triple Screen de Alexander Elder. Evalúa velas diarias y combina la pendiente del MACD (12, 23, 9) con un filtro Williams %R de 24 períodos. El sistema busca comprar cuando la tendencia está girando hacia arriba mientras que %R muestra condiciones de sobreventa, y vender cuando la tendencia gira hacia abajo y %R señala sobrecompra.

Siempre que se detecta una configuración válida, la estrategia coloca órdenes stop más allá del máximo o mínimo de la sesión anterior. Las entradas se alejan del mercado actual por un búfer configurable para evitar entrar demasiado cerca del precio, imitando la lógica de offset original de "16 puntos". Una vez en posición, la estrategia gestiona un stop de protección, toma de ganancias opcional y un trailing stop en pasos de precio.

## Lógica de Trading

- **Datos**: Trabaja con velas diarias por defecto (configurable).
- **Filtro de tendencia**: Compara los dos valores más recientes de la línea principal del MACD. MACD ascendente implica sesgo largo, MACD descendente implica sesgo corto.
- **Filtro oscilador**: Usa el valor anterior de Williams %R. Por debajo de `WilliamsBuyLevel` (predeterminado -75) permite configuraciones largas, por encima de `WilliamsSellLevel` (predeterminado -25) permite configuraciones cortas.
- **Entrada**:
  - Largo: colocar un buy-stop por encima del máximo anterior más un paso de precio. La entrada se eleva a al menos `EntryBufferSteps` pasos de precio por encima del último cierre para mantener una distancia mínima del mercado.
  - Corto: colocar un sell-stop por debajo del mínimo anterior menos un paso de precio. La orden se baja a como máximo el último cierre menos los pasos de `EntryBufferSteps`.
- **Gestión de riesgos**:
  - El stop inicial está anclado al extremo opuesto de la vela anterior (máximo para cortos, mínimo para largos).
  - La distancia de toma de ganancias equivale a `TakeProfitSteps` pasos de precio. El valor predeterminado (999) mantiene el comportamiento cercano a la versión MQL que usaba un objetivo muy amplio.
  - El trailing stop se habilita cuando `TrailingStopSteps` > 0. Sigue el cierre por esa cantidad de pasos y solo se ajusta en la dirección de la operación.
- **Manejo de órdenes**:
  - Las órdenes stop existentes se cancelan y se actualizan cuando el precio de entrada o los niveles de protección necesitan ser actualizados.
  - Las señales de tendencia opuesta eliminan las órdenes pendientes que ya no se alinean con la dirección del MACD.
  - Cuando se abre una posición, los niveles pendientes almacenados se reutilizan para inicializar los precios de stop/toma de ganancias en vivo.
- **Escalonamiento opcional**: El EA original escalonaba la colocación de órdenes en pares de divisas para evitar órdenes pendientes simultáneas. Establecer `UseSymbolStagger` en `true` impone las mismas ventanas de minutos para EURUSD, GBPUSD, USDCHF y USDJPY.

## Parámetros

- `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – Períodos MACD usados para la verificación de la pendiente de tendencia.
- `WilliamsLength` – Períodos de lookback para Williams %R.
- `WilliamsBuyLevel`, `WilliamsSellLevel` – Umbrales de sobreventa/sobrecompra (valores negativos, más cercanos a -100/-0 respectivamente).
- `EntryBufferSteps` – Offset mínimo desde el mercado actual al colocar entradas stop (número de pasos de precio).
- `TakeProfitSteps` – Distancia objetivo en pasos de precio (establecer un número pequeño para activar un objetivo rígido).
- `TrailingStopSteps` – Distancia del trailing stop en pasos; establecer en cero para deshabilitar el trailing.
- `UseSymbolStagger` – Habilita las ventanas de minutos específicas por símbolo.
- `CandleType` – Marco temporal para las velas (diario por defecto).

## Notas

- Usar el volumen de estrategia para controlar el tamaño del lote; por defecto es 1 si no se especifica volumen.
- Las órdenes pendientes y las salidas trailing operan en velas completadas, por lo que los llenados entre cierres de velas se aproximan con el precio de entrada almacenado.
- El valor predeterminado del take profit es grande para coincidir con el comportamiento del EA original; ajustarlo cuando se necesita un objetivo finito.
