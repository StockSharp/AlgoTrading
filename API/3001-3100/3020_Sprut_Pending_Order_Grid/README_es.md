# Estrategia de Cuadrícula de Órdenes Pendientes Sprut
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La **Estrategia de Cuadrícula de Órdenes Pendientes Sprut** reproduce el asesor experto de MetaTrader 5 *Sprut (edición de barabashkakvn)* dentro del framework de estrategias de alto nivel de StockSharp. Construye una cuadrícula configurable de órdenes pendientes de compra y venta alrededor del precio actual del mercado y gestiona el tiempo de vida de cada orden, el escalado de volumen y la protección posterior al llenado usando los métodos auxiliares de StockSharp (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`).

La versión convertida mantiene la filosofía del asesor experto original:

* colocar la primera orden para cada dirección habilitada en un precio manual o en un desplazamiento automático medido en pips desde la mejor cotización;
* extender la cuadrícula paso a paso usando espaciado independiente para órdenes stop y límite;
* escalar volúmenes de órdenes usando un multiplicador que refleja la implementación de MT5;
* armar cada orden llenada con su propio stop-loss y take-profit, expresados como desplazamientos en pips desde el precio de entrada;
* aplicar puntos de control globales de ganancia y pérdida que inmediatamente liquidan posiciones y eliminan cualquier orden pendiente restante cuando se alcanzan;
* opcionalmente expirar órdenes pendientes después de un número de minutos especificado.

## Cómo Funciona la Estrategia
1. **Datos de mercado** – la estrategia se suscribe a actualizaciones del libro de órdenes para rastrear el mejor bid/ask y a velas (por defecto 1 minuto) para ejecutar mantenimiento periódico. No se requieren indicadores.
2. **Inicialización de la cuadrícula** – cuando no hay posición abierta ni orden de cuadrícula activa, la estrategia calcula el precio inicial para cada uno de los cuatro tipos posibles de órdenes:
   * **Buy Stop**: mejor ask + `DeltaFirstBuyStop` (a menos que `FirstBuyStop` sea distinto de cero).
   * **Buy Limit**: mejor bid − `DeltaFirstBuyLimit` (a menos que `FirstBuyLimit` sea distinto de cero).
   * **Sell Stop**: mejor bid − `DeltaFirstSellStop` (a menos que `FirstSellStop` sea distinto de cero).
   * **Sell Limit**: mejor ask + `DeltaFirstSellLimit` (a menos que `FirstSellLimit` sea distinto de cero).
   Cada desplazamiento se convierte desde pips usando el `PriceStep` del valor (alternativa: 0.0001).
3. **Apilamiento de órdenes** – para cada dirección habilitada la estrategia crea `CountOrders` entradas separadas por `StepStop` o `StepLimit` (también en pips). Los volúmenes siguen la fórmula original: la orden #0 usa el volumen base, mientras que la orden #N usa `baseVolume * N * coefficient` siempre que el coeficiente sea mayor que 1. Los volúmenes se ajustan para respetar `Security.VolumeStep`, `Security.MinVolume` y `Security.MaxVolume`.
4. **Expiración** – si `ExpirationMinutes` es positivo, la estrategia marca con tiempo cada orden pendiente y la cancela automáticamente después del plazo.
5. **Protección después del llenado** – cuando StockSharp informa que una orden de entrada está completada, la estrategia registra las órdenes de stop-loss y take-profit correspondientes (`StopLoss` y `TakeProfit` en pips). Una distancia de cero deshabilita la protección respectiva.
6. **Punto de control de ganancia** – el PnL realizado más no realizado se recalcula cuando llegan nuevos datos. Si `ProfitClose` es positivo y se alcanza, o `LossClose` (típicamente negativo) se viola, la estrategia solicita una liquidación completa: cierra el mercado de la posición, cancela todas las órdenes de cuadrícula y cancela las órdenes de protección restantes. El trading se reanuda automáticamente después de que todo esté plano.
7. **Mantenimiento continuo** – cada actualización limpia órdenes terminadas, elimina elementos expirados, intenta colocar una nueva cuadrícula cuando las condiciones lo permiten y evita rearmar durante una liquidación en progreso.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CountOrders` | Número de órdenes por dirección habilitada. | 5 |
| `FirstBuyStop`, `FirstBuyLimit`, `FirstSellStop`, `FirstSellLimit` | Precios absolutos opcionales para la primera orden en cada dirección (0 = usar desplazamiento automático). | 0 |
| `DeltaFirstBuyStop`, `DeltaFirstBuyLimit`, `DeltaFirstSellStop`, `DeltaFirstSellLimit` | Desplazamientos en pips aplicados al mejor bid/ask cuando se usa precio automático. | 15 |
| `UseBuyStop`, `UseBuyLimit`, `UseSellStop`, `UseSellLimit` | Habilitar o deshabilitar cada dirección de cuadrícula. | false |
| `StepStop`, `StepLimit` | Distancia entre órdenes stop o límite consecutivas (pips). | 50 |
| `VolumeStop`, `VolumeLimit` | Volumen base para la primera orden stop/límite. | 0.01 |
| `CoefficientStop`, `CoefficientLimit` | Multiplicador aplicado a órdenes adicionales (>1 mantiene el comportamiento de escalado MT5). | 1.6 |
| `ProfitClose` | Umbral de PnL total que activa la liquidación (unidades monetarias). | 10 |
| `LossClose` | Suelo de PnL total que activa la liquidación (unidades monetarias, típicamente negativo). | -100 |
| `ExpirationMinutes` | Tiempo de vida de la orden pendiente en minutos (0 = buena hasta cancelar). | 60 |
| `StopLoss`, `TakeProfit` | Distancias en pips para órdenes stop/take de protección creadas después de un llenado (0 deshabilita). | 50 / 0 |
| `CandleType` | Serie de velas usada para mantenimiento periódico. | Velas de 1 minuto |

## Notas de Uso
* Habilite al menos uno de los cuatro interruptores booleanos (`UseBuyStop`, `UseBuyLimit`, `UseSellStop`, `UseSellLimit`) para permitir que se cree la cuadrícula.
* La conversión de pips depende del `PriceStep` del valor. Los instrumentos con tamaños de tick exóticos pueden requerir ajustar los desplazamientos para un comportamiento equivalente.
* `ProfitClose`/`LossClose` comparan la suma del PnL realizado (`Strategy.PnL`) y el PnL no realizado calculado desde el último mejor bid/ask; asegúrese de que los metadatos de precio del paso estén completos para el instrumento operado.
* Las órdenes de protección stop y take son órdenes independientes de StockSharp; si cierra manualmente una posición fuera de la estrategia, las órdenes de protección restantes se cancelan cuando la posición neta vuelve a cero.
* El parámetro `CandleType` solo controla con qué frecuencia se ejecuta el mantenimiento; la colocación de órdenes todavía reacciona inmediatamente a las actualizaciones del libro de órdenes.

## Diferencias del Asesor Experto MT5
* La contabilidad de posiciones está en modo neto: StockSharp mantiene una sola posición neta por valor, similar al régimen neto de MT5.
* En lugar de los campos de stop-loss/take-profit incorporados de MT5 en las órdenes pendientes, las órdenes de protección de StockSharp se crean solo después de que se ejecuta una orden de entrada.
* La normalización de volumen usa `Security.VolumeStep`, `MinVolume` y `MaxVolume`; verifique estos valores cuando opere CFDs o exchanges de criptomonedas.
* La estrategia no expone un botón de *cerrar todo* separado — la rutina de liquidación es completamente automática a través de los umbrales de PnL, que coincide con la lógica del experto original donde `ProfitClose`/`LossClose` activaban un cierre completo.

## Primeros Pasos
1. Asigne la estrategia a un conector que suministre al menos datos del libro de órdenes y velas para el `CandleType` elegido.
2. Configure los cuatro interruptores direccionales y parámetros de volumen para que coincidan con su perfil de riesgo.
3. Defina distancias de stop-loss/take-profit cuando se requieren órdenes de protección (establecer en cero para deshabilitar).
4. Ajuste `ProfitClose`/`LossClose` a valores consistentes con la moneda de su cuenta.
5. Inicie la estrategia; esperará la primera instantánea del libro de órdenes antes de construir la cuadrícula.

> **Versión Python** – no proporcionada. Solo se incluye la implementación en C#, como se solicitó.
