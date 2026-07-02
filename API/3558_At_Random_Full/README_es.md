# Estrategia completa al azar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia At Random Full** es una fiel conversión del asesor experto MetaTrader 5 "At Random Full". Mantiene el
idea original de abrir operaciones basadas en un generador aleatorio mientras se exponen los mismos interruptores de administración de dinero: dirección
filtros, espaciado de cuadrícula, ventanas de tiempo opcionales y un interruptor de encendido/apagado para promediar. El puerto StockSharp utiliza el nivel alto API,
por lo que todo el ciclo de decisión está impulsado por suscripciones de velas y ayudantes estándar `StartProtection` para órdenes de protección.

## Lógica de trading
1. En cada vela terminada, la estrategia verifica que se permita el comercio (filtro de sesión, estado de la cartera y opcional).
bandera "sólo una posición").
2. Un generador pseudoaleatorio decide entre una entrada larga o corta. El parámetro `ReverseSignals` puede cambiar el resultado a
emular el modo inverso MQL.
3. Los filtros de dirección (`TradeMode`) bloquean señales no deseadas. El código también aplica la regla original EA de una sola operación por
barra en cada dirección recordando el tiempo de apertura de la vela de la última señal.
4. Las opciones de gestión de red reflejan el comportamiento de MetaTrader:
   - `MaxPositions` limita el número de entradas promedio por lado.
   - `MinStepPoints` requiere una distancia mínima (convertida en precio utilizando el paso del precio de seguridad) entre entradas consecutivas.
   - `CloseOpposite` obliga a cerrar la exposición opuesta existente antes de enviar una nueva operación.
5. Las órdenes de mercado se emiten a través de `BuyMarket` / `SellMarket` con un volumen normalizado definido por `OrderVolume`.

## Gestión de posiciones y riesgos
- `StartProtection` adjunta órdenes de stop-loss y take-profit que coinciden con las entradas MetaTrader. Si `TrailingStopPoints` es
mayor que cero, el modo de seguimiento integrado StockSharp está habilitado. Los parámetros `TrailingActivatePoints` y
`TrailingStepPoints` se convierten en distancias de precios y se registran para mayor transparencia, pero el seguimiento real lo maneja el
plataforma.
- Todos los cálculos de volumen respetan los metadatos de intercambio (mínimo, máximo y paso) exactamente como las rutinas auxiliares MQL.
- El control de tiempo emula el bloque `InpTimeControl` del script. Cuando está habilitado, los intercambios se permiten solo dentro del configurado
`[SessionStart, SessionEnd]` ventana; Se admiten sesiones nocturnas.

## Parámetros
| Parámetro | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Serie de velas utilizada para programar el ciclo de decisión. | `15 minute timeframe` |
| `OrderVolume` | Volumen de orden de mercado base en lotes. | `0.1` |
| `MaxPositions` | Número máximo de entradas promediadas por dirección (0 = ilimitado). | `5` |
| `MinStepPoints` | Distancia mínima entre entradas expresada en MetaTrader puntos. | `150` |
| `StopLossPoints` | Distancia de stop-loss en puntos. | `150` |
| `TakeProfitPoints` | Distancia de toma de ganancias en puntos. | `460` |
| `TrailingActivatePoints` | Umbral de beneficio (en puntos) registrado con fines informativos cuando el seguimiento está habilitado. | `70` |
| `TrailingStopPoints` | La distancia del trailing stop pasó a `StartProtection`. | `250` |
| `TrailingStepPoints` | Paso entre ajustes de seguimiento, registrado a lo largo de la distancia de activación. | `50` |
| `OnlyOnePosition` | Bloquea nuevas operaciones hasta que se cierre la posición neta actual. | `false` |
| `CloseOpposite` | Cierra la exposición opuesta antes de abrir una operación. | `false` |
| `ReverseSignals` | Invierte la decisión aleatoria para que las compras se conviertan en ventas y viceversa. | `false` |
| `UseTimeControl` | Habilita el filtro de tiempo de la sesión de negociación. | `false` |
| `SessionStart` | Hora de inicio de la sesión (inclusive) cuando `UseTimeControl` es `true`. | `10:01` |
| `SessionEnd` | Hora de finalización de la sesión (inclusive) cuando `UseTimeControl` es `true`. | `15:02` |
| `Mode` | Dirección comercial permitida (`Both`, `BuyOnly`, `SellOnly`). | `Both` |
| `RandomSeed` | Semilla determinista opcional para el generador pseudoaleatorio (0 = recuento de tics ambientales). | `0` |

## Notas de implementación
- Todos los comentarios están escritos en inglés y el código utiliza sangría de tabulación, coincidiendo con las pautas del repositorio.
- El procesamiento de velas se basa en `SubscribeCandles().Bind(...)`, lo que garantiza que la lógica se ejecute una vez por barra terminada como en EA.
- La estrategia realiza un seguimiento de los últimos precios de compra y venta para imponer la restricción de espacio mínimo incluso durante el promedio.
- Las declaraciones de registro reflejan los diagnósticos detallados impresos por el script original: cada entrada anuncia la dirección elegida,
precio de entrada, volumen y configuración final al inicio.

## Consejos de uso
- Debido a que la señal comercial es aleatoria, la estrategia es más adecuada para probar la infraestructura o demostrar controles de riesgo.
- Ajuste `OrderVolume`, `StopLossPoints` y `TakeProfitPoints` para alinearlos con el tamaño del tick y la volatilidad del instrumento que
planea comerciar.
- Habilite `UseTimeControl` si EA debe funcionar solo durante una sesión específica (por ejemplo, la sesión de Londres o Nueva York).
- Utilice `RandomSeed` durante las ejecuciones de optimización para lograr secuencias reproducibles de decisiones aleatorias.
