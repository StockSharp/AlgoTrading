# Estrategia del comerciante de noticias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el comportamiento del script original **NewsTrader.mq4** al armar ambos lados del mercado poco antes de una publicación macroeconómica programada. Diez minutos antes de la marca de tiempo de noticias configurada, el robot envía un par de órdenes de detención de ruptura e inmediatamente coloca salidas protectoras cuando se activa un lado.

## Lógica principal

- Utiliza una suscripción de vela de 1 minuto (configurable) únicamente como fuente de sincronización.
- Calcula el momento de activación como `news time - LeadMinutes` y espera hasta la primera vela terminada cuyo tiempo de apertura esté en ese punto o más allá.
- Coloca un stop de venta por debajo del precio actual y un stop de compra por encima de él, compensado por `BiasPips` convertido a través de `Security.PriceStep` (refleja la lógica `bias * Point` en MQL4).
- Una vez que se completa una orden pendiente, se cancela la orden pendiente opuesta; Las órdenes dedicadas de stop-loss y take-profit se colocan utilizando las distancias de pips configuradas.
- Las acciones de stop-loss o take-profit cancelan la orden de protección restante y aplanan la estrategia.
- Llama a `StartProtection()` al inicio para que la estrategia coopere con salvaguardas de nivel superior StockSharp.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TradeVolume` | Contratos presentados con cada pedido pendiente. | `1` |
| `StopLossPips` | Distancia de stop-loss en pips (0 desactiva la orden de stop). | `10` |
| `TakeProfitPips` | Distancia de obtención de beneficios en pips (0 desactiva la orden objetivo). | `10` |
| `BiasPips` | Distancia desde el precio de referencia hasta las órdenes stop de ruptura. | `20` |
| `LeadMinutes` | Minutos antes de la marca de tiempo de las noticias cuando se activan las órdenes de ruptura. | `10` |
| `NewsYear`, `NewsMonth`, `NewsDay`, `NewsHour`, `NewsMinute` | Componentes del horario informativo programado (reloj de plataforma). | `2010`, `3`, `8`, `1`, `30` |
| `CandleType` | Tipo de datos de vela utilizado para realizar un seguimiento de la progresión del tiempo. | `1 Minute` |

## Notas de implementación

- La estrategia establece `Volume` en `TradeVolume` durante `OnStarted`, lo que garantiza que los métodos auxiliares como `BuyStop` y `SellStop` utilicen el tamaño esperado.
- `Security.PriceStep` debe estar definido; de lo contrario, la lógica genera una excepción porque las distancias basadas en pips no se pueden traducir a precios.
- Los precios de cierre de velas se utilizan como indicador de la última oferta/demanda al calcular los niveles de parada, coincidiendo con la lógica MQL4 original que se basaba en la cotización más reciente en el momento de activación.
- Los pedidos pendientes se realizan sólo una vez; el algoritmo no se rearma después de que pasa el evento de noticias configurado.
- Las órdenes de protección se omiten cuando su respectiva distancia de pip es cero, lo que mantiene el comportamiento configurable para intervención manual.

## Archivos

- `CS/NewsTraderStrategy.cs` — Implementación en C# de la estrategia.

La versión de Python se omite intencionalmente según lo solicitado.
