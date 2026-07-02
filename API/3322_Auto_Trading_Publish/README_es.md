# Estrategia Auto Trading Publish
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Esta estrategia porta la utilidad de MetaTrader 4 **"Auto Trading Publish"** a StockSharp. En lugar de enviar órdenes de mercado, se centra en controlar cuándo se permite operar. Supervisa el reloj de mercado mediante una suscripción de velas y cambia la bandera `AutoTradingActive` cuando se alcanza la hora configurada de inicio o parada. La bandera replica el comportamiento de la utilidad original, que alternaba programáticamente el botón "AutoTrading" de MT4.

## Lógica de negociación
- Suscribirse a un flujo ligero de velas (por defecto, velas de un minuto) para seguir la hora de mercado incluso si no se realizan operaciones.
- Cuando una vela terminada informa la `StartHour` configurada, activar `AutoTradingActive` y registrar el evento.
- Cuando una vela terminada informa la `StopHour` configurada, desactivar `AutoTradingActive` y registrar el evento.
- Suprimir alternancias duplicadas dentro de la misma hora para que el log no se sature si llegan varias velas o ticks durante esa hora.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `StartHour` | Hora del día (0-23) que activa el trading automático. |
| `StopHour` | Hora del día (0-23) que desactiva el trading automático. |
| `CandleType` | Marco temporal usado para consultar el reloj de mercado. Marcos menores reaccionan más rápido. |

## Notas de uso
- La estrategia no envía órdenes; solo expone la propiedad `AutoTradingActive`, que otras estrategias o paneles pueden observar para decidir cuándo enviar operaciones.
- Cuando la hora de inicio y parada son iguales, el evento de parada se ejecuta después del inicio, dejando el trading desactivado, idéntico al asesor experto original.
- Elija un marco de velas que coincida con la rapidez requerida para el cambio. Un marco de un minuto ofrece buen equilibrio entre respuesta y uso de recursos.

## Diferencias frente a MetaTrader
- MT4 alternaba un botón global de plataforma mediante mensajes de Windows. StockSharp expone en cambio una bandera a nivel de estrategia, facilitando la integración con configuraciones complejas.
- El port StockSharp se ejecuta íntegramente dentro de la API de alto nivel, lo que facilita combinarlo con gráficos u otras estrategias auxiliares sin hooks de mensajes de bajo nivel.
