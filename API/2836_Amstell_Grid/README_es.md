# Estrategia de Cuadrícula Amstell
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Cuadrícula Amstell es un puerto en C# del asesor experto de MetaTrader 5 `exp_Amstell.mq5`. Crea una cuadrícula simétrica de compra/venta y aplica un take profit virtual a las entradas individuales. La conversión sigue las pautas de la API de alto nivel de StockSharp y reemplaza el manejo de ticks con procesamiento de velas mientras mantiene intacta la idea original.

## Cómo funciona

1. **Inicialización**
   - La estrategia se suscribe al tipo de vela configurado e inicia la protección de posición.
   - Se calcula un tamaño de pip ajustado a partir del `PriceStep` del instrumento y la precisión decimal. Los símbolos de cinco dígitos y tres dígitos reciben automáticamente un multiplicador de 10x, reflejando la implementación de MT5.

2. **Primera operación**
   - Cuando los precios de compra y venta registrados están vacíos (lanzamiento inicial), se envía inmediatamente una orden de compra a mercado. Esto arranca la cuadrícula exactamente como el asesor experto original.

3. **Expansión de la cuadrícula**
   - Se emite una nueva **compra** cada vez que el precio de cierre actual está al menos `StepPips` por debajo del último precio de compra registrado.
   - Se emite una nueva **venta** cada vez que el precio está al menos `StepPips` por encima del último precio de venta registrado.
   - La estrategia rastrea internamente pilas separadas de largos y cortos para que las órdenes alternadas puedan coexistir incluso en una cuenta de netting. Las órdenes opuestas primero reducen la otra pila antes de agregar nueva exposición, reproduciendo el comportamiento de cobertura de la versión MT5.

4. **Take Profit virtual**
   - Cada largo abierto se monitorea de forma independiente. Cuando el precio avanza `TakeProfitPips`, se envía una venta a mercado solo por el volumen de esa posición.
   - Cada corto abierto se trata de manera similar en la dirección opuesta. El take profit es "virtual" porque las posiciones se cierran programáticamente sin usar órdenes TP del lado del broker.
   - Después de que una dirección se haya cerrado completamente mientras el lado opuesto aún existe, el precio del último trato correspondiente se limpia para que la siguiente orden en esa dirección pueda dispararse inmediatamente, igual que en el código original.

5. **Seguimiento del estado**
   - El controlador `OnOwnTradeReceived` reconstruye las pilas de largos/cortos a partir de las operaciones ejecutadas, permitiendo manejar llenados parciales y reversiones con elegancia.
   - Los últimos precios de compra/venta permanecen en caché cuando ambos lados están planos para que la cuadrícula espere el paso requerido antes de volver a entrar después de un reinicio completo.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `Volume` | `0.1` | Tamaño de orden utilizado para cada orden de mercado en ambas direcciones. |
| `TakeProfitPips` | `50` | Distancia en pips que debe ganarse antes de que se cierre una posición individual. |
| `StepPips` | `15` | Brecha en pips entre órdenes de cuadrícula consecutivas de la misma dirección. |
| `CandleType` | `1 Minute` | Fuente de datos de velas usada para aproximar la lógica basada en ticks. |

Todas las configuraciones basadas en pips respetan el paso de precio y precisión del instrumento. Por ejemplo, en EURUSD (5 dígitos) `StepPips = 15` corresponde a 0.0015.

## Notas prácticas

- La estrategia usa precios de cierre de velas para emular las comparaciones a nivel de tick que se encuentran en el código MT5. Para operaciones de alta frecuencia, reduzca el marco temporal.
- No existe stop-loss por defecto. Como con cualquier enfoque de cuadrícula, las tendencias desbocadas pueden acumular gran exposición. Use volúmenes conservadores y considere la supervisión basada en sesiones.
- Debido a que los take profits se manejan virtualmente, las operaciones cerradas se reflejan inmediatamente en el PnL de la estrategia sin colocar órdenes TP visibles en el broker.
- La implementación deja los últimos precios en caché sin cambios después de que ambos lados se aplanen. Esto preserva el comportamiento original donde la cuadrícula espera el desplazamiento de precio antes de reiniciarse.

## Archivos

- `CS/AmstellGridStrategy.cs` – Implementación de la estrategia StockSharp con extensos comentarios en línea.
- `README.md`, `README_ru.md`, `README_zh.md` – Documentación completa en inglés, ruso y chino.

Este puerto está listo para mayor personalización (por ejemplo, gestión de dinero, límites de riesgo) directamente dentro del ecosistema StockSharp.
