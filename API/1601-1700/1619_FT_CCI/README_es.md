# Estrategia FT CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port para StockSharp del asesor experto de MetaTrader 5 "FT_CCI (barabashkakvn's edition)". Utiliza el Commodity Channel Index (CCI) para capturar reversiones bruscas cuando el oscilador se aleja mucho de su media. El sistema replica la lógica original: cuando el CCI perfora la banda inferior cambia a largo, y cuando perfora la banda superior cambia a corto. Los valores opcionales de stop-loss y take-profit se introducen en pips y se convierten automáticamente en desplazamientos de precio.

## Descripción general
- **Indicador principal**: Commodity Channel Index con un período de promediado configurable (predeterminado 14).
- **Sesgo**: Largo/corto simétrico. La estrategia mantiene como máximo una posición neta y revierte en señales opuestas.
- **Ejecución**: Órdenes de mercado al cierre de las velas terminadas del marco temporal seleccionado.
- **Gestión del riesgo**: Distancias opcionales de stop-loss y take-profit expresadas en pips. Si alguno de los valores es cero, la protección correspondiente se desactiva.
- **Marco temporal predeterminado**: Velas de 30 minutos (refleja la selección de `Period()` en el experto original).

## Cómo funciona
### Configuración larga
1. Suscribirse a las velas terminadas del marco temporal seleccionado.
2. Actualizar el indicador CCI con valores de precio típico.
3. Cuando el último valor de CCI está en o por debajo del umbral inferior configurado (predeterminado -210):
   - Cerrar cualquier exposición corta abierta.
   - Entrar o añadir a una posición larga usando el volumen de operación configurado.
4. Mantener la posición hasta que se active una configuración corta opuesta, ocurra un evento de stop-loss/take-profit, o la estrategia se detenga manualmente.

### Configuración corta
1. Monitorear los mismos valores CCI en las velas terminadas.
2. Cuando el indicador está en o por encima del umbral superior (predeterminado +210):
   - Cerrar cualquier exposición larga abierta.
   - Entrar o añadir a una posición corta usando el volumen configurado.
3. Mantener el corto hasta que se active una condición larga opuesta o las órdenes de protección cierren la operación.

### Gestión de la operación
- Las distancias de stop-loss y take-profit se definen en pips. La estrategia los multiplica por el tamaño de pip detectado (paso de precio, multiplicado por 10 para símbolos forex de 3 y 5 dígitos) para obtener un desplazamiento de precio absoluto antes de habilitar `StartProtection` integrado de StockSharp.
- Dado que la protección se aplica una vez al inicio, cualquier nueva posición hereda inmediatamente los mismos valores de stop y objetivo relativos a su precio de ejecución.
- Los giros de posición se ejecutan mediante órdenes de mercado dimensionadas en `volumen configurado + |posición actual|`, asegurando que revertir una posición tanto cierra la exposición actual como abre la nueva en una sola transacción.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| **Candle Type** | Marco temporal usado para cálculos y generación de señales. |
| **Trade Volume** | Tamaño del lote para nuevas posiciones. Se usa junto con el valor de posición actual para dimensionar operaciones de reversión. |
| **CCI Period** | Longitud de promediado del Commodity Channel Index. |
| **CCI Upper Threshold** | Nivel CCI que activa entradas cortas. |
| **CCI Lower Threshold** | Nivel CCI que activa entradas largas. |
| **Stop Loss (pips)** | Distancia al stop de protección en pips. Establecer en 0 para deshabilitar. |
| **Take Profit (pips)** | Distancia al objetivo de beneficio en pips. Establecer en 0 para deshabilitar. |

Todos los parámetros admiten optimización a través del gestor de parámetros de StockSharp.

## Uso recomendado
- Funciona mejor en pares forex líquidos e índices donde las velas de 30 minutos a 4 horas producen extremos de CCI pronunciados.
- Los umbrales de ±210 recrean los valores predeterminados de FT_CCI. Los valores más bajos hacen el sistema más reactivo; los más altos se centran solo en las reversiones más extremas.
- Asegúrese de que los metadatos del instrumento expongan un `PriceStep` válido. El convertidor de pips depende de este valor para traducir pips en desplazamientos de precio.
- La estrategia asume un modelo de cuenta de compensación (posición neta única). Para cuentas de cobertura, establezca el volumen de operación apropiadamente para que las reversiones aplanen completamente la operación anterior.

## Notas
- El indicador debe estar completamente formado antes de que se considere cualquier señal de operación. Las primeras velas se ignoran hasta que el CCI tenga suficientes datos para emitir valores válidos.
- Las órdenes de stop-loss y take-profit son opcionales. Dejarlas en cero reproduce el comportamiento del asesor experto original que dependía únicamente de señales opuestas para las salidas.
- Añada la estrategia a un gráfico en StockSharp para visualizar velas, el indicador CCI y las operaciones ejecutadas; estas ayudas visuales se habilitan automáticamente en la implementación de C#.
