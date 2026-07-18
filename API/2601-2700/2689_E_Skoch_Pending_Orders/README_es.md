# Estrategia de Órdenes Pendientes E Skoch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

La **Estrategia de Órdenes Pendientes E Skoch** recrea el asesor experto original de MetaTrader que espera una nueva barra, analiza los dos máximos y mínimos más recientes tanto en el marco temporal de trading como en el diario, y coloca órdenes de ruptura pendientes. El objetivo es capturar momentum cuando el mercado rompe a través de la barra anterior después de un retroceso a corto plazo confirmado por la tendencia diaria.

La implementación de StockSharp mantiene las ideas originales pero usa características de la API de alto nivel como suscripciones a velas, órdenes de protección automáticas y parámetros de estrategia. La versión en C# se almacena en la carpeta `CS/` y aún no se proporciona un puerto en Python.

## Lógica de Trading

1. En cada vela terminada, la estrategia recupera los máximos y mínimos de las dos velas anteriores en el marco temporal de trabajo y las dos velas diarias anteriores.
2. Si el último máximo diario es menor que el de hace dos días **y** el máximo intradía anterior es menor que el anterior, la estrategia coloca un **buy stop** por encima del último máximo intradía más un búfer configurable.
3. Si el último mínimo diario es mayor que el de hace dos días **y** el mínimo intradía anterior es mayor que el anterior, la estrategia coloca un **sell stop** por debajo del último mínimo intradía menos un búfer configurable.
4. Cada orden pendiente establece niveles individuales de stop-loss y take-profit. Cuando se activa una entrada, la estrategia envía inmediatamente órdenes de stop y límite de protección para la posición abierta.
5. Cuando no hay posiciones ni órdenes activas, la estrategia registra el capital actual como línea de base. Si el capital de la cuenta crece en el porcentaje configurado relativo a esa línea de base, todas las posiciones se cierran y las órdenes de protección se cancelan.
6. El bloqueo opcional (`CheckExistingTrade`) evita nuevas entradas mientras cualquier posición esté abierta, imitando el parámetro de entrada original "CheckTrade".

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Marco temporal principal utilizado para señales. Predeterminado: velas de 1 hora. |
| `TakeProfitBuyPips` / `StopLossBuyPips` | Compensaciones de ganancia y pérdida del lado largo medidas en pips. |
| `TakeProfitSellPips` / `StopLossSellPips` | Compensaciones de ganancia y pérdida del lado corto medidas en pips. |
| `IndentHighPips` / `IndentLowPips` | Distancia en pips desde el último máximo o mínimo usada para colocar órdenes de stop. |
| `CheckExistingTrade` | Cuando es verdadero, se omiten nuevas órdenes mientras cualquier posición esté abierta. |
| `PercentEquity` | Porcentaje de ganancia en capital requerido para salir de todas las posiciones. |
| `Volume` | Tamaño de orden (predeterminado 0.01 lote para coincidir con el asesor experto original). |

## Gestión de Riesgos

- Las órdenes buy stop colocan un stop-loss por debajo del precio de entrada y un take-profit por encima.
- Las órdenes sell stop colocan un stop-loss por encima del precio de entrada y un take-profit por debajo.
- Las órdenes de protección se cancelan automáticamente cuando la posición se cierra o cuando se crea un nuevo conjunto de protección.
- La verificación de crecimiento del capital actúa como un "disyuntor" global para asegurar ganancias antes de que se reanude el trading.

## Notas

- La estrategia requiere tanto el marco temporal de trading como velas diarias, así que asegúrese de que los datos para ambas suscripciones estén disponibles en Designer o durante las pruebas retrospectivas.
- La conversión de pips ajusta automáticamente los símbolos que usan precios de pip fraccionarios (3 o 5 dígitos decimales) multiplicando el paso de precio por 10.
- La lógica asume una única posición agregada; la exposición simultánea larga y corta se evita intencionalmente cuando `CheckExistingTrade` está habilitado.
