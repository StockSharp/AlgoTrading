# Estrategia Ultra MFI de Gestión Monetaria Recontada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Ultra MFI MMRec** es un port directo del asesor experto de MetaTrader 5 `Exp_UltraMFI_MMRec`. Combina un oscilador de Índice de Flujo de Dinero (MFI) suavizado en múltiples pasos con gestión del dinero basada en rachas. Dos contadores internos acumulan cuántas capas de suavizado apuntan hacia arriba o hacia abajo. Los cruces entre estos contadores generan señales de trading, mientras que los resultados recientes de las operaciones determinan si la siguiente posición usa el tamaño de posición normal o reducido.

## Lógica de trading
1. **Indicador base** – se calcula un Índice de Flujo de Dinero con longitud configurable en el tipo de vela seleccionado.
2. **Suavizado escalonado** – el valor MFI se pasa a través de una escalera de medias móviles. Cada paso aumenta la longitud de suavizado en un incremento fijo. Los métodos de suavizado admitidos son Simple, Exponencial, Suavizado, Linealmente Ponderado y medias móviles Jurik (otros modos específicos de MT5 no están disponibles en StockSharp).
3. **Contadores direccionales** – para cada barra la estrategia compara la salida actual y anterior de cada paso de suavizado. Si el paso está subiendo, el contador alcista aumenta, de lo contrario aumenta el bajista. Ambos contadores se suavizan de nuevo por una media móvil final.
4. **Desplazamiento de señal** – las reglas de trading operan en barras terminadas. Un `SignalShift` configurable le indica a la estrategia cuántas velas cerradas mirar hacia atrás al comparar los contadores, imitando el comportamiento de MT5 al usar `SignalBar=1`.
5. **Entradas y salidas** –
   * Si la barra anterior mostró toros más fuertes (`bulls > bears`) y la última barra muestra un cruce a `bulls < bears`, la estrategia abre una posición larga. La misma condición también cierra cualquier posición corta abierta.
   * Si la barra anterior mostró osos más fuertes y la última barra cambia a `bulls > bears`, la estrategia abre una posición corta y cierra cualquier posición larga abierta.
   * El stop-loss y take-profit opcionales (basados en porcentaje) pueden gestionarse a través de `StartProtection`.
6. **Gestión del dinero** – el siguiente tamaño de orden depende de los últimos resultados de operaciones por dirección. Después de cerrar cada posición se inspecciona el PnL realizado:
   * La estrategia almacena las operaciones de compra más recientes `BuyTotalTrigger` y cuenta cuántas fueron pérdidas. Cuando el conteo alcanza `BuyLossTrigger`, la siguiente orden de compra usa `ReducedVolume`, de lo contrario usa `NormalVolume`.
   * La misma lógica se aplica independientemente para las operaciones de venta con `SellTotalTrigger` y `SellLossTrigger`.

## Parámetros
- **CandleType** – tipo de datos del instrumento (marco temporal) usado para la generación de señales.
- **MfiPeriod** – longitud del oscilador Índice de Flujo de Dinero.
- **StepSmoothing / FinalSmoothing** – tipo de media móvil para los pasos de la escalera y los contadores finales.
- **StartLength / StepSize / StepsTotal** – geometría de la escalera de suavizado (primera longitud, incremento, número de pasos).
- **FinalSmoothingLength** – longitud de la etapa de suavizado del contador.
- **SignalShift** – número de barras completadas a mirar hacia atrás al evaluar señales.
- **NormalVolume / ReducedVolume** – tamaño de operación para condiciones normales y después de una racha perdedora.
- **BuyTotalTrigger / BuyLossTrigger** – profundidad de historial y umbral de pérdida para cambiar la siguiente operación larga a tamaño reducido.
- **SellTotalTrigger / SellLossTrigger** – configuraciones análogas para operaciones cortas.
- **AllowLongEntries / AllowShortEntries / AllowLongExits / AllowShortExits** – habilitar o deshabilitar entradas y salidas para cada dirección.
- **TakeProfitPercent / StopLossPercent** – niveles de protección opcionales basados en porcentaje.

## Notas de uso
- El suavizado escalonado requiere suficientes velas históricas para llenar cada media móvil. Espere hasta que la estrategia esté completamente formada antes de confiar en las señales.
- Dado que StockSharp no proporciona suavizadores específicos de MT5 como JurX, Parabolic, VIDYA o AMA, se usan las alternativas compatibles más cercanas. El suavizado Jurik es un buen valor predeterminado que reproduce la sensación original del indicador UltraMFI.
- La gestión del dinero se basa en el PnL realizado. Asegúrese de que sus backtests incluyan la ejecución de órdenes para que el PnL realizado se actualice después de cada cierre de posición.
- Este port mantiene el comportamiento de solo entrar en nuevas posiciones cuando la posición actual es plana. Cuando aparece una señal de reversión mientras se mantiene la posición opuesta, la estrategia primero sale de la operación existente y entrará en la siguiente barra elegible una vez que esté plana.
