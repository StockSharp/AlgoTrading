# Estrategia Stopreversal Tm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Stopreversal Tm es una traducción directa del asesor experto original de MetaTrader 5 `Exp_Stopreversal_Tm.mq5`. La idea de trading sigue el indicador personalizado Stopreversal, que mantiene un trailing stop dinámico alrededor del precio y genera alertas de reversión cada vez que el precio cruza ese límite de seguimiento. La estrategia opera en un único instrumento y un único feed de velas y está diseñada para el trading de reversión de tendencia con un filtro de sesión definido por el usuario.

## Generación de señales
El indicador Stopreversal calcula un precio de referencia a partir del modo de precio aplicado seleccionado y luego ajusta un nivel de trailing stop por `Sensitivity` (el parámetro `nPips`). Cada vez que el nuevo precio aplicado sube por encima del trailing stop mientras la barra anterior estaba por debajo, se produce una señal alcista. A la inversa, aparece una señal bajista cuando el nuevo precio cae por debajo del trailing stop después de haber estado por encima. Cada señal alcista solicita simultáneamente el cierre de posiciones cortas existentes y la apertura de un nuevo largo, mientras que cada señal bajista cierra largos y abre cortos.

Para reproducir el comportamiento de la implementación original de MetaTrader, la estrategia puede retrasar la ejecución de señales en varias barras completadas (`Signal Bar Delay`). Esto replica la entrada `SignalBar` del asesor experto y previene el trading en la vela que aún se está formando.

## Filtro de sesión y manejo de posiciones
El asesor experto permitía el trading solo dentro de una ventana de tiempo especificada. La estrategia convertida mantiene la misma lógica: cuando el indicador `Use Time Filter` está habilitado, las órdenes se permiten solo dentro de la sesión configurada por `Start Hour/Minute` y `End Hour/Minute`. Si la hora actual sale de la ventana permitida, cualquier posición abierta se cierra inmediatamente. Las salidas impulsadas por señales permanecen activas incluso cuando la sesión está deshabilitada.

La estrategia trabaja en posiciones netas. Siempre se ejecuta una acción de cierre antes de una entrada opuesta, garantizando que la dirección cambia sin exposiciones superpuestas.

## Parámetros
- **Allow Buy Entries / Allow Sell Entries** – habilitar o deshabilitar la apertura de nuevas posiciones largas o cortas cuando se recibe la señal correspondiente.
- **Allow Long Exits / Allow Short Exits** – controlar si las señales opuestas pueden cerrar posiciones existentes.
- **Use Time Filter** – activa la ventana de sesión de trading.
- **Start Hour / Start Minute / End Hour / End Minute** – define el inicio inclusivo y el fin exclusivo de la ventana de trading. El filtro de tiempo soporta sesiones nocturnas donde la hora de fin es anterior a la hora de inicio.
- **Sensitivity (`nPips`)** – distancia relativa (expresada como multiplicador, p. ej., `0.004 = 0.4%`) usada para mover el trailing stop más cerca o más lejos del precio.
- **Signal Bar Delay (`SignalBar`)** – número de velas completadas a esperar antes de actuar sobre una señal. `0` ejecuta inmediatamente en la vela de cierre, `1` reproduce el comportamiento predeterminado de actuar en la barra anterior.
- **Candle Type** – marco temporal de la suscripción de velas usada para los cálculos del indicador.
- **Applied Price** – elección de la serie de precio (cierre, apertura, precio mediano, modos de seguimiento de tendencia, precio Demark, etc.) que alimenta el cálculo del trailing stop.

## Notas de implementación
- El indicador está implementado directamente dentro de la estrategia sin depender de búferes externos, asegurando que la lógica del trailing stop `nPips` coincida con el código MQL5 original.
- La gestión de sesión y la secuencia de señales siguen al experto original, incluida la prioridad de cerrar la exposición existente antes de abrir nuevas operaciones.
- La conversión se centra en la API de alto nivel de StockSharp: suscripciones de velas, cola de señales retrasadas y órdenes de mercado (`BuyMarket` / `SellMarket`). Las características de gestión monetaria vinculadas a las métricas de cuenta de MetaTrader se omitieron porque las estrategias de StockSharp ya operan con tamaños de posición explícitos.
