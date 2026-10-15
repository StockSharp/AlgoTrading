# Estrategia de inmersión de Heiken Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia replica el comportamiento de los MetaTrader 5 expertos **heiken ashi engulf ea buy mt5.mq5** y **heiken ashi engulf sell ea mt5.mq5** combinando ambas direcciones dentro de una sola StockSharp estrategia de alto nivel. Reconstruye las velas Heiken Ashi clásicas a partir del período de tiempo suscrito, espera un patrón envolvente, lo confirma con una alineación de promedio móvil y dos filtros basados ​​en RSI, y finalmente abre una posición de mercado con distancias fijas opcionales de stop-loss y take-profit expresadas en MetaTrader pips.

La conversión mantiene separadas las configuraciones originales de “compra” y “venta” para que cada lado pueda optimizarse de forma independiente. Un selector de dirección permite a los operadores ejecutar solo el libro de jugadas alcista, solo el bajista o ambos a la vez.

## Lógica de trading
### Reconstrucción de Heiken Ashi
1. Para cada vela completa, la estrategia construye valores de apertura, máximo, mínimo y cierre de Heiken Ashi utilizando la apertura y el cierre sintéticos anteriores (algoritmo MT estándar).
2. Se almacenan dos velas históricas de Heiken Ashi (`shift = 1` y `shift = 2`) para emular los parámetros `Shift` del código MetaTrader.

### Configuración larga
1. No se permite ninguna posición abierta (equivalente al bloque `NoOpenedOrders`.
2. La última vela Heiken Ashi debe ser alcista y la anterior bajista (`ChosenCandleType = 1`, `PreviousCandleType = 2`).
3. La vela real más reciente debe cerrar por encima del máximo de la vela anterior (`Close[1] > High[2]`), mientras que la vela anterior debe ser bajista (`Close[2] < Open[2]`).
4. El cierre Heiken Ashi de la vela más nueva debe permanecer por encima de la media móvil base (`iMA` con parámetros `BuyBaselineMethod/Period`).
5. La MA de tendencia rápida debe estar por encima de la MA de tendencia lenta (`BuyFast` frente a `BuySlow`).
6. Dos filtros RSI deben mantener sus valores dentro de los límites configurados para el número especificado de velas (la misma lógica que el bloque `IndicatorWithinLimits`, incluido el contador de excepciones).
7. Si se cumplen todas las condiciones, la estrategia compra el volumen solicitado, convierte las distancias de stop-loss y take-profit configuradas de pips a unidades de precio y establece órdenes de protección a través de `SetStopLoss` / `SetTakeProfit`. Un mensaje de registro opcional replica la alerta MetaTrader.

### Configuración corta
La lógica corta refleja las reglas largas con comparaciones opuestas:
1. Posición plana.
2. La última vela Heiken Ashi es bajista y la anterior alcista.
3. La vela real más reciente cierra por debajo del mínimo de la vela anterior (`Close[1] < Low[2]`), y esa vela anterior es alcista.
4. El cierre de Heiken Ashi se mantiene por debajo de la MA de referencia bajista, mientras que la MA rápida permanece por debajo de la MA lenta.
5. Ambos filtros RSI permanecen entre sus límites, utilizando su propia configuración de turno/período/excepción.
6. Se coloca una orden de venta de mercado y se aplican las distancias de stop-loss/take-profit para cortos.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | H1 | Plazo utilizado para todos los indicadores y señales. |
| `Direction` | ambos | Qué lado del libro de jugadas envolvente debe estar activo (`BuyOnly`, `SellOnly`, `Both`). |
| `BuyVolume` | 0,01 | Tamaño de lote para operaciones largas. |
| `BuyStopLossPips` | 50 | MetaTrader pips entre la entrada y el stop-loss para posiciones largas. `0` desactiva la parada fija. |
| `BuyTakeProfitPips` | 50 | MetaTrader pips entre entrada y obtención de beneficios para posiciones largas. `0` desactiva el objetivo fijo. |
| `BuyBaselinePeriod` / `BuyBaselineMethod` | 20 / Exponencial | MA en comparación con la vela alcista Heiken Ashi (espejos `inp1_Ro_*`). |
| `BuyFastPeriod` / `BuyFastMethod` | 20 / Exponencial | MA de tendencia rápida (`inp12_Lo_*`). |
| `BuySlowPeriod` / `BuySlowMethod` | 30 / Exponencial | MA de tendencia lenta (`inp12_Ro_*`). |
| `BuyPrimaryRsi*` | 14, turno 1, ventana 2, excepciones 0, límites [0;100] | Primer filtro RSI (coincide con `inp13_*`). |
| `BuySecondaryRsi*` | 5, turno 2, ventana 3, excepciones 0, límites [0;100] | Segundo filtro RSI (`inp14_*`). |
| `SellVolume` | 0,01 | Tamaño de lote para operaciones cortas. |
| `SellStopLossPips` | 50 | MetaTrader pips entre la entrada y el stop-loss para cortos. |
| `SellTakeProfitPips` | 50 | MetaTrader pips entre entrada y obtención de beneficios para cortos. |
| `SellBaselinePeriod` / `SellBaselineMethod` | 20 / Exponencial | MA de referencia para configuraciones bajistas (`inp15_*`). |
| `SellFastPeriod` / `SellFastMethod` | 20 / Exponencial | MA de tendencia rápida (`inp26_Lo_*`). |
| `SellSlowPeriod` / `SellSlowMethod` | 30 / Exponencial | MA de tendencia lenta (`inp26_Ro_*`). |
| `SellPrimaryRsi*` | 14, turno 1, ventana 2, excepciones 0, límites [0;100] | Primer filtro RSI para cortos (`inp27_*`). |
| `SellSecondaryRsi*` | 5, turno 2, ventana 3, excepciones 0, límites [0;100] | Segundo filtro RSI para cortos (`inp28_*`). |
| `AlertTitle` | "Mensaje de alerta" | Texto escrito en el registro cuando se abre una operación. |
| `SendNotification` | cierto | Habilita el mensaje de registro de información que reemplaza MetaTrader ventanas emergentes/notificaciones. |

## Gestión del riesgo
- Las distancias de stop-loss y take-profit se convierten de MetaTrader pips a unidades de precio. La conversión escala automáticamente el valor según el tamaño del tick de seguridad (se incluye soporte para cotizaciones de 3/5 dígitos).
- Cuando se ejecuta una nueva operación, la posición resultante esperada se pasa a `SetStopLoss` / `SetTakeProfit`, imitando la ubicación del stop virtual/real original.
- No había ninguna lógica de seguimiento adicional presente en la fuente EA y, por lo tanto, no se introduce ninguna.

## Notas
- Los filtros RSI utilizan la misma lógica de "ventana con excepciones" que el constructor MetaTrader. Si el número de velas disponibles es insuficiente, la señal comercial se ignora hasta que se recopile suficiente historial.
- Los valores de Heiken Ashi se almacenan en caché por vela para que los cambios de indicador (`Shift + CandlesShift`) coincidan con el comportamiento de los archivos `.mq5` originales.
- Configurar `Direction` en `BuyOnly` o `SellOnly` desactiva completamente el lado opuesto sin alterar sus parámetros, lo que ayuda durante la optimización.
