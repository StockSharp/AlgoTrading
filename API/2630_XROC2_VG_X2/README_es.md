# Estrategia XROC2 VG X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia XROC2 VG X2 es un sistema multi-marco temporal que combina dos streams suavizados de tasa de cambio. El marco temporal superior actúa como filtro direccional mientras que el inferior produce señales concretas de entrada y salida. El asesor experto original de MetaTrader 5 dependía del indicador personalizado XROC2_VG con opciones flexibles de suavizado y un módulo de gestión de capital. El port de StockSharp mantiene la lógica de señales intacta y expone los parámetros clave como entradas de la estrategia.

La estrategia se suscribe a dos series de velas:
- **Marco temporal superior** (predeterminado 6 horas) – establece la dirección de tendencia predominante.
- **Marco temporal inferior** (predeterminado 30 minutos) – genera entradas y salidas monitoreando cómo se cruzan las dos líneas ROC suavizadas.

Ambos streams comparten el mismo modo de cálculo de tasa de cambio pero usan configuraciones individuales de suavizado. Por defecto la estrategia aplica medias móviles Jurik, imitando la versión MQL. Los tipos de suavizado avanzados que no son directamente compatibles con StockSharp (JurX, ParMA, T3, VIDYA, AMA con control de fase) caen de vuelta a la implementación de media móvil más cercana disponible.

## Lógica de trading
1. **Detección de tendencia (marco temporal superior)**
   - Calcular dos valores ROC suavizados usando los períodos y métodos de suavizado configurados.
   - Evaluar el par de líneas en la barra definida por `HigherSignalBar`. Si la línea rápida está por encima de la lenta, la tendencia es alcista; de lo contrario, bajista. Una lectura neutral mantiene la tendencia actual en cero y deshabilita el trading.
2. **Generación de señales (marco temporal inferior)**
   - Calcular el mismo par de valores ROC suavizados en el marco temporal inferior.
   - Observar la barra terminada más reciente (desplazamiento `LowerSignalBar`) y la barra anterior. La combinación de estas dos barras determina si acaba de ocurrir un cruce.
   - Una configuración larga aparece cuando el marco temporal superior es alcista, la línea rápida cruzó por debajo de la lenta (cruce descendente) y los largos están habilitados.
   - Una configuración corta aparece cuando el marco temporal superior es bajista, la línea rápida cruzó por encima de la lenta (cruce ascendente) y los cortos están habilitados.
3. **Gestión de posiciones**
   - Cerrar posiciones largas cuando el cruce del marco temporal inferior indica caída (`CloseBuyOnLower`) o cuando la tendencia del marco temporal superior cambia a bajista (`CloseBuyOnTrendFlip`).
   - Cerrar posiciones cortas cuando el cruce del marco temporal inferior se vuelve alcista (`CloseSellOnLower`) o cuando la tendencia del marco temporal superior cambia a alcista (`CloseSellOnTrendFlip`).
   - Las nuevas operaciones se abren solo cuando no hay ninguna posición activa. El tamaño de la orden está controlado por la propiedad `Volume` de la estrategia.

## Parámetros
- `HigherCandleType` – tipo de vela para el filtro de tendencia (predeterminado marco temporal de 6 horas).
- `LowerCandleType` – tipo de vela para la generación de señales (predeterminado marco temporal de 30 minutos).
- `HigherSignalBar` – cuántas barras cerradas desplazar al leer valores del marco temporal superior (predeterminado 1).
- `LowerSignalBar` – cuántas barras cerradas desplazar al leer valores del marco temporal inferior (predeterminado 1).
- `HigherRocMode` / `LowerRocMode` – variante de cálculo de tasa de cambio (`Momentum`, `RateOfChange`, `RateOfChangePercent`, `RateOfChangeRatio`, `RateOfChangeRatioPercent`).
- `HigherFastPeriod`, `HigherFastMethod`, `HigherFastLength`, `HigherFastPhase` – configuración ROC rápido para el marco temporal superior.
- `HigherSlowPeriod`, `HigherSlowMethod`, `HigherSlowLength`, `HigherSlowPhase` – configuración ROC lento para el marco temporal superior.
- `LowerFastPeriod`, `LowerFastMethod`, `LowerFastLength`, `LowerFastPhase` – configuración ROC rápido para el marco temporal inferior.
- `LowerSlowPeriod`, `LowerSlowMethod`, `LowerSlowLength`, `LowerSlowPhase` – configuración ROC lento para el marco temporal inferior.
- `AllowBuyOpen`, `AllowSellOpen` – habilitar o deshabilitar apertura de largos y cortos.
- `CloseBuyOnTrendFlip`, `CloseSellOnTrendFlip` – forzar salidas cuando el marco temporal superior cambia de dirección.
- `CloseBuyOnLower`, `CloseSellOnLower` – salir cuando el cruce del marco temporal inferior va contra la posición.

## Notas de implementación
- La estrategia MQL original usaba una gran biblioteca de suavizado. La versión de StockSharp mapea las opciones soportadas a indicadores incorporados (SMA, EMA, SMMA/RMA, LWMA, Jurik, Kaufman AMA). Los modos no soportados (JurX, ParMA, T3, VIDYA) se aproximan con la media móvil más cercana disponible, por lo que el comportamiento puede diferir para esas combinaciones.
- Las funciones de gestión de capital, stop-loss, take-profit y configuraciones de deslizamiento de `TradeAlgorithms.mqh` no están reproducidas. En cambio, la estrategia opera con el `Volume` fijo especificado en la configuración de la estrategia.
- Las órdenes se ejecutan con órdenes de mercado. La lógica de protección como stop-losses o trailing stops puede añadirse a través de módulos de protección de StockSharp si es necesario.
- La estrategia solo opera cuando ambas suscripciones de velas están completamente formadas e `IsFormedAndOnlineAndAllowTrading()` devuelve verdadero.

## Consejos de uso
- Elegir tipos de velas que correspondan al estilo de trading original (p. ej., 6h/30m para swing trading). Son posibles otras combinaciones.
- Ajustar los períodos ROC y los métodos de suavizado para que coincidan con la capacidad de respuesta preferida. El suavizado Jurik mantiene el comportamiento más cercano al script fuente.
- Considerar añadir gestión de riesgo explícita (stop-loss, dimensionamiento de posición) cuando se opera en cuentas reales, ya que el port usa salidas de mercado simples.
