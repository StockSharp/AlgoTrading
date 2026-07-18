# Estrategia de reversión RPoint 250
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de reversión RPoint 250** es una StockSharp versión del MetaTrader 4 asesor experto `e_RPoint_250`. El robot original
se basa en un indicador personalizado llamado *RPoint* que resalta el máximo y el mínimo más recientes. Porque ese indicador es
no disponible en StockSharp, la conversión reproduce el mismo comportamiento con los indicadores integrados `Highest` y `Lowest`.
Cada vez que un nuevo extremo reemplaza al previamente detectado, la estrategia inmediatamente invierte la posición y restaura la misma.
Lógica de stop-loss, take-profit y trailing definida en la versión MQL.

## Flujo de trabajo comercial

1. Suscríbase a la serie de velas especificada por `CandleType` (predeterminado: velas de 5 minutos).
2. Realice un seguimiento del máximo y mínimo móvil de las últimas `ReversePoint` barras. Estos valores representan los niveles de RPoint emulados.
3. Si el precio alcanza un nuevo máximo, cierre cualquier posición larga y abra una posición corta con volumen `OrderVolume`.
4. Si el precio imprime un nuevo mínimo, cierre cualquier posición corta y abra una posición larga con volumen `OrderVolume`.
5. Aplicar órdenes de protección usando `StartProtection`. Las distancias de stop-loss y take-profit se expresan en puntos de precio mediante
los parámetros `StopLossPoints` y `TakeProfitPoints`.
6. Opcionalmente, siga las ganancias por `TrailingStopPoints`. El motor de seguimiento mide hasta qué punto se ha movido el precio a favor del
posición y la cierra cuando el precio retrocede en el número de puntos configurado.
7. Recuerde el tiempo de la vela de la última entrada exitosa para evitar abrir múltiples operaciones dentro de la misma barra, coincidiendo con el
`TimeN` salvaguarda del script MQL.

La estrategia siempre mantiene como máximo una posición abierta. Cierra las operaciones existentes antes de entrar en la dirección opuesta y
nunca se amplía.

## Parámetros

| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|---------|-------------|
| `OrderVolume` | `decimal` | `0.1` | Volumen enviado con cada orden de mercado. Refleja la entrada `Lots` en la versión MetaTrader. |
| `TakeProfitPoints` | `decimal` | `15` | Distancia a la orden de toma de ganancias medida en puntos de precio. Establezca en `0` para deshabilitar los objetivos de ganancias. |
| `StopLossPoints` | `decimal` | `999` | Distancia al tope de protección expresada en puntos de precio. Establezca en `0` para operar sin un tope fijo. |
| `TrailingStopPoints` | `decimal` | `0` | Distancia de seguimiento opcional en puntos de precio. Cuando es cero, la lógica de seguimiento está desactivada. |
| `ReversePoint` | `int` | `250` | Número de velas consideradas al buscar el último máximo y mínimo. Los valores más altos suavizan el ruido. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Agregación de velas analizada por la estrategia. Cámbielo para que coincida con el período de tiempo del gráfico utilizado en MetaTrader. |

## Notas de implementación

- `Highest` y `Lowest` están vinculados a la suscripción de velas a través del `Bind` API de alto nivel, por lo que no hay colas de indicadores manuales.
requerido.
- `StartProtection` reproduce las distancias originales de stop-loss y take-profit en unidades de precio absoluto. StockSharp maneja el
colocación del pedido una vez que aparece una nueva posición.
- Los trailingstops se implementan monitoreando cada vela completada. Cuando el precio retrocede según el número configurado de puntos desde
el mejor precio alcanzado después de la entrada, la posición se cierra con una orden de mercado.
- La clase almacena los niveles de reversión ejecutados más recientes (`_executedHighLevel` y `_executedLowLevel`) para evitar duplicados.
entradas. Esto es equivalente a las variables `Reverse_High` / `Reverse_Low` en el código MQL.
- El campo `_lastSignalTime` refleja la variable `TimeN` y bloquea múltiples órdenes dentro de la misma vela, evitando
presentaciones dobles accidentales en mercados ilíquidos.

## Pautas de uso

1. Adjunte la estrategia a una cartera que admita el instrumento y el tipo de vela seleccionados.
2. Ajuste `OrderVolume` para cumplir con el tamaño del contrato y las reglas de gestión de riesgos de su corredor.
3. Ajuste `ReversePoint` para que coincida con la volatilidad del activo negociado. Los valores más altos producen menos cambios pero más significativos.
4. Verifique que `StopLossPoints`, `TakeProfitPoints` y `TrailingStopPoints` sean compatibles con el valor `PriceStep`.
5. Ejecute una prueba retrospectiva en StockSharp Designer o Backtester para confirmar el comportamiento antes de operar con capital real.
6. Supervise la salida del registro: los mensajes informativos resaltarán los cambios de posición y pueden ayudar a validar la conversión.

Debido a que el indicador RPoint se aproxima con componentes integrados, se observan diferencias menores con respecto a la ejecución de MetaTrader.
posible en datos históricos con lagunas o diferentes reglas de redondeo. Valide siempre los resultados con sus propios feeds de datos de mercado
antes de confiar en la estrategia en producción.
