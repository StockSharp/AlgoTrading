# Estrategia de Lanzamiento de Moneda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Lanzamiento de Moneda es un port literal del clásico asesor experto de MetaTrader que decide si comprar o vender simulando un lanzamiento de moneda. Cada vela completada desencadena una nueva decisión cuando la estrategia está plana, por lo que el sistema alterna a través de una serie continua de operaciones independientes. La conversión a StockSharp mantiene el comportamiento intencionalmente simple: solo se mantiene una posición a la vez y cada operación va emparejada con un take-profit y stop-loss simétricos expresados en pips.

Aunque la idea central es intencionalmente ingenua, el ejemplo demuestra cómo traducir incluso asesores expertos muy pequeños a la API de alto nivel de StockSharp. La estrategia es útil como ayuda didáctica para conectar suscripciones, ayudantes de gestión de dinero y órdenes protectoras.

## Lógica de trading
1. Al iniciar la estrategia, el generador de números aleatorios se inicializa con el recuento de ticks del entorno actual, reflejando el espíritu de la llamada original `MathSrand(GetTickCount())` de MQL.
2. Para cada vela terminada (el marco temporal predeterminado es 1 minuto, pero se puede proporcionar cualquier tipo de vela) la estrategia verifica si está permitido operar y si no hay ninguna posición abierta actualmente.
3. Cuando está plana, el generador produce 0 o 1. Un valor de 0 resulta en una orden de compra a mercado, mientras que 1 activa una orden de venta a mercado. El volumen se calcula dinámicamente basado en el porcentaje de riesgo configurado y la distancia al stop-loss.
4. Las órdenes protectoras creadas por `StartProtection` adjuntan un stop-loss y take-profit a cada posición para que la gestión de salida permanezca automática.

No se usan otros filtros: cada vez que se cierra una posición, la siguiente vela crea inmediatamente una nueva operación.

## Dimensionamiento de posición
La versión de StockSharp reinterpreta la fórmula del tamaño del lote para trabajar con valores de cartera. El monto de riesgo se calcula como `Portfolio.CurrentValue * RiskPercent / 100`. Este capital se divide por la distancia del stop-loss en unidades de precio (pips convertidos usando el paso de precio del instrumento) para derivar el número de contratos. El helper luego redondea el tamaño al paso de volumen admisible más cercano y aplica los límites del mercado a través de `MinVolume` y `MaxVolume`.

Esto mantiene el espíritu del código original — arriesgar un porcentaje fijo del patrimonio por operación — mientras se asegura de que el tamaño de la orden respete los metadatos de seguridad de StockSharp.

## Parámetros
| Parámetro | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `RiskPercent` | Porcentaje de la cartera arriesgado en cada operación. | `2` | Aumentar este número amplifica el volumen; las reducciones hacen las órdenes más pequeñas. |
| `TakeProfitPips` | Distancia entre la entrada y el nivel de take-profit en pips. | `20` | Convertido a precio absoluto usando el paso de precio del instrumento y pasado a `StartProtection`. |
| `StopLossPips` | Distancia entre la entrada y el nivel de stop-loss en pips. | `10` | También convertido a unidades de precio; el mismo valor se usa para el dimensionamiento de posición. |
| `CandleType` | Suscripción de velas que programa el bucle de decisión. | `marco temporal de 1 minuto` | Se puede proporcionar cualquier tipo de vela de StockSharp; intervalos más altos ralentizan el ritmo de trading. |

## Gestión de riesgos
`StartProtection` se inicia una vez durante `OnStarted` con las distancias de take-profit y stop-loss calculadas. StockSharp entonces gestiona las órdenes protectoras automáticamente, reflejando los argumentos `OrderSend` en el script MQL. Debido a que la estrategia solo opera cuando `Position == 0`, no es necesario cancelar o reenviar manualmente las órdenes existentes; la plataforma cancela las órdenes protectoras una vez que se cierra la posición.

## Notas de implementación
- El procesamiento de velas usa el patrón de alto nivel `SubscribeCandles().Bind(...)` para mayor claridad y simplicidad.
- Las declaraciones de registro describen la dirección elegida y el volumen para que los backtests muestren claramente cómo se comporta el generador pseudoaleatorio.
- La normalización del volumen tiene en cuenta `VolumeStep`, `MinVolume` y `MaxVolume`, asegurando que los tamaños generados cumplan con la especificación del instrumento.
- El código mantiene todos los comentarios en inglés, según lo requerido, y refleja la estructura exigida por las pautas del repositorio.

## Notas de uso
- Debido a que la dirección de trading es aleatoria, no se espera rentabilidad a largo plazo. Usar la estrategia con fines de demostración o prueba.
- Asegurarse de que la cartera asignada a la estrategia tenga un `CurrentValue` positivo, de lo contrario el cálculo de riesgo devuelve cero y no se realizarán operaciones.
- Ajustar el tipo de vela si se prefiere que el lanzamiento de moneda ocurra con menos frecuencia (por ejemplo, velas horarias) o más a menudo (por ejemplo, velas de tick).
- Al optimizar, se pueden explorar distancias alternativas de take-profit y stop-loss o reducir el porcentaje de riesgo para mantener las pérdidas manejables.
