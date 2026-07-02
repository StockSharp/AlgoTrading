# Estrategia TwoPerBar Ron
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El experto original en MetaTrader "TwoPerBar" de Ron Thompson abre **dos órdenes de mercado al comienzo de cada nueva barra**: una larga y otra corta. Siempre que un tramo alcanza un objetivo de efectivo fijo (`ProfitMade * Point` en el código MQL) se cierra y, en la apertura de la siguiente barra, cualquier exposición restante se liquida antes de que se cree un nuevo par cubierto. Si la barra anterior terminó con posiciones abiertas, el tamaño del lote se duplica hasta un límite de seguridad (`LotLimit`). El puerto StockSharp reproduce este comportamiento utilizando la estrategia de alto nivel API, cotizaciones de nivel 1 para monitoreo de oferta/demanda y seguimiento explícito de los dos tramos cubiertos.

## Flujo de trabajo comercial
1. **Detección de barras**: `SubscribeCandles(CandleType)` notifica a la estrategia cuando finaliza la serie de velas configuradas. Una vela completa marca el comienzo de una nueva barra, al igual que el cambio `Time[0]` de MetaTrader.
2. **Inspección de ganancias**: las instantáneas de nivel 1 (oferta/demanda) se monitorean continuamente. Tan pronto como la mejor oferta o demanda se aleja lo suficiente del precio de entrada registrado, el tramo coincidente se cierra con `SellMarket` o `BuyMarket`.
3. **Liquidación forzada**: al inicio de una nueva barra, cualquier tramo superviviente se cierra en el mercado. Esto refleja el bucle `OrderClose` en el script MQL.
4. **Escalado de volumen**: cuando el ciclo anterior tenía operaciones activas, el tamaño del lote se multiplica por `VolumeMultiplier` (predeterminado `2`). De lo contrario, se restablece a `BaseVolume`. El valor se normaliza con respecto al paso de volumen del instrumento y se fija mediante `MaxVolume` y el intercambio `Security.MaxVolume`.
5. **Creación de cobertura**: se envían dos órdenes de mercado a través de `BuyMarket` y `SellMarket`. Cada tramo recuerda su volumen objetivo, el tamaño real llenado y el precio promedio ponderado de llenado para que las verificaciones de ganancias funcionen con información precisa.

## Gestión de riesgos y dinero.
- **Martingale escalado de estilo**: duplicar el lote después de un ciclo sin terminar imita el tamaño original tipo martingala. Cuando ambas piernas se cierran durante la barra, la secuencia se reinicia al lote base.
- **Objetivos de ganancias por tramo**: `ProfitTargetPoints` traduce la entrada MetaTrader `ProfitMade`. El valor se multiplica por el tamaño de los puntos del instrumento y se compara con la oferta/demanda para decidir cuándo salir de un tramo.
- **Cumplimiento del intercambio**: `NormalizeVolume` garantiza que los lotes generados respeten el instrumento `VolumeStep` y `MinVolume`. Los valores sobredimensionados desencadenan un restablecimiento a una cantidad negociable.
- **Contabilidad cubierta**: la estrategia mantiene su propia lista de tramos, porque las carteras StockSharp normalmente exponen solo posiciones netas. Esto permite que los entornos que admiten cuentas cubiertas sigan el mismo comportamiento.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | velas de 1 minuto | Periodo de tiempo principal que indica cuándo ha comenzado una nueva barra. |
| `BaseVolume` | `decimal` | `0.1` | Tamaño de lote inicial para un ciclo nuevo. |
| `VolumeMultiplier` | `decimal` | `2` | Multiplicador aplicado después de que una barra termina con posiciones abiertas. |
| `MaxVolume` | `decimal` | `12.8` | Techo duro para el tamaño del lote martingala. |
| `ProfitTargetPoints` | `decimal` | `19` | Objetivo de beneficio expresado en puntos; multiplicado por el tamaño de puntos del instrumento y comparado con las cotizaciones de oferta y demanda. |

## Diferencias con la versión MQL
- Utiliza `SubscribeLevel1()` en lugar de valores globales `Bid`/`Ask` tick por tick, pero mantiene la misma lógica basada en las mejores comillas.
- Los pedidos se envían a través de StockSharp métodos auxiliares (`BuyMarket`, `SellMarket`), por lo que todo el redondeo específico del intercambio se realiza automáticamente.
- El manejo de volúmenes respeta `VolumeStep`, `MinVolume` y `MaxVolume`, mientras que el script original funcionaba con valores dobles sin formato.
- El puerto StockSharp almacena información del tramo internamente; Los conectores que se ejecutan en modo de compensación aún pueden aplanar las coberturas, así que confirme que su corredor admita posiciones opuestas.

## Consejos de uso
- Haga coincidir `BaseVolume` con un tamaño de lote válido para el instrumento seleccionado; de lo contrario, el paso de normalización omitirá la negociación.
- Mantenga `ProfitTargetPoints` alineado con el tamaño en puntos del símbolo; rara vez se alcanzarán valores excesivamente grandes dentro de una sola barra.
- Debido a que la estrategia envía órdenes de mercado opuestas, ejecútela en fuentes de datos de demostración o cuentas de cobertura antes de pasar a entornos de producción.
- Adjunte la estrategia a un gráfico: `OnStarted` agrega velas y operaciones ejecutadas al gráfico visual para facilitar el seguimiento.
