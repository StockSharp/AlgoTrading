# Estrategia ColorMetroDuplexStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

`ColorMetroDuplexStrategy` es una conversión en C# del experto de MetaTrader 5 **Exp_ColorMETRO_Duplex**. El robot original utiliza dos instancias independientes del indicador ColorMETRO para gestionar los módulos de trading largo y corto. Cada módulo opera con su propia suscripción de velas, evalúa dos envoltorios RSI escalonados producidos por el indicador ColorMETRO, y opcionalmente abre o cierra posiciones cuando los envoltorios rápido y lento se cruzan.

La versión de StockSharp mantiene ambos módulos y reproduce las mismas reglas de evaluación de señales, utilizando la API de alto nivel para suscripciones de velas, gestión de órdenes y vinculación de indicadores. Se incluye un `ColorMetroIndicator` personalizado para imitar la implementación de iCustom de MT5, exponiendo las bandas rápida y lenta de ColorMETRO junto con el valor RSI interno.

## Cómo funciona

1. Se crean dos instancias de `SignalModule` — **Long** y **Short** — cada una con su propia serie de velas, configuración de ColorMETRO y opciones de gestión de operaciones.
2. Cuando la estrategia se inicia, cada módulo se suscribe a su marco temporal configurado y vincula el `ColorMetroIndicator` mediante `SubscribeCandles(...).BindEx(...)`.
3. Para cada vela finalizada el indicador produce:
   - La banda rápida de ColorMETRO (envoltorio RSI rápido).
   - La banda lenta de ColorMETRO (envoltorio RSI lento).
   - El valor RSI subyacente (usado solo como referencia).
4. El módulo almacena el historial del indicador y evalúa los últimos dos valores usando el desplazamiento `SignalBar` configurado (correspondiendo a la lógica de `CopyBuffer` de MT5).
5. Reglas de trading:
   - **Módulo largo**
     - *Abrir*: la banda rápida estaba por encima de la banda lenta en la barra anterior y ahora está por debajo o igual.
     - *Cerrar*: la banda lenta estaba por encima de la banda rápida en la barra anterior.
   - **Módulo corto**
     - *Abrir*: la banda rápida estaba por debajo de la banda lenta en la barra anterior y ahora está por encima o igual.
     - *Cerrar*: la banda lenta estaba por debajo de la banda rápida en la barra anterior.
6. Las órdenes se enrutan mediante `BuyMarket` / `SellMarket`. Se respeta la posición neta actual — las operaciones opuestas cierran la exposición existente antes de abrir una nueva.

## Parámetros

Cada módulo expone un grupo de parámetros dedicado. Los valores predeterminados reflejan el experto de MT5.

### Parámetros de mercado compartidos

- **Long_Volume**, **Short_Volume** — tamaño de operación (lotes) usado para nuevas entradas.
- **Long_OpenAllowed**, **Short_OpenAllowed** — habilitar o deshabilitar la apertura de operaciones para el módulo.
- **Long_CloseAllowed**, **Short_CloseAllowed** — habilitar o deshabilitar las salidas automáticas.
- **Long_MarginMode**, **Short_MarginMode** — modo de gestión de dinero mantenido para compatibilidad (sin efecto en este port).
- **Long_StopLoss**, **Long_TakeProfit**, **Long_Deviation**, **Short_StopLoss**, **Short_TakeProfit**, **Short_Deviation** — reservados para documentación; los stops y el control de slippage no están automatizados en esta versión.
- **Long_Magic**, **Short_Magic** — números mágicos originales de MT5 preservados como referencia.

### Parámetros del indicador

- **Long_CandleType**, **Short_CandleType** — marco temporal para cada módulo ColorMETRO.
- **Long_PeriodRSI**, **Short_PeriodRSI** — longitud RSI usada dentro del algoritmo ColorMETRO.
- **Long_StepSizeFast**, **Short_StepSizeFast** — paso (en puntos RSI) para el envoltorio rápido.
- **Long_StepSizeSlow**, **Short_StepSizeSlow** — paso para el envoltorio lento.
- **Long_SignalBar**, **Short_SignalBar** — desplazamiento de barra usado al leer los búferes del indicador (idéntico a la entrada `SignalBar` de MT5).
- **Long_AppliedPrice**, **Short_AppliedPrice** — fuente de precio para el cálculo RSI (precio de cierre por defecto).

## Diferencias respecto a MT5

- **Modelo de posición** — las estrategias de StockSharp trabajan con la posición neta. El experto original almacenaba posiciones separadas mediante números mágicos; el port cierra la exposición actual antes de abrir el lado opuesto.
- **Gestión de dinero** — los modos de margen y la configuración de desviación se conservan como parámetros pero no se aplican automáticamente. Use las entradas de `Volume` para controlar el tamaño.
- **Stop-loss / take-profit** — el experto MT5 colocaba stops de protección con cada orden. La versión de StockSharp mantiene las distancias como parámetros de referencia, pero las órdenes de stop reales deben implementarse por separado si es necesario.
- **Control de nivel de tiempo** — el código MT5 usaba variables globales para asegurar solo una operación por tiempo de señal. En StockSharp procesamos cada vela finalizada una vez y nos apoyamos en la verificación de posición neta para prevenir entradas duplicadas.

## Notas

- El `ColorMetroIndicator` personalizado reproduce la lógica de MT5, incluyendo los envoltorios RSI escalonados y la memoria de tendencia. Expone las bandas rápida/lenta y el RSI interno para gráficos o depuración.
- Los comentarios dentro del código son intencionalmente detallados para aclarar las decisiones de portado y asistir con mayor personalización.
- Para habilitar la automatización de stop-loss o take-profit, extienda `SignalModule.ProcessModule` para colocar órdenes protectoras usando los controles de riesgo de StockSharp.
