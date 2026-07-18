# Estrategia Exp FisherCG Oscilador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto **Exp_FisherCGOscillator** de MetaTrader 5 a la API de alto nivel de StockSharp. Recrea el oscilador Fisher Center of Gravity y su línea de disparo, evalúa señales en una barra histórica configurable, y reproduce el flujo de stop/take original con órdenes de StockSharp y ayudantes de riesgo.

## Cómo funciona

1. **Cadena de indicadores** – cada vela terminada se pasa por el oscilador Fisher CG: los precios medianos alimentan un bucle de centro de gravedad, los valores se normalizan sobre las últimas `Length` barras, y una transformación de Fisher produce la línea del oscilador. La línea de disparo es simplemente el oscilador retrasado una barra.
2. **Extracción de señales** – la estrategia inspecciona dos lecturas históricas definidas por `SignalBar`. Abre un largo cuando el valor más antiguo del oscilador (`SignalBar + 1`) está por encima de su disparo mientras el valor más nuevo (`SignalBar`) cruza de nuevo por encima del disparo, señalando un giro alcista. Los cortos reflejan esta lógica en el lado bajista.
3. **Manejo de salidas** – las salidas largas ocurren tan pronto como el oscilador más antiguo cae por debajo de su disparo, mientras que las salidas cortas se activan cuando sube por encima del disparo, coincidiendo con los indicadores de cierre inmediato del EA. Las entradas opuestas cierran la posición activa antes de revertir.
4. **Procesamiento barra a barra** – todo se ejecuta en velas completadas desde `CandleType`; no se generan operaciones intrabar, asegurando backtests deterministas y coincidiendo con la puerta de "nueva barra" del EA.

## Gestión de riesgo y dimensionamiento de posición

- **Stops/objetivos** – `StopLossPoints` y `TakeProfitPoints` se expresan en pasos del instrumento y se traducen en distancias de precio absolutas a través de `Security.PriceStep`.
- **Control de volumen** – `SizingMode = FixedVolume` envía el `FixedVolume` constante. `SizingMode = PortfolioShare` convierte `DepositShare` del valor actual del portafolio en contratos usando el último cierre y `VolumeStep`.
- **Posición única** – la estrategia siempre aplana antes de entrar en el lado opuesto, evitando posiciones hedgeadas simultáneas.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Marco temporal suscrito para velas y cálculos de indicadores. |
| `Length` | Período del oscilador Fisher CG (también usado para la ventana de normalización). |
| `SignalBar` | Número de velas cerradas hacia atrás usadas para leer señales; `1` coincide con el valor por defecto del EA. |
| `AllowLongEntry` / `AllowShortEntry` | Alternar entradas largas/cortas. |
| `AllowLongExit` / `AllowShortExit` | Alternar salidas automáticas para posiciones largas/cortas. |
| `StopLossPoints` / `TakeProfitPoints` | Distancias de stop de protección y objetivo en pasos de precio. Establecer en `0` para deshabilitar. |
| `FixedVolume` | Volumen usado en el modo de dimensionamiento fijo. |
| `DepositShare` | Fracción del portafolio asignada por operación en el modo `PortfolioShare`. |
| `SizingMode` | Elige entre volumen fijo y dimensionamiento basado en participación. |

## Notas de uso

- Alinee `CandleType` y `SignalBar` con el marco temporal usado por el indicador original (H8 y desplazamiento de barra de 1 por defecto).
- Permita un breve período de calentamiento para que el oscilador tenga suficiente historial para formarse; la estrategia ignora las operaciones hasta que el indicador esté completamente inicializado.
- Los stops y objetivos operan en el cierre de la vela. Ajuste los valores de puntos para que coincidan con el tamaño del tick de su instrumento.
- Cuando se selecciona el dimensionamiento `PortfolioShare`, asegúrese de que la valoración del portafolio esté disponible; de lo contrario, la estrategia vuelve al volumen fijo.

## Diferencias vs EA original

- Las órdenes se envían como órdenes de mercado sin el parámetro de deslizamiento `Deviation_`; StockSharp maneja la ejecución con su propia configuración de deslizamiento.
- La gestión monetaria se simplifica a dos modos de dimensionamiento (`FixedVolume` y `PortfolioShare`). Las opciones de porcentaje de pérdida del EA se omiten intencionalmente.
- Las marcas de tiempo de órdenes pendientes (`UpSignalTime`/`DnSignalTime`) no se usan. Las señales se ejecutan inmediatamente en la vela procesada.
