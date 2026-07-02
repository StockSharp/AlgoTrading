# ErrorEstrategia EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia ErrorEA** es un puerto StockSharp del asesor MetaTrader `errorEA.mq4`. El experto original comparó los componentes +DI y -DI del índice direccional promedio y siguió acumulando órdenes de mercado en la dirección de la tendencia detectada mientras aplicaba un stop-loss de seguridad muy grande y una toma de ganancias ajustada. Esta versión de C# recrea la misma idea con el nivel alto API de StockSharp, agrega controles de parámetros claros y documenta el modelo de riesgo explícitamente.

## Lógica comercial
1. Suscríbase al período de tiempo configurado (`CandleType`) y alimente un indicador `AverageDirectionalIndex` con las velas entrantes.
2. Espere hasta que la vela esté completamente cerrada y ADX produzca un valor final para esa barra.
3. Compare las líneas +DI y -DI:
   - si +DI > -DI, la estrategia trata el mercado como alcista;
   - si -DI > +DI, el mercado se considera bajista;
   - valores iguales no generan nuevas señales.
4. Ante una señal alcista:
   - aplanar una posición neta corta existente (StockSharp utiliza cuentas de compensación, por lo que se cierran las coberturas opuestas);
   - Si el número de operaciones escaladas largas aún es inferior a `MaxTrades`, envíe una orden de compra de mercado más con el volumen devuelto por el bloque de control de riesgo.
5. Ante una señal bajista:
   - cerrar una posición larga existente;
   - si el número de tramos cortos es inferior a `MaxTrades`, envíe una orden de venta de mercado con la misma lógica de tamaño de posición.
6. Las órdenes de protección son gestionadas por `StartProtection`:
   - `StopLossPoints` se convierte en pasos de precio y funciona como un stop fijo amplio, al igual que la entrada `StopLoss` en MetaTrader;
   - si `EnableTakeProfit` es verdadero, `TakeProfitPoints` replica el pequeño objetivo de especulación que EA aplicó a través de `OrderModify`.
7. Los contadores de posición (`_longTrades`/`_shortTrades`) se reinician cada vez que la posición neta vuelve a cero o gira hacia el lado opuesto, lo que garantiza que el límite de ampliación se aplique en los stop-outs y reversiones.

## Gestión de riesgos y dimensionamiento.
- `BaseVolume` refleja la entrada `MiniLots` de MetaTrader. Actúa como el tamaño del lote inicial para cada operación.
- Cuando `EnableRiskControl` es verdadero, la estrategia reproduce la fórmula `PowerRisk` original: `volume = BaseVolume * max(1, PortfolioValue / RiskDivider)`. El divisor predeterminado (`10000`) coincide con la implementación de MQL.
- Después de aplicar la fórmula, el resultado está limitado por `MinVolume`, `MaxVolume`, los límites de intercambio (`Security.MinVolume`, `Security.MaxVolume`) y el paso de volumen (`Security.VolumeStep`). Esto evita que el EA solicite un tamaño que el lugar rechazaría.
- El tamaño calculado se utiliza para cada nuevo orden de escala mientras la dirección correspondiente permanece dentro del límite `MaxTrades`.

## Parámetros
| Nombre | Tipo | Predeterminado | MetaTrader contraparte | Descripción |
| --- | --- | --- | --- | --- |
| `AdxPeriod` | `int` | `14` | `iADX(..., 14, ...)` | Período de suavizado del Índice Direccional Promedio. |
| `CandleType` | `DataType` | plazo de 15 minutos | cronograma del gráfico | Serie de velas utilizada para todos los cálculos. |
| `MaxTrades` | `int` | `9` | `MaxTrades` | Número máximo de pedidos escalables por dirección. |
| `EnableRiskControl` | `bool` | `true` | `RiskControl` | Habilita el cálculo dinámico de lotes en función del valor de la cartera. |
| `BaseVolume` | `decimal` | `0.15` | `MiniLots` | Tamaño del lote base antes de aplicar el multiplicador de riesgo. |
| `RiskDivider` | `decimal` | `10000` | implícito (divisor en `PowerRisk`) | Divisor que se aplica al valor de la cartera cuando el control de riesgos está activo. |
| `MaxVolume` | `decimal` | `3` | `MaxLot` | Límite para el volumen calculado automáticamente (antes del redondeo cambiario). |
| `MinVolume` | `decimal` | `0.01` | `MarketInfo(..., MODE_MINLOT)` | Volumen mínimo permitido en el pedido final. |
| `StopLossPoints` | `int` | `1000` | `StopLoss` | Distancia de stop-loss en pasos de precio. Establezca en `0` para desactivar la parada. |
| `EnableTakeProfit` | `bool` | `true` | `ScalpeControl` | Permite la obtención de beneficios mediante especulación estricta. |
| `TakeProfitPoints` | `int` | `10` | `ScalpeProfit` | Distancia de obtención de beneficios en pasos de precio. |

## Diferencias con el asesor experto original
- La versión MetaTrader contenía un error que sobrescribía el valor +DI con el valor -DI. El puerto StockSharp compara los componentes correctos, reflejando el comportamiento previsto de la estrategia.
- MetaTrader permite cobertura. StockSharp opera en un entorno de compensación, por lo que el puerto cierra la exposición opuesta antes de agregar nuevas operaciones en la dirección de la señal.
- La detección de deslizamiento (`GetSlippage`) y la salida de comentarios se eliminaron porque StockSharp maneja internamente el deslizamiento de pedidos y las cadenas de riesgo eran puramente cosméticas.
- Las modificaciones de órdenes (`OrderModify`) se reemplazan con una única llamada `StartProtection`, que cubre distancias de stop-loss y take-profit con redondeo según el tipo de cambio.

## Consejos de uso
- Asegúrese de que la seguridad tenga metadatos `PriceStep`, `VolumeStep`, `MinVolume` y `MaxVolume` adecuados para que el ajuste de volumen integrado pueda funcionar correctamente.
- Alinee `BaseVolume`, `MinVolume` y `MaxVolume` con el instrumento con el que opera. El constructor también asigna el volumen base ajustado a `Strategy.Volume`, lo que hace que las acciones manuales en la interfaz de usuario sean consistentes con los pedidos automatizados.
- Aumente el período de tiempo o ADX período en el que las señales +DI/-DI se vuelven demasiado ruidosas; La lógica de escalado funciona mejor durante tendencias constantes.
- Desactive `EnableTakeProfit` si prefiere dejar que el stop-loss salga de la posición en lugar de arrancar pequeñas ganancias.
