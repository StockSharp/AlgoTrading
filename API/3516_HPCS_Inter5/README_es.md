# Estrategia HPCS Inter5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia HPCS Inter5** es un script de impulso de un solo disparo convertido del MetaTrader 4 experto `_HPCS_Inter5_MT4_EA_V01_WE`. Cuando comienza la estrategia, inspecciona las últimas velas completadas y, si el precio de cierre de hace cinco barras es más alto que el cierre más reciente, envía una orden de compra de mercado. Las distancias protectoras opcionales de stop-loss y take-profit emulan el comportamiento basado en pips del EA original.

## Lógica de trading

1. Suscríbase a la serie de velas configuradas y mantenga los últimos seis cierres completos.
2. Una vez que se llene el búfer, compare el cierre de hace cinco barras con el último cierre (`Close[5] > Close[1]` en términos MetaTrader).
3. Si se cumple la condición y aún no se ha realizado ninguna operación, envíe una orden de compra de mercado con el volumen configurado.
4. Las órdenes de protección se activan una vez al inicio hasta `StartProtection`, utilizando la conversión de pips estilo MetaTrader: los instrumentos con 3 o 5 decimales multiplican `PriceStep` por 10 para determinar el tamaño del pip; de lo contrario, se utiliza el `PriceStep` sin procesar.

La estrategia no abre operaciones adicionales e ignora todas las señales posteriores una vez que se llena la primera posición.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Candle Type` | marco de tiempo de 1 minuto | Tipo de vela utilizada para recoger los precios de cierre. Configúrelo en el período de tiempo que coincida con el intervalo de señal deseado. |
| `Stop Loss (pips)` | 10 | Distancia del stop-loss de protección en MetaTrader pips. Un valor de `0` desactiva la parada. |
| `Take Profit (pips)` | 10 | Distancia para la toma de ganancias protectora en MetaTrader pips. Un valor de `0` deshabilita la toma de ganancias. |
| `Trade Volume` | 1 | Volumen de la orden de mercado presentada cuando se activa la condición de entrada. |

## Notas de implementación

- La estrategia requiere un `Security.PriceStep` (o `Security.Step`) configurado para convertir distancias de pips. Si falta esta información, las compensaciones de protección permanecen inactivas pero la señal de entrada aún funciona.
- Solo se procesan velas terminadas (`CandleStates.Finished`) para que coincidan con el comportamiento MetaTrader que se basa en `Close[1]` y valores anteriores.
- El buffer interno mantiene exactamente seis cierres sin utilizar el historial del indicador, respetando la naturaleza minimalista de la fuente EA.
- `IsFormedAndOnlineAndAllowTrading()` se verifica antes de enviar la orden para garantizar que el entorno esté listo para la ejecución.

## Consejos de uso

1. Asigne un instrumento Forex con la configuración adecuada de precio y volumen.
2. Ajuste el `Candle Type` para que coincida con el período de tiempo que desea analizar.
3. Deje el stop-loss o el take-profit en cero si prefiere gestionar las salidas manualmente.
4. Reinicie la estrategia siempre que desee volver a evaluar la condición de entrada porque solo se activa una vez por sesión.
