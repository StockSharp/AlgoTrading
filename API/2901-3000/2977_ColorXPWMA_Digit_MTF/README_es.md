# Estrategia ColorXPWMA Digit Multi-Marco Temporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia convierte el asesor experto de MetaTrader 5 **Exp_ColorXPWMA_Digit_NN3_MMRec** en la API de alto nivel de StockSharp. El robot original opera tres módulos independientes que operan en diferentes marcos temporales analizando la coloración digital de la media móvil ColorXPWMA. El port de StockSharp mantiene el mismo comportamiento: cada módulo observa su propia serie de velas, cierra posiciones cuando el indicador cambia de color y opcionalmente abre un nuevo trade en la dirección detectada.

La configuración predeterminada sigue la plantilla MT5:

| Módulo | Marco temporal | Stop Loss (puntos) | Take Profit (puntos) |
| ------ | -------------- | ------------------ | -------------------- |
| A | 8 horas | 3000 | 10000 |
| B | 4 horas | 2000 | 6000 |
| C | 1 hora | 1000 | 3000 |

Cada módulo puede habilitarse o deshabilitarse para entradas y salidas largas y cortas a través de parámetros booleanos dedicados. La implementación mantiene el seguimiento de posiciones individuales por módulo para que las operaciones largas y cortas simultáneas puedan coexistir sin interferir con la contabilidad de volumen de los otros marcos temporales.

## Indicador ColorXPWMA Digit
El indicador ColorXPWMA Digit emula el indicador personalizado MT5. Para cada vela terminada el algoritmo:

1. Construye un promedio ponderado por potencia del precio aplicado seleccionado (`Period` y `Power`).
2. Suaviza el valor con la media móvil elegida (`SmoothMethods` y `SmoothLength`).
3. Redondea el resultado al número de decimales configurado (`Digit`).
4. Asigna un código de color: **2** cuando el valor suavizado aumenta, **0** cuando disminuye, de lo contrario se reutiliza el color anterior.

`SignalBar` controla qué barra histórica se inspecciona. El valor `0` usa el vela cerrada más reciente, el valor `1` la vela anterior, etc. Una oportunidad de compra aparece cuando la barra monitoreada cambia al color `2` después de ser diferente en la barra anterior. Se genera una oportunidad de venta cuando el color se convierte en `0` después de ser diferente en la barra anterior.

Los métodos de suavizado se asignan a los indicadores de StockSharp de la siguiente manera:

- `Sma`, `Ema`, `Smma`, `Lwma`, `Jjma` → medias móviles correspondientes de StockSharp.
- `T3` → implementación interna de Tillson T3.
- `Vidya` → implementación interna de VIDYA impulsada por el Oscilador de Momentum de Chande.
- `Ama` → Media Móvil Adaptativa de Kaufman.
- Las opciones no soportadas (`JurX`, `Parabolic`) recaen en la media móvil simple, coincidiendo con el comportamiento de la plantilla original cuando no están disponibles suavizadores exóticos.

## Gestión de trades y gestión del dinero
Para cada módulo la estrategia mantiene dos posiciones virtuales independientes (larga y corta). Cuando un módulo recibe una señal de cierre, la estrategia envía una orden de mercado igual al volumen restante de esa posición virtual. Las órdenes de apertura se ignoran mientras una posición opuesta aún esté abierta.

El dimensionamiento de la posición copia el asistente de gestión del dinero de MT5:

- `NormalMM` define el volumen base.
- `SmallMM` reemplaza el volumen base cuando las operaciones recientes registraron al menos `LossTrigger` pérdidas dentro de las últimas `TotalTrigger` operaciones para esa dirección.

La lógica se evalúa por separado para secuencias largas y cortas. Los resultados de las operaciones se calculan desde el precio promedio completado cuando un módulo cierra completamente su posición virtual.

La gestión de riesgos refleja los stops de MT5 en puntos de precio:

- Cuando una posición larga está abierta y los mínimos de las velas cruzan `entry - StopLoss * PriceStep`, la posición larga se cierra inmediatamente.
- Cuando los máximos de las velas tocan `entry + TakeProfit * PriceStep`, se toman las ganancias.
- Las reglas se reflejan para las posiciones cortas (`entry + StopLoss` para protección, `entry - TakeProfit` para objetivos).

## Parámetros
Todos los parámetros están expuestos a través de objetos `StrategyParam<T>` y pueden optimizarse desde el diseñador de StockSharp. Están agrupados por módulo (A, B, C). La siguiente tabla lista las configuraciones para cualquier módulo **X**:

| Parámetro | Descripción |
| --------- | ----------- |
| `X_CandleType` | Serie de velas a suscribir (marcos temporales por defecto mostrados arriba). |
| `X_Period`, `X_Power` | Ventana ponderada por potencia usada para construir el valor base XPWMA. |
| `X_SmoothMethod`, `X_SmoothLength`, `X_SmoothPhase` | Suavizador aplicado al precio ponderado. `SmoothPhase` se mantiene por compatibilidad con usuarios MT5 JJMA. |
| `X_AppliedPrice` | Fuente de precio (close, open, high, low, median, typical, weighted, simple, quarter, TrendFollow, DeMark). |
| `X_Digit` | Precisión de redondeo aplicada al valor suavizado. |
| `X_SignalBar` | Barra histórica usada para la evaluación de señales. |
| `X_BuyMagic`, `X_SellMagic` | Preservados para trazabilidad (usados dentro de los comentarios de órdenes). |
| `X_BuyTotalTrigger`, `X_BuyLossTrigger` | Umbrales de gestión del dinero del lado largo. |
| `X_SellTotalTrigger`, `X_SellLossTrigger` | Umbrales de gestión del dinero del lado corto. |
| `X_SmallMM`, `X_NormalMM` | Volúmenes usados por la regla de gestión del dinero. |
| `X_MarginMode`, `X_Deviation` | Campos reservados mantenidos para paridad de características; no alteran las órdenes de StockSharp. |
| `X_StopLoss`, `X_TakeProfit` | Distancias en pasos de precio aplicadas a la posición virtual del módulo. |
| `X_BuyOpen`, `X_SellOpen`, `X_SellClose`, `X_BuyClose` | Interruptores de permiso para las acciones del módulo. |

## Notas
- Cada orden de mercado está anotada con `A|BuyOpen`, `B|SellClose`, etc. para que los fills puedan ser rastreados hasta su módulo.
- La estrategia opera exclusivamente en velas terminadas y por lo tanto reproduce la protección `IsNewBar` de MT5 proporcionada automáticamente por la API de alto nivel.
- Si múltiples módulos se activan en la misma barra, sus volúmenes se procesan secuencialmente usando los buffers de posición virtual por módulo.
