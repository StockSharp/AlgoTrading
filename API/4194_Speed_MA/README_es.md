# Estrategia MA de velocidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Speed MA** es una versión StockSharp directa del MetaTrader 4 asesor experto `ytg_Speed_MA_ea`. El sistema original mide la rapidez con la que una media móvil simple cambia de una barra a la siguiente. Cuando la pendiente de la media móvil supera un umbral definido por el usuario, el experto abre una posición de mercado en la dirección correspondiente. Esta implementación de C# reproduce ese comportamiento con el nivel alto de StockSharp API: se suscribe a velas, evalúa un promedio móvil simple desplazado y activa operaciones cuando la diferencia entre valores desplazados consecutivos es lo suficientemente grande. La estrategia mantiene el volumen de pedidos, los objetivos de ganancias y el límite de pérdidas expresados ​​en MetaTrader "puntos" para permanecer fiel al código fuente.

## Lógica de trading
1. Suscríbase al tipo de vela configurado (velas de un minuto de forma predeterminada) y cree una media móvil simple utilizando el parámetro `MovingAveragePeriod`.
2. Para cada vela terminada, registre el último valor de promedio móvil. La lista del historial mantiene solo los valores necesarios para evaluar el `Shift` configurado y la barra anterior.
3. Calcule la pendiente como la diferencia entre el valor de la media móvil `Shift` barras hacia atrás y el valor una barra antes (es decir, `Shift + 1` barras hacia atrás). Esto refleja la llamada MetaTrader `iMA(..., shift)` y `iMA(..., shift + 1)`.
4. Compare la pendiente con `SlopeThresholdPoints` convertida en unidades de precio absoluto. Si la diferencia es mayor que el umbral positivo, genere una señal larga. Si la diferencia es inferior al umbral negativo, genere una señal corta.
5. Cuando `ReverseSignals` esté habilitado, invierta la señal generada para que una pendiente alcista abra una posición corta y viceversa.
6. Sólo envíe una nueva orden de mercado cuando no haya ninguna posición activa. El asesor experto original se basó en `OrdersTotal() < 1` y nunca revirtió directamente; esta implementación se comporta de manera idéntica al ignorar las señales mientras una posición está abierta.
7. Las órdenes de protección se gestionan a través de `StartProtection`. Las distancias de parada de pérdidas y toma de ganancias se definen en MetaTrader puntos (`TakeProfitPoints` y `StopLossPoints`) y se traducen automáticamente en compensaciones de precios utilizando la precisión decimal del valor.

## Gestión del riesgo
- **Stop-loss**: `StopLossPoints` define cuántos MetaTrader puntos por debajo/por encima de la entrada se coloca el tope de protección. Un valor de `0` desactiva el stop-loss.
- **Take-profit**: `TakeProfitPoints` establece la distancia objetivo de ganancias en MetaTrader puntos. La configuración `0` desactiva el objetivo de ganancias.
- La estrategia no se detiene ni obtiene ganancias parciales; se centra en replicar el comportamiento original que establece inmediatamente objetivos fijos y se detiene cuando se completa una orden.
- Debido a que el experto sólo abre una nueva posición cuando está plano, nunca hay más de una posición activa. Esto hace que el tamaño de la posición sea predecible y refleja la implementación de MetaTrader donde el volumen se fijó en 0,1 lotes.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `OrderVolume` | Volumen comercial utilizado para las entradas al mercado. Equivalente al tamaño de lote `0.1` del EA original. | `0.1` |
| `MovingAveragePeriod` | Período de la media móvil simple utilizada para medir la velocidad. | `13` |
| `Shift` | Número de barras completadas entre la vela actual y la muestra de media móvil. La estrategia compara los valores en `shift` y `shift + 1`. | `1` |
| `SlopeThresholdPoints` | Diferencia mínima entre los dos valores de media móvil desplazados, medida en MetaTrader puntos. | `10` |
| `ReverseSignals` | Invierta la dirección comercial para que una pendiente alcista abra una posición corta. | `false` |
| `TakeProfitPoints` | Distancia de obtención de beneficios expresada en MetaTrader puntos (convertida internamente a precio absoluto). | `500` |
| `StopLossPoints` | Distancia de stop-loss expresada en MetaTrader puntos (convertida internamente a precio absoluto). | `490` |
| `CandleType` | Tipo de vela utilizado para los cálculos (el valor predeterminado es un período de tiempo de 1 minuto). | `1 minute` período de tiempo |

## Notas de implementación
- La constante `Point` de MetaTrader se reconstruye utilizando el `Decimals` del instrumento. Para símbolos Forex de 5 o 3 decimales, el código divide uno por `10^Decimals` para obtener el mismo valor de tick utilizado en MetaTrader.
- El historial de valores de media móvil se recorta para conservar solo las muestras requeridas por el `Shift` seleccionado. Esto evita el crecimiento ilimitado de la memoria y al mismo tiempo respeta los índices exactos a los que hace referencia el asesor experto.
- `StartProtection` convierte los MetaTrader parámetros basados en puntos en StockSharp `Unit` instancias con compensaciones de precios absolutas. Esto mantiene las distancias de stop-loss y take-profit idénticas a las de la versión MQL4.
- La estrategia utiliza el flujo de trabajo de alto nivel `SubscribeCandles().Bind(...)` para que las actualizaciones de indicadores y la evaluación de señales ocurran solo en velas terminadas. No se requiere ninguna llamada manual a `Indicator.GetValue()`.
- Se proporcionan comentarios en línea en inglés en el código fuente para resaltar las decisiones de conversión críticas.
- Solo se proporciona la implementación de C#. Se omite intencionalmente un puerto de Python que coincide con la solicitud.

## Consejos de uso
- Reducir `SlopeThresholdPoints` aumenta la cantidad de operaciones porque los movimientos más pequeños de promedio móvil califican como señales. Aumentar el valor filtra más operaciones y exige un impulso más fuerte.
- Ajuste `Shift` para cambiar cuántas barras atrás se mide la pendiente. Un valor de `0` compara la barra terminada actual con la barra anterior, mientras que los valores más altos evalúan secciones más antiguas de la media móvil.
- Combine la estrategia con StockSharp módulos de riesgo o controles a nivel de cartera si se requiere una administración de dinero adicional más allá de los límites y objetivos fijos.
- Asegúrese de que el `CandleType` suscrito coincida con el período de tiempo que se utilizó al optimizar el experto MQL4. Las diferencias en el calendario alteran drásticamente la magnitud de la pendiente.

## Diferencias frente al Expert Advisor original
- Las entradas y salidas del mercado utilizan los asistentes de orden de mercado de StockSharp en lugar de `OrderSend`, pero el comportamiento resultante (una orden de mercado con SL/TP fijo) sigue siendo idéntico.
- MetaTrader gestiona pedidos utilizando recuentos de tickets; StockSharp monitorea la posición agregada. La lógica que requiere una posición plana antes de abrir una nueva operación recrea `OrdersTotal() < 1` en el nuevo entorno.
- El registro, la visualización de gráficos y el manejo de unidades ahora aprovechan las funciones StockSharp, lo que proporciona mejores diagnósticos sin afectar las decisiones comerciales.

## Archivos
- `CS/SpeedMAStrategy.cs` – implementación de estrategia.
- `README.md`, `README_zh.md`, `README_ru.md`: documentación detallada en inglés, chino y ruso respectivamente.

No se incluye ningún directorio de Python, de acuerdo con las pautas de conversión.
