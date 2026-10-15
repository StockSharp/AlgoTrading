# Estrategia ASCPlusPlus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de ruptura **ASC++ Williams** transfiere el experto heredado MQL4 "ASC++.mq4" al API de alto nivel de StockSharp. La lógica busca rangos de negociación estrechos confirmados por el oscilador Williams %R y luego coloca órdenes stop ligeramente más allá de los extremos de las velas. Una vez activada, la gestión de riesgos integrada mantiene la posición protegida con toma de ganancias automática, stop loss y comportamiento de seguimiento opcional.

## Cómo funciona la estrategia

1. **Preparación de indicadores**
   - Los osciladores %R Williams rápidos y lentos (por defecto, 9 y 54 períodos) miden el impulso a corto plazo.
   - Un rango verdadero promedio de 10 períodos suaviza el cálculo del rango ponderado "ASC".
   - Los umbrales dinámicos `x1 = 67 + RiskLevel` y `x2 = 33 - RiskLevel` imitan las bandas de sobrecompra/sobreventa adaptativas originales.
2. **Puntuación de señal**
   - Cada vela terminada calcula `value2 = 100 - |%R_fast|`. Los valores por debajo de `x2` indican un entorno de sobreventa con presión para romper al alza; los valores superiores a `x1` indican una condición de sobrecompra que puede romperse a la baja.
   - Velas consecutivas que permanecen dentro de los mismos contadores de confirmación de incremento extremo. Se permite una operación solo después de `SignalConfirmation` barras consecutivas (5 por defecto) para aproximarse a los `SigVal` temporizadores originales.
3. **Realización de pedidos**
   - Cuando el filtro de rango (`ATR < EntryRange`) confirma la consolidación y el impulso coincide (`%R_fast` por encima/por debajo de `%R_slow`), la estrategia coloca una orden de parada:
     - Compre stop en `High + ATR * 0.5 + EntryStopLevel * PriceStep` para rupturas alcistas.
     - Stop de venta en `Low - ATR * 0.5 - EntryStopLevel * PriceStep` para rupturas bajistas.
   - Las órdenes pendientes del lado opuesto se cancelan para evitar exposiciones conflictivas.
4. **Gestión de posiciones**
   - Las órdenes de protección se configuran a través de `StartProtection` (takeprofit y stop loss expresados en puntos, seguimiento opcional habilitado cuando `TrailingStopPoints > 0`).
   - Si una nueva señal entra en conflicto con una posición existente (por ejemplo, una ruptura alcista en corto), el motor inmediatamente aplana la exposición opuesta antes de poner en cola la orden de ruptura, al igual que la fuente EA.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | plazo de 15 minutos | Fuente de vela base utilizada para los cálculos. |
| `FastLength` | 9 | Williams Longitud %R utilizada para el detector de impulso rápido. |
| `SlowLength` | 54 | Williams Longitud %R utilizada para el oscilador de confirmación. |
| `RangeLength` | 10 | ATR ventana de suavizado que reemplaza el bucle de rango ponderado manual. |
| `EntryStopLevel` | 10 puntos | Compensación adicional (en incrementos de precio) agregada a las órdenes stop de ruptura. |
| `EntryRange` | 27 puntos | Rango promedio máximo permitido antes de aceptar una configuración. |
| `RiskLevel` | 3 | Ajusta los umbrales `x1`/`x2`, haciendo que las bandas de confirmación sean más estrechas o más amplias. |
| `SignalConfirmation` | 5 barras | Número de velas consecutivas que deben permanecer en el mismo extremo antes de que se arme una orden. |
| `TakeProfitPoints` | 100 puntos | Distancia de la orden de toma de ganancias automática. |
| `StopLossPoints` | 40 puntos | Distancia de la orden automática de stop-loss. |
| `TrailingStopPoints` | 20 puntos | Habilita el comportamiento de seguimiento cuando es mayor que cero. |

## Notas de conversión

- El EA original construyó un ATR ponderado manualmente; el puerto StockSharp utiliza el indicador nativo `AverageTrueRange` con la misma retrospectiva de 10 períodos. Esto coincide con la intención de suavizar y al mismo tiempo evita los búferes personalizados.
- Los temporizadores `SigValBuy` y `SigValSell` en el código MQL dependían de contadores basados en minutos. La versión C# los emula con `SignalConfirmation` comprobaciones de velas consecutivas para mantener la cadencia de entrada constante sin acceder a marcas de tiempo de minutos.
- Las órdenes de entrada pendientes se implementan con `BuyStop`/`SellStop` ayudantes. Antes de realizar un nuevo pedido, se cancela el lado opuesto, reflejando la lógica heredada `OrderDelete`.
- La gestión de paradas se basa en `StartProtection`, que gestiona automáticamente la obtención de beneficios, la parada de pérdidas y el seguimiento. Esto cubre la escalera final MQL (`TSLevel1`, `TSLevel2`) de una manera simplificada pero sólida.
- Todo el acceso a los indicadores se realiza a través de suscripciones y enlaces de alto nivel, según lo exigen las pautas del proyecto, sin llamadas `GetValue` manuales ni cachés de indicadores personalizados.

## Consejos de uso

- La estrategia espera instrumentos con un `PriceStep` definido; de lo contrario, el valor predeterminado es `1`. Ajuste `EntryStopLevel`, `EntryRange` y los parámetros de riesgo para que coincidan con el tamaño de tick del instrumento.
- Reduzca `SignalConfirmation` para realizar operaciones más agresivas en períodos de tiempo más cortos, o auméntelo para operar solo con consolidaciones pronunciadas.
- Considere habilitar el dibujo de gráficos en una aplicación host para visualizar las órdenes stop y confirmar que los niveles de ruptura se alinean con los máximos y mínimos recientes.
- Pruebe siempre con datos históricos porque la estrategia es muy sensible a las definiciones de diferenciales, deslizamientos y pasos de precios específicos del corredor.
