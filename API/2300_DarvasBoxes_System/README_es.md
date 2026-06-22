# Estrategia del Sistema de Cajas Darvas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia implementa un enfoque de ruptura basado en el concepto clásico de **Darvas Boxes**. Monitorea el movimiento del precio dentro de un rango dinámico (caja) calculado mediante el indicador **Donchian Channels**. Cuando el precio cierra por encima del límite superior de la caja, se abre una posición larga. Cuando el precio cierra por debajo del límite inferior, se abre una posición corta. Los niveles opcionales de stop-loss y take-profit proporcionan una gestión básica del riesgo.

## Cómo funciona

1. Para cada vela, el indicador Donchian Channels calcula los límites superior e inferior utilizando el `BoxPeriod` especificado.
2. La estrategia rastrea los valores anteriores de los límites para detectar rupturas.
3. Si el precio de cierre actual cruza por encima del límite superior anterior, la estrategia:
   - Cierra cualquier posición corta existente (si está permitido).
   - Abre una nueva posición larga (si está permitido).
4. Si el precio de cierre actual cruza por debajo del límite inferior anterior, la estrategia:
   - Cierra cualquier posición larga existente (si está permitido).
   - Abre una nueva posición corta (si está permitido).
5. Las posiciones activas se monitorizan para verificar las condiciones de stop-loss y take-profit.

## Parámetros

- **BoxPeriod** (`int`): Número de velas utilizadas para construir la caja de precio. El valor predeterminado es 20.
- **StopLoss** (`decimal`): Distancia desde el precio de entrada hasta el nivel de stop-loss. El valor predeterminado es 1000.
- **TakeProfit** (`decimal`): Distancia desde el precio de entrada hasta el nivel de take-profit. El valor predeterminado es 2000.
- **AllowBuyEntry** (`bool`): Habilita la apertura de posiciones largas. El valor predeterminado es `true`.
- **AllowSellEntry** (`bool`): Habilita la apertura de posiciones cortas. El valor predeterminado es `true`.
- **AllowBuyExit** (`bool`): Habilita el cierre de posiciones largas ante señales inversas o eventos de riesgo. El valor predeterminado es `true`.
- **AllowSellExit** (`bool`): Habilita el cierre de posiciones cortas ante señales inversas o eventos de riesgo. El valor predeterminado es `true`.
- **CandleType** (`DataType`): Tipo de velas utilizadas para los cálculos. El valor predeterminado son velas de 4 horas.

## Uso

1. Adjunte la estrategia a un valor y establezca los valores de parámetros deseados.
2. Inicie la estrategia. Se suscribirá a la serie de velas configurada y procesará los datos entrantes.
3. Las operaciones se ejecutan con órdenes de mercado cuando se cumplen las condiciones de ruptura.
4. Los niveles opcionales de stop-loss y take-profit gestionan las posiciones abiertas.

## Notas

- La estrategia utiliza la API de alto nivel con `BindEx` para conectar los valores del indicador y los datos de velas.
- Se evitan las colecciones internas; los valores del indicador se acceden a través del callback de enlace.
- Solo se procesan las velas completadas para garantizar señales fiables.
- Los comentarios dentro del código están en inglés, como se requiere.
