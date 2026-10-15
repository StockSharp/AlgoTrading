# Estrategia Color XPWMA Digit MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Color XPWMA Digit MMRec** replica el asesor experto MetaTrader `Exp_ColorXPWMA_Digit_MMRec`. Utiliza el indicador ColorXPWMA Digit para identificar puntos de inflexión de tendencia y envuelve la lógica del contador de gestión de dinero original. El indicador construye una media móvil ponderada por potencia (PWMA) que es opcionalmente suavizada por un método de media móvil seleccionado. La pendiente de la línea suavizada se convierte en colores discretos: `2` para pendiente ascendente, `0` para pendiente descendente y `1` cuando la dirección es plana.

Las decisiones de trading se toman después de evaluar los colores del indicador en una barra histórica configurable (`SignalBar`). Cuando el color anterior (`SignalBar + 1`) era alcista (2) pero la barra en `SignalBar` ya no mantiene el color alcista, la estrategia cierra posiciones cortas y opcionalmente abre una nueva posición larga. La lógica inversa se aplica cuando el color histórico era bajista (0) pero la barra más reciente ya no mantiene ese color bajista.

## Lógica del indicador
- **Media móvil ponderada por potencia** – cada barra recibe un peso `(period - index)^power`. Las potencias más altas enfatizan las últimas muestras.
- **Suavizado** – la serie ponderada se pasa por una media móvil suavizadora. Los métodos soportados incluyen SMA, EMA, SMMA, LWMA, Jurik, T3 y Kaufman AMA. Las opciones JurX, Parabólico y VIDYA se aproximan con suavizado exponencial porque StockSharp no expone implementaciones directas.
- **Codificación de color** – el signo de la pendiente suavizada define el buffer de color que desencadena entradas y salidas.
- **Redondeo de dígitos** – el valor final puede redondearse a un número fijo de dígitos para coincidir con el comportamiento original de "Digit".

## Reglas de trading
1. **Fallo de continuación alcista**
   - Condición: el color en `SignalBar + 1` es igual a `2` (alcista) y el color en `SignalBar` es diferente de `2`.
   - Acción: cerrar cortos activos; si se permiten entradas largas, abrir una nueva posición larga dimensionada por el contador de gestión de dinero.
2. **Fallo de continuación bajista**
   - Condición: el color en `SignalBar + 1` es igual a `0` (bajista) y el color en `SignalBar` es diferente de `0`.
   - Acción: cerrar largos activos; si se permiten entradas cortas, abrir una nueva posición corta dimensionada por el contador.

Las órdenes siempre se ejecutan en el cierre de la vela que produjo la señal. Al cambiar de dirección, la estrategia cierra la exposición opuesta e inmediatamente abre la nueva posición en una única orden de mercado.

## Contador de gestión de dinero
La estrategia mantiene un historial continuo de resultados de operaciones cerradas para largos y cortos. Antes de abrir una nueva operación, inspecciona los resultados más recientes de `BuyTotalTrigger` o `SellTotalTrigger`:

- Si el número de operaciones perdedoras en esa ventana alcanza el disparador de pérdida respectivo (`BuyLossTrigger` o `SellLossTrigger`), el tamaño de posición se reduce a `ReducedVolume`.
- De lo contrario, se usa el `NormalVolume` estándar.

Esto reproduce el comportamiento de las rutinas originales `BuyTradeMMRecounterS` y `SellTradeMMRecounterS`.

## Parámetros
| Grupo | Parámetro | Descripción |
| --- | --- | --- |
| General | `CandleType` | Marco temporal usado tanto para cálculos del indicador como para decisiones de trading. |
| Indicador | `IndicatorPeriod` | Período de la media móvil ponderada por potencia. |
| Indicador | `IndicatorPower` | Exponente aplicado a los pesos. Valores más altos enfatizan las barras más recientes. |
| Indicador | `SmoothingMethod` | Método de media móvil usado para suavizado. JurX, ParMa y Vidya recurren a una media exponencial. |
| Indicador | `SmoothingLength` | Longitud de la media móvil de suavizado. |
| Indicador | `SmoothingPhase` | Parámetro de fase reenviado a suavizadores que lo soportan. |
| Indicador | `AppliedPrices` | Precio fuente usado por el indicador (cierre, apertura, alto, bajo, etc.). |
| Indicador | `RoundingDigits` | Número de dígitos decimales usados para redondear la salida del indicador. |
| Lógica | `SignalBar` | Desplazamiento histórico (en barras) usado al leer el buffer de color. |
| Permisos | `EnableBuyEntries` / `EnableSellEntries` | Permitir abrir posiciones largas/cortas. |
| Permisos | `EnableBuyExits` / `EnableSellExits` | Permitir cerrar largos/cortos. |
| Gestión de dinero | `NormalVolume` | Tamaño de orden predeterminado. |
| Gestión de dinero | `ReducedVolume` | Tamaño de orden aplicado después de una racha de pérdidas. |
| Gestión de dinero | `BuyTotalTrigger`, `BuyLossTrigger` | Número de operaciones largas recientes a inspeccionar y umbral de pérdida para cambiar al volumen reducido. |
| Gestión de dinero | `SellTotalTrigger`, `SellLossTrigger` | Misma lógica para operaciones cortas. |
| Gestión de riesgo | `StopLossPoints`, `TakeProfitPoints` | Distancias de protección opcionales (puntos) aplicadas a través de `StartProtection` si no son cero. |

## Notas prácticas
- Mantenga `SignalBar = 1` para imitar el comportamiento predeterminado del Asesor Experto y garantizar que las señales se evalúen en velas completamente cerradas.
- La estrategia almacena solo los resultados más recientes necesarios para el contador, evitando el crecimiento descontrolado de memoria.
- Debido a que StockSharp ejecuta órdenes de forma asíncrona, la estrategia asume rellenos al precio de cierre de la vela al actualizar los contadores de pérdida. Esto refleja cómo funcionó el experto MQL original con datos históricos.
- Las opciones de suavizado JurX, ParMa y Vidya son aproximaciones que usan suavizado exponencial internamente. Si requiere los filtros propietarios originales, implemente clases de indicadores personalizadas y conéctelas a la estrategia.
