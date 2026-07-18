# Estrategia de Farhad Hill Versión 2 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una versión StockSharp del asesor experto MetaTrader “Farhad Hill Versión 2”.
Combina múltiples filtros de indicadores para negociar cambios de tendencia en símbolos de Forex. el
la lógica convertida conserva la pila de indicadores original (MACD, Stochastic, Parabolic SAR,
Momentum y cruce de media móvil opcional) y su gestión de dinero más seguimiento
comportamiento.

La estrategia funciona en un solo período de tiempo (velas predeterminadas de 30 minutos) y abre solo una
posición a la vez. Se incluyen estilos de stop-loss protector, take-profit y tres trailing-stop.
compatible para reflejar la versión MQL. Todos los comentarios en el código se proporcionan en inglés como
solicitado.

## Lógica de trading
- Filtro **MACD**: cuando está habilitado, los largos requieren MACD línea principal debajo de la línea de señal;
los cortos requieren MACD principal por encima de la línea de señal.
- **Stochastic filtro de nivel**: demanda de posiciones largas %K por debajo del umbral inferior, demanda de posiciones cortas
%K por encima del umbral superior. Cuando el filtro cruzado opcional está habilitado, una tendencia alcista
Se requiere un cruce %K/%D (de abajo hacia arriba) para posiciones largas y un cruce bajista para posiciones cortas.
- Filtro **Parabolic SAR**: las posiciones largas requieren SAR por debajo del precio con un paso hacia abajo
(anterior SAR superior al actual); los cortos requieren SAR por encima del precio con un precio al alza
paso. La conversión utiliza como referencia los precios de velas cerradas.
- **Filtro de impulso**: calculado sobre los precios de apertura de velas, que coinciden con la configuración de MQL.
Los largos necesitan impulso por debajo del umbral inferior, los cortos necesitan impulso por encima del umbral superior
umbral.
- **Cruce de media móvil (opcional)**: tipo de MA configurable, precio aplicado y períodos.
Los largos necesitan el MA rápido por encima del MA lento; los cortos necesitan la relación inversa.

La estrategia solo evalúa señales en velas terminadas y omite nuevas entradas cuando una
existe una posición abierta. Las entradas se realizan con órdenes de mercado utilizando el lote calculado.
tamaño.

## Gestión de Puestos
- **Stop-loss/Take-profit** – especificado en pips. Un pip se deriva del valor del instrumento.
`PriceStep`, recurriendo a `0.0001` si no está disponible.
- **Tipos de paradas dinámicas**
  1. Inmediato: una vez que el precio supera la distancia del stop, el stop sigue al precio.
  2. Retrasado: espera a que el precio se mueva la distancia final desde la entrada anterior
siguiendo un desplazamiento fijo.
  3. Tres etapas: reproduce la lógica original de tres niveles con dos pasos de equilibrio
y una distancia final de seguimiento.
- Las órdenes de protección se colocan con `SellStop`/`BuyStop` (para stop-loss) y
`SellLimit`/`BuyLimit` (para obtener ganancias) para que permanezcan visibles en el intercambio.

## Gestión monetaria
- **Lote fijo**: negocia el volumen fijo configurado. Si `AccountIsMini` está habilitado, muchos
se convierten al tamaño de minilote con un mínimo de 0,1.
- **Riesgo porcentual**: replica la fórmula original
`floor(FreeMargin * percent / 10000) / 10`, limitado por el límite `MaxLots` y ajustado
para cuentas mini cuando sea necesario. Si el valor de la cartera no está disponible, la estrategia
vuelve al lote fijo.

## Parámetros
Todos los parámetros están expuestos a través de objetos `StrategyParam<T>` y se pueden optimizar o
cambiado desde la interfaz de usuario. Grupos clave:

| grupo | Parámetro | Descripción |
| --- | --- | --- |
| generales | `CandleType` | Plazo de las velas utilizadas para las señales. |
| Gestión del dinero | `AccountIsMini`, `UseMoneyManagement`, `TradeSizePercent`, `FixedVolume`, `MaxLots` |
| Riesgo | `StopLossPips`, `TakeProfitPips`, `UseTrailingStop`, `TrailingStopType`, `TrailingStopPips`, `FirstMovePips`, `TrailingStop1`, `SecondMovePips`, `TrailingStop2`, `ThirdMovePips`, `TrailingStop3` |
| Indicadores | `UseMacd`, `UseStochasticLevel`, `UseStochasticCross`, `UseParabolicSar`, `UseMomentum`, `UseMovingAverageCross`, `MacdFast`, `MacdSlow`, `MacdSignal`, `StochasticK`, `StochasticD`, `StochasticSlowing`, `StochasticHigh`, `StochasticLow`, `MomentumPeriod`, `MomentumHigh`, `MomentumLow`, `SlowMaPeriod`, `FastMaPeriod`, `MaMode`, `MaPrice` |

## Notas y suposiciones
- Las comparaciones Parabolic SAR utilizan el precio de cierre de la vela para aproximar las comprobaciones de oferta/demanda
de MT4. Esto mantiene la lógica determinista sobre los datos históricos.
- La gestión del dinero requiere una cartera conectada para obtener capital actual; de lo contrario
Se utiliza el volumen fijo.
- Las combinaciones de indicadores se procesan únicamente en velas completadas, evitando cambios prematuros.
señales sobre datos parciales.

## Archivos
- `CS/FarhadHillVersion2Strategy.cs` – Implementación C# de la estrategia.
- `README.md` – Este documento.
- `README_ru.md` – traducción al ruso.
- `README_zh.md` – Traducción al chino.
