# Exp Color PEMA Digit TM Plus MMRec Duplex (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia recrea el Asesor Experto "Exp_ColorPEMA_Digit_Tm_Plus_MMRec_Duplex" utilizando la API de alto nivel de StockSharp. Opera con dos flujos independientes de Media Móvil Exponencial Quíntuple (PEMA) que pueden usar diferentes marcos temporales y fuentes de precios. El módulo largo abre operaciones cuando la pendiente del PEMA cambia a alcista, mientras que el módulo corto reacciona a los giros bajistas. Cada lado admite salidas basadas en indicadores y un temporizador de seguridad que fuerza el cierre de la posición después de un número configurable de minutos.

## Indicadores
* **PEMA quíntuple** – indicador personalizado que encadena ocho medias exponenciales de la misma longitud y las combina usando los coeficientes clásicos (8, -28, 56, -70, 56, -28, 8, -1). El indicador expone tanto el valor actual como la muestra anterior para que la estrategia pueda clasificar la dirección de la pendiente (arriba, abajo, plana).
* **Lógica de color** – la pendiente se mapea a tres estados discretos: arriba (verde), abajo (magenta) y neutral (gris), reproduciendo el comportamiento del indicador ColorPEMA original.

## Generación de señales
### Módulo largo
1. Espera una vela finalizada en el marco temporal largo seleccionado.
2. Solicita el valor del PEMA usando el modo de precio configurado y los dígitos de redondeo.
3. Evalúa el estado de color `SignalBar` velas atrás y lo compara con la barra anterior.
4. **Entrada**: cuando el color cambia a `Up` y las entradas están permitidas, la estrategia compra usando el `TradeVolume` compartido y almacena la hora de entrada.
5. **Salida**: cuando el color cambia a `Down`, la estrategia cierra la posición larga si las salidas basadas en indicadores están habilitadas.
6. **Guardia temporal**: si la posición larga abierta sobrevive más de `LongTimeExitMinutes`, se cierra independientemente del estado del indicador.

### Módulo corto
El lado corto repite el mismo flujo de trabajo de manera independiente:
1. Monitorear las velas del marco temporal corto.
2. Calcular la serie PEMA corta.
3. Buscar `ShortSignalBar` velas atrás para detectar un cambio al color `Down`.
4. **Entrada**: cuando el color se vuelve bajista y los cortos están habilitados, la estrategia vende.
5. **Salida**: cuando el color se vuelve `Up`, el corto se cubre si las salidas están permitidas.
6. **Guardia temporal**: si se supera `ShortTimeExitMinutes`, la posición corta se cierra.

## Gestión de riesgos
* Usa el parámetro `TradeVolume` para configurar el tamaño predeterminado de la orden.
* Se puede establecer un stop-loss y take-profit opcionales en pasos de precio. Cuando cualquiera de ellos es positivo, la estrategia habilita `StartProtection` con órdenes de salida de mercado, reflejando la protección de gestión monetaria presente en la versión MQL.
* Temporizadores de salida basados en tiempo independientes para los módulos largo y corto evitan que las operaciones se prolonguen indefinidamente.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `LongCandleType` | Marco temporal utilizado para el flujo de indicadores largo. |
| `ShortCandleType` | Marco temporal para el flujo de indicadores corto. |
| `LongEmaLength`, `ShortEmaLength` | Longitudes de suavizado del PEMA quíntuple (se admiten valores fraccionarios). |
| `LongPriceMode`, `ShortPriceMode` | Modo de precio aplicado para cada flujo (cierre, apertura, máximo, mínimo, mediano, típico, ponderado, simple, cuarto, seguimiento de tendencia y Demark). |
| `LongDigits`, `ShortDigits` | Redondeo decimal aplicado a los valores calculados del PEMA. |
| `LongSignalBar`, `ShortSignalBar` | Número de barras completadas hacia atrás para evaluar el cambio de color. |
| `LongAllowOpen`, `ShortAllowOpen` | Habilitar/deshabilitar nuevas entradas para cada lado. |
| `LongAllowClose`, `ShortAllowClose` | Habilitar/deshabilitar salidas basadas en indicadores. |
| `LongAllowTimeExit`, `ShortAllowTimeExit` | Activar o desactivar el temporizador de salida basado en tiempo. |
| `LongTimeExitMinutes`, `ShortTimeExitMinutes` | Tiempo máximo de retención en minutos para operaciones largas y cortas. |
| `TradeVolume` | Volumen predeterminado para órdenes de mercado. |
| `StopLossSteps`, `TakeProfitSteps` | Distancias protectoras opcionales expresadas en pasos de precio del instrumento. |

## Notas
* La estrategia se suscribe a ambas series de velas largas y cortas; si ambos parámetros apuntan al mismo marco temporal, StockSharp reutiliza automáticamente el feed de datos.
* Ambos módulos comparten la misma configuración de instrumento y volumen, garantizando un comportamiento simétrico.
* Los cálculos de indicadores se ejecutan solo en velas finalizadas para evitar el repintado.
