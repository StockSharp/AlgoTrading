# Estrategia Contratendencia XFatl XSatl Cloud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de StockSharp recrea el experto MT5 **Exp_XFatlXSatlCloud**. Observa la "nube" FATL/SATL suavizada y opera **en contra** de la dirección de su cruce. Cuando la línea rápida (XFATL) cae de nuevo por debajo de la línea lenta (XSATL) después de haber estado por encima, la estrategia abre una posición larga. Cuando la línea rápida sube de nuevo por encima después de haber estado por debajo, abre una posición corta. Los niveles opcionales de stop loss y take profit se expresan en pasos de precio del instrumento.

## Lógica de trading

- La fuente de datos predeterminada es un marco temporal de 8 horas. Otros tipos de velas pueden seleccionarse con el parámetro `CandleType`.
- Se construyen dos pipelines de suavizado a partir de medias móviles de StockSharp. Por defecto ambas usan una media móvil Jurik con longitud y fase configurables. También están disponibles familias de suavizado alternativas (SMA, EMA, SMMA, WMA).
- Las señales se evalúan en la barra definida por `SignalBar` (desplazamiento en barras desde la última vela cerrada). La estrategia almacena una ventana rodante de valores recientes del indicador para que los últimos y anteriores valores puedan compararse igual que la versión MT5.
- Reglas de entrada (contrario):
  - **Largo** – la línea rápida estaba por encima de la línea lenta en la barra anterior y ahora ha cruzado hacia ella o por debajo.
  - **Corto** – la línea rápida estaba por debajo en la barra anterior y ahora ha cruzado hacia ella o por encima.
- Reglas de salida:
  - Las posiciones largas se cierran cuando la barra anterior mostró una nube bajista (rápida por debajo de lenta) y `AllowLongExit` está habilitado.
  - Las posiciones cortas se cierran cuando la barra anterior mostró una nube alcista (rápida por encima de lenta) y `AllowShortExit` está habilitado.
- Una nueva posición solo se abre una vez que la posición anterior se ha cerrado completamente, reflejando el comportamiento del asesor experto original.

## Gestión de riesgos

- `TradeVolume` controla la cantidad usada para las órdenes de mercado. La estrategia nunca escala — cada nueva posición usa el mismo tamaño.
- `TakeProfitTicks` y `StopLossTicks` se convierten directamente en distancias de paso de precio y se conectan al módulo de protección integrado de StockSharp. Configúrelos en cero para deshabilitar la orden protectora correspondiente.
- Dado que el experto MT5 dependía de cálculos de gestión de dinero específicos del broker, esta versión reemplaza esa lógica con parámetros explícitos de volumen y protección.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Tipo de vela o marco temporal usado para los cálculos del indicador. |
| `FastMethod` / `SlowMethod` | Familia de suavizado para XFATL y XSATL (Jurik por defecto). |
| `FastLength` / `SlowLength` | Longitudes de período para los filtros rápido y lento. |
| `FastPhase` / `SlowPhase` | Entradas de fase reenviadas a la media móvil Jurik cuando se admite. |
| `SignalBar` | Desplazamiento de barra usado al evaluar cruces (1 = barra anterior). |
| `TradeVolume` | Tamaño de orden para entradas. |
| `AllowLongEntry` / `AllowShortEntry` | Habilitar o deshabilitar entradas contrarias en cada dirección. |
| `AllowLongExit` / `AllowShortExit` | Permitir que el indicador cierre posiciones abiertas en señales opuestas. |
| `TakeProfitTicks` | Distancia al objetivo de take-profit expresada en pasos de precio. |
| `StopLossTicks` | Distancia al stop protector en pasos de precio. |

## Notas de implementación

- La estrategia mantiene colas cortas de salidas recientes del indicador y las recorta a la longitud mínima requerida por `SignalBar`. No se crean buffers históricos adicionales.
- El soporte de fase de Jurik se configura vía reflexión para que la estrategia siga siendo compatible con diferentes versiones de StockSharp. Si el indicador subyacente carece de una propiedad `Phase`, el valor simplemente se ignora.
- Solo se usa el precio de cierre de cada vela, coincidiendo con la configuración más común para el experto original. Extender la lógica a tipos de precio alternativos requeriría aumentar la estrategia.
- Se usan componentes de la API de alto nivel (`SubscribeCandles`, `Bind`, `StartProtection`) a lo largo, por lo que la estrategia se integra limpiamente con Designer y otros productos de StockSharp.
