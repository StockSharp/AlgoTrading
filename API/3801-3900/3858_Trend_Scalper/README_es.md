# Estrategia de revendedor de tendencias (API/3858)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
**TrendScalperStrategy** es una conversión de C# del MetaTrader 4 asesor experto `Currencyprofits_01_1.mq4`. El robot original es un revendedor liviano que sigue tendencias y combina un filtro cruzado EMA/SMA a corto plazo con entradas de ruptura alrededor de los máximos y mínimos más recientes. El puerto StockSharp mantiene las mismas reglas de decisión al tiempo que adopta la suscripción de velas de alto nivel y el canal de indicadores del marco.

## Lógica de trading
1. **Indicadores**
   - EMA rápida (predeterminado 6) en precios de cierre.
   - Lento SMA (predeterminado 12) en precios de cierre.
   - Máximo más alto (ventana 6 predeterminada) y mínimo más bajo (ventana 6 predeterminada) calculados a partir de los máximos y mínimos de las velas.
2. **Condiciones de entrada**
   - **Largo**: el precio avanza hacia la banda baja reciente (`Lowest Low`) mientras que el EMA rápido está por encima del SMA lento. La estrategia envía una orden de compra de mercado con el volumen definido por la regla de gestión del dinero.
   - **Corto**: el precio toca la banda alta reciente (`Highest High`) mientras que el EMA rápida está por debajo del lento SMA. Una orden de venta de mercado se coloca utilizando el mismo cálculo de volumen.
   - El sistema permanece plano mientras una posición está abierta, reflejando el comportamiento de orden única de la versión MQL.
3. **Condiciones de salida**
   - **Salida larga**: cuando en una posición larga abierta el máximo de la vela supera el `Highest High` registrado, la posición se cierra en el mercado.
   - **Salida corta**: cuando una posición corta abierta observa que el mínimo de la vela cae a través del `Lowest Low`, la posición corta se cubre en el mercado.
   - Se adjunta un stop-loss protector administrado por `StartProtection` a cada operación cuando `StopLossPoints` es mayor que cero.

## Gestión monetaria
La lógica de tamaño de lote reproduce los tres modos expuestos en el script MQL:

| Modo | Descripción | Comportamiento en el puerto |
|------|-------------|-----------------------|
| `0`  | Lotes fijos (`LotsIfNoMM`). | Devuelve el `FixedVolume` configurado. |
| `<0` | Lotes fraccionarios computados a partir del saldo de la cuenta y el factor de riesgo. | Calcula `ceil(balance * risk / 10000) / 10`, con un límite de 100 lotes. |
| `>0` | Escalado de lotes completos a partir del equilibrio y el factor de riesgo. | Utiliza la misma fórmula base, pero el resultado se redondea al siguiente número entero, con un mínimo de 1 lote y un límite de 100. |

El saldo se toma de `Portfolio.CurrentValue` (regresando a `BeginValue`). Si el valor de la cartera no está disponible, la estrategia vuelve al volumen fijo, por lo que las órdenes aún se emiten durante las pruebas retrospectivas.

## Gestión del riesgo
- **Stop-loss**: el parámetro `StopLossPoints` se expresa en puntos de precio (pips). Durante `OnStarted` la distancia se multiplica por `Security.PriceStep` y se pasa a `StartProtection`, permitiendo a StockSharp mantener la orden de protección.
- **Posición única**: la lógica aplica `Position == 0` antes de abrir una nueva operación, evitando posiciones superpuestas exactamente como el experto en MT4.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `CandleType` | plazo de 15 minutos | Serie de velas utilizadas para cálculos y señales de indicadores. |
| `FastLength` | 6 | Período de la EMA rápida. |
| `SlowLength` | 12 | Periodo de la lentitud SMA. |
| `BreakoutWindow` | 6 | Número de velas inspeccionadas para detectar el filtro de ruptura más alto/más bajo. |
| `FixedVolume` | 0,1 lotes | Volumen cuando la administración del dinero está deshabilitada o se requiere respaldo. |
| `MoneyManagementMode` | 0 | Selecciona entre tamaño de lote fijo, fraccionario o redondeado. |
| `MoneyManagementRisk` | 40 | Multiplicador de factor de riesgo utilizado en el tamaño de lote basado en el equilibrio. |
| `StopLossPoints` | 50 | Distancia de stop-loss en puntos de precio (convertida a precio absoluto antes de llamar a `StartProtection`). |

## Notas de implementación
- El encadenamiento de indicadores se basa en el flujo de trabajo de alto nivel `SubscribeCandles().Bind(...)`; no se requiere almacenamiento en búfer en serie manual.
- Los comentarios en el código se agregaron en inglés para cumplir con las pautas del repositorio.
- No se modificaron pruebas unitarias; El foco de esta conversión es la estrategia y la documentación que la acompaña.

## Consejos de uso
- Seleccione un intervalo de vela que coincida con el entorno comercial original (por ejemplo, marcos de tiempo intradiarios cortos para el especulación).
- Asegúrese de que la cartera tenga un `PriceStep` válido para que la conversión de stop-loss a precio absoluto funcione correctamente.
- Ajuste `MoneyManagementRisk` con cuidado: los valores más altos conducen a posiciones más grandes debido al cálculo `ceil(balance * risk / 10000)` heredado del experto MQL.
