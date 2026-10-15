# Estrategia de Stochastic CG Oscillator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto MetaTrader 5 **Exp_StochasticCGOscillator** a StockSharp. La conversión mantiene la lógica original del oscilador Stochastic Center of Gravity, reconstruye el suavizado de la línea de disparo y ejecuta operaciones usando la API de estrategia de alto nivel de StockSharp.

## Cómo funciona

1. **Pipeline de indicadores** – cada vela finalizada de `CandleType` alimenta el oscilador Stochastic CG personalizado. Los precios medios impulsan un bucle center-of-gravity, los valores se normalizan sobre las últimas `Length` barras, y una ventana deslizante ponderada produce la línea del oscilador. La línea de disparo se recrea mediante el mismo suavizado `0.96 * (previous + 0.02)` que aplica el EA.
2. **Muestreo de señal** – la estrategia inspecciona dos lecturas históricas separadas por `SignalBar`. Se prepara una compra cuando la lectura más antigua (desplazamiento `SignalBar + 1`) está por encima del trigger mientras la más reciente (desplazamiento `SignalBar`) cruza por debajo. Los cortos reflejan la lógica en dirección opuesta.
3. **Gestión de posición** – las posiciones largas se cierran en cuanto la lectura más antigua cae por debajo del trigger, mientras que las posiciones cortas salen cuando la lectura más antigua sube por encima de él. Cuando aparece una nueva entrada en el lado opuesto, la posición actual se aplana antes de enviar la orden de reversión.
4. **Manejo de riesgo** – las distancias opcionales de stop-loss y take-profit se expresan en pasos del instrumento y se evalúan sobre el precio de cierre de cada vela procesada. Reflejan los inputs protectores del EA sin depender de órdenes pendientes.
5. **Control de calentamiento** – la estrategia espera hasta que el indicador esté completamente inicializado (suficiente historial para el bucle CG y el buffer de suavizado de cuatro valores) antes de emitir señales, garantizando backtests deterministas.

## Gestión de riesgo y dimensionamiento de posición

- **Stops/objetivos** – `StopLossPoints` y `TakeProfitPoints` se traducen en distancias absolutas usando `Security.PriceStep`. Un valor de `0` deshabilita el límite respectivo.
- **Posición activa única** – el algoritmo nunca mantiene exposición larga y corta al mismo tiempo. Las señales opuestas activan un cierre explícito antes de entrar en la nueva dirección.
- **Dimensionamiento de posición** – `SizingMode = FixedVolume` siempre opera con `FixedVolume`. `SizingMode = PortfolioShare` convierte `DepositShare` del valor del portafolio en contratos usando el último cierre y `Security.VolumeStep`.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Marco temporal suscrito para velas y cálculos de indicadores. |
| `Length` | Período del oscilador Stochastic CG (afecta las ventanas CG y de normalización). |
| `SignalBar` | Número de velas cerradas atrás usadas para evaluar señales (`1` reproduce el valor predeterminado del EA). |
| `AllowLongEntry` / `AllowShortEntry` | Activa/desactiva entradas largas/cortas. |
| `AllowLongExit` / `AllowShortExit` | Activa/desactiva salidas automáticas para posiciones largas/cortas. |
| `StopLossPoints` / `TakeProfitPoints` | Distancias protectoras en pasos de precio. Establezca en `0` para deshabilitar. |
| `FixedVolume` | Tamaño de orden cuando el modo de dimensionamiento es volumen fijo. |
| `DepositShare` | Fracción del portafolio usada en el dimensionamiento basado en participación. |
| `SizingMode` | Elige entre volumen fijo y dimensionamiento de posición basado en participación. |

## Notas de uso

- Alinee `CandleType` con el marco temporal usado por el indicador original (H8 en la versión MQL). Valores de `SignalBar` más grandes requieren un calentamiento más largo porque el buffer de historial del indicador debe cubrir el desplazamiento.
- Los stops y objetivos actúan sobre los cierres de velas; no son órdenes intrabarra. Ajuste los valores de puntos para adaptarse al tamaño del tick del instrumento.
- Cuando el dimensionamiento `PortfolioShare` está habilitado, asegúrese de que la valoración del portafolio esté disponible; de lo contrario la estrategia recurre al volumen fijo.
- El indicador produce valores en el rango `[-1, 1]` como la implementación original, permitiendo reutilizar filtros familiares basados en umbrales si se desea.

## Diferencias con el EA original

- Las órdenes de mercado se envían inmediatamente sin el parámetro `Deviation_`; el manejo del deslizamiento se delega a la capa de ejecución de StockSharp.
- La gestión de dinero se simplifica a dos modos (`FixedVolume` y `PortfolioShare`). Las opciones de dimensionamiento adicionales basadas en margen del EA no se reproducen.
- Las marcas de tiempo de órdenes pendientes (`UpSignalTime` / `DnSignalTime`) son innecesarias porque las estrategias de StockSharp trabajan en velas completadas y se ejecutan sincrónicamente.
