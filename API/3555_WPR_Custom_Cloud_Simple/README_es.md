# Estrategia simple de nube personalizada de WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia simple de nube personalizada de WPR** es una StockSharp versión del MetaTrader asesor experto `WPR Custom Cloud Simple.mq5`. El EA monitorea el oscilador %R de Larry Williams' y abre operaciones cuando el indicador sale del territorio de sobreventa o sobrecompra. Esta versión de C# mantiene el diseño original de operar solo con velas nuevas, invirtiendo la posición cuando aparece una señal opuesta y evita cualquier orden de stop-loss o take-profit exactamente igual que la implementación de referencia.

## Lógica comercial
1. Suscríbase al período de tiempo configurado (`CandleType`) y alimente un indicador `WilliamsR` con las velas entrantes.
2. Espere hasta que se acabe la vela; la estrategia nunca actúa sobre barras incompletas.
3. Almacene los dos últimos valores %R completados. Reflejan las lecturas de `wpr[1]` y `wpr[2]` de MetaTrader.
4. Generar señales en cruces:
   - **Configuración larga**: la barra anterior cierra por encima de `OversoldLevel` mientras que la barra anterior estaba por debajo del nivel. Esto recrea la condición de "salida de sobreventa" (`wpr[2] < level` y `wpr[1] > level`) del EA.
   - **Configuración breve**: la barra anterior se cierra debajo de `OverboughtLevel` mientras que la barra anterior estaba encima, coincidiendo con la marca original `wpr[2] > level` y `wpr[1] < level`.
5. Cuando aparezca una configuración larga, aplane cualquier exposición corta y compre un volumen neto. Cuando se activa una configuración corta, aplana el lado largo y vende un volumen neto. Debido a que StockSharp funciona con posiciones netas, enviar `BuyMarket`/`SellMarket` con `Volume + |Position|` replica perfectamente el flujo de cierre e inversión de la cuenta de cobertura de MetaTrader.
6. No se utilizan salidas adicionales; un nuevo cruce opuesto es la única forma de cerrar operaciones, como en el asesor original.

## Parámetros
| Nombre | Tipo | Predeterminado | MetaTrader contraparte | Descripción |
| --- | --- | --- | --- | --- |
| `WprPeriod` | `int` | `14` | `Inp_WPR_Period` | Longitud retrospectiva para el cálculo de Williams %R. |
| `OverboughtLevel` | `decimal` | `-20` | `Inp_WPR_Level1` | Umbral que define el territorio de sobrecompra. Cruzar por debajo de él activa los cortos. |
| `OversoldLevel` | `decimal` | `-80` | `Inp_WPR_Level2` | Umbral que define el territorio de sobreventa. Cruzar por encima de él activa posiciones largas. |
| `CandleType` | `DataType` | plazo de 1 hora | `InpWorkingPeriod` | Serie de velas utilizada para actualizar el indicador y evaluar señales. |
| `Volume` | `decimal` | Volumen base de la estrategia | `InpLots` | Tamaño de lote para órdenes de mercado. La estrategia compensa automáticamente la posición neta actual antes de abrir una nueva operación. |

## Diferencias con el EA original
- StockSharp opera con posiciones netas. El cierre de la exposición opuesta se gestiona aumentando el volumen de la orden de mercado, por lo que el comportamiento coincide con el modelo de cobertura sin estructuras contables adicionales como `STRUCT_POSITION`.
- Todas las clases de ayuda de gestión de pedidos (`CTrade`, `CPositionInfo`, comprobaciones de margen, etc.) se reemplazan por los controles de riesgo integrados de StockSharp. La estrategia se basa en `Strategy.Volume` y los metadatos del intercambio en lugar de cálculos manuales de margen libre.
- El registro se simplifica. La versión StockSharp evita declaraciones detalladas `Print` porque la versión de alto nivel API ya proporciona actualizaciones del estado del pedido.
- Las órdenes de protección se omiten intencionalmente para reflejar el diseño de "cerca en señal opuesta" de la fuente EA.

## Consejos de uso
- Ajuste `CandleType` al mismo período de tiempo que utilizó en MetaTrader para mantener comparable la frecuencia de cruce.
- Williams Los umbrales %R son valores negativos. Acercar `OverboughtLevel` a cero hace que las entradas cortas sean más raras, mientras que acercar `OversoldLevel` hacia `-100` hace que las entradas largas sean más raras.
- La estrategia supone que `Volume` ya está alineado con el paso mínimo y las reglas de compensación del corredor. Ajuste el volumen base en la interfaz de usuario o mediante código antes de comenzar a operar en vivo.
