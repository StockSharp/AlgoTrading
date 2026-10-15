# Estrategia eInTradePanel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia eInTradePanel automatiza el flujo de trabajo del panel de operaciones original de MetaTrader. Permite los mismos ocho modos de orden (mercado, stop, límite y stop-límite en ambas direcciones) mientras calcula automáticamente distancias de disparo, entrada, stop-loss y take-profit desde el spread actual y una estimación de ATR sensible a la volatilidad. Las órdenes de protección se simulan mediante el monitoreo de velas para que la estrategia pueda usarse con proveedores de datos que no admiten órdenes SL/TP adjuntas.

## Características destacadas

- **Modos de orden** – elegir entre Buy, Sell, Buy/Sell Stop, Buy/Sell Limit o Buy/Sell Stop-Limit. Las órdenes stop-límite se arman una vez que el precio alcanza la distancia de disparo y luego envían la entrada límite.
- **Distancias dinámicas** – los niveles pendientes, disparadores, stops y objetivos son proporcionales al mayor entre el spread actual o un spread sintético derivado del ATR (`ATR × AtrFactor`). Cuando el ATR no está listo, se usa una distancia base de tick configurable.
- **Adaptación a la volatilidad** – la longitud del ATR sigue el panel original (55) para que los offsets reaccionen a cambios de régimen sin ajuste extra.
- **Expiración de órdenes** – ventana de cancelación opcional con cumplimiento de tiempo mínimo de vida (predeterminado 11 minutos) mantiene las órdenes pendientes obsoletas fuera del libro.
- **Gestión de riesgo** – cada posición abierta se monitorea en cada vela cerrada; si el máximo/mínimo perfora el stop o objetivo calculado, la posición se cierra a mercado.
- **Consciencia de cotización** – la estrategia se suscribe al libro de órdenes para obtener los mejores precios de oferta/demanda para cálculos de offset más precisos, recurriendo a los cierres de velas cuando la profundidad no está disponible.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `Volume` | Tamaño de orden usado para todas las entradas. |
| `Mode` | Modo de entrada (mercado, stop, límite o stop-límite). |
| `Candle Type` | Agregación usada para ATR y verificaciones de ejecución basadas en velas. |
| `Base Ticks` | Distancia mínima de tick cuando los datos de ATR no están disponibles. |
| `Pending Multiplier` | Multiplicador aplicado a la distancia base de tick para offsets de órdenes pendientes. |
| `Trigger Multiplier` | Multiplicador adicional para distancias de disparo stop-límite. |
| `Stop Multiplier` | Multiplicador para distancia de stop-loss (establecer en 0 para deshabilitar). |
| `Take Multiplier` | Multiplicador para distancia de take-profit (establecer en 0 para deshabilitar). |
| `Use ATR` | Habilita el escalado basado en ATR de todas las distancias. |
| `ATR Factor` | Fracción del ATR tratada como spread sintético al escalar. |
| `Expiration` | Minutos hasta que se cancelan las órdenes pendientes (0 las mantiene GTC). |
| `Min Expiration` | Tiempo de vida mínimo pendiente en minutos, replicando la protección del panel. |

## Lógica de negociación

1. **Preparación de datos** – la estrategia se suscribe al tipo de vela configurado y mantiene un ATR de 55 períodos actualizado. Las instantáneas del libro de órdenes actualizan el último precio de oferta/demanda visto.
2. **Cálculo de distancias** – cada vela finalizada recalcula la distancia base de tick desde el ATR y el spread, luego deriva precios pendientes, de disparo, stop y take-profit según el modo seleccionado.
3. **Envío de órdenes** –
   - Los modos de mercado se ejecutan inmediatamente en la siguiente vela finalizada mientras la estrategia está plana.
   - Los modos stop y límite colocan la orden pendiente correspondiente y opcionalmente la cancelan después de la ventana de expiración.
   - Los modos stop-límite esperan hasta que el precio de disparo es impreso por el máximo/mínimo de la vela, luego envían la entrada límite.
4. **Supervisión de posición** – una vez que una posición está abierta, la estrategia comprueba las velas completadas en busca de violaciones de stop o objetivo y cierra la posición a mercado si se viola algún nivel.
5. **Restablecimiento de estado** – cuando la estrategia está plana y no hay ninguna orden activa, recalcula los niveles para que se pueda preparar una nueva operación en la siguiente vela.

El enfoque refleja el panel manual mientras permanece compatible con la API de alto nivel de StockSharp y el flujo de órdenes asíncrono.
