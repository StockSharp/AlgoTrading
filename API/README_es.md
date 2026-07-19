# Catálogo de estrategias de la API de StockSharp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este directorio contiene ejemplos de estrategias para la API de StockSharp implementados en C# y Python. Las carpetas se dividen en rangos numéricos (`0001-0100`, `0101-0200`, etc.) y las páginas siguientes agrupan todas las estrategias por su idea principal de trading.

Cada entrada enlaza directamente con ambas carpetas de implementación y utiliza un logotipo SVG transparente compatible con temas claros y oscuros.

**Estrategias:** 3811

**Implementaciones:** C# y Python

## Tipos de estrategia

- [Arbitraje, pares y valor relativo (25)](StrategyTypes/arbitrage-pairs-relative-value_es.md) — Estrategias que operan relaciones de precios entre instrumentos, diferenciales o activos vinculados, en lugar de depender de una única previsión direccional.
- [Reversión a la media y giros (299)](StrategyTypes/mean-reversion-reversals_es.md) — Sistemas contra tendencia que buscan precios extendidos, movimientos agotados o tendencias fallidas para operar un retorno al equilibrio o a un punto de giro.
- [Rupturas y canales (319)](StrategyTypes/breakouts-channels_es.md) — Estrategias basadas en la salida del precio de un rango, el cruce de soporte o resistencia o el paso por el límite calculado de un canal.
- [Volumen, VWAP y flujo de órdenes (63)](StrategyTypes/volume-vwap-order-flow_es.md) — Sistemas que usan volumen negociado, VWAP, liquidez, profundidad de mercado o flujo de órdenes para identificar entradas y salidas.
- [Patrones de velas y precios (191)](StrategyTypes/candlestick-price-patterns_es.md) — Estrategias que reconocen formaciones de velas, estructuras gráficas, huecos, pivotes y otros patrones recurrentes directamente en la acción del precio.
- [Estacionalidad, sesiones y eventos (92)](StrategyTypes/seasonal-session-event_es.md) — Sistemas sensibles al tiempo impulsados por sesiones, calendarios, eventos programados, rangos de apertura o comportamientos estacionales.
- [Estadística, modelos adaptativos e IA (77)](StrategyTypes/statistical-adaptive-ai_es.md) — Estrategias cuantitativas que usan estimación estadística, modelos adaptativos, aprendizaje automático, redes neuronales o clasificación de señales.
- [Factores, cartera y rotación (24)](StrategyTypes/factor-portfolio-rotation_es.md) — Enfoques multiactivo que clasifican instrumentos, asignan capital por factores, reequilibran carteras o rotan entre mercados.
- [Grid, DCA y gestión de posiciones (143)](StrategyTypes/grid-dca-position-management_es.md) — Estrategias centradas en escaleras de órdenes, promediado, entradas escalonadas, tamaño de posición, salidas y gestión continua de operaciones.
- [Scalping y ejecución (133)](StrategyTypes/scalping-execution_es.md) — Sistemas de corto plazo donde el momento de entrada, el spread, la colocación de órdenes y la ejecución son fundamentales para la ventaja operativa.
- [Volatilidad y opciones (78)](StrategyTypes/volatility-options_es.md) — Estrategias basadas en regímenes de volatilidad, expansión o contracción del rango, derivados, valoración de opciones y riesgo de volatilidad.
- [Medias móviles y cruces (191)](StrategyTypes/moving-averages-crossovers_es.md) — Sistemas de tendencia centrados en dirección, alineación, desplazamiento y cintas de medias móviles, además de cruces rápidos y lentos.
- [Indicadores direccionales de tendencia (264)](StrategyTypes/directional-trend-indicators_es.md) — Estrategias dirigidas por herramientas de tendencia y fuerza direccional como ADX/DMI, SuperTrend, Parabolic SAR, Ichimoku y Alligator.
- [Tendencia por momentum y osciladores (206)](StrategyTypes/momentum-oscillator-trend_es.md) — Estrategias direccionales confirmadas por momentum, MACD, RSI, CCI, estocástico, ROC, divergencias y osciladores relacionados.
- [Rupturas, retrocesos y acción del precio (95)](StrategyTypes/breakouts-pullbacks-price-action_es.md) — Entradas de continuación de tendencia expresadas mediante rupturas, retrocesos, canales, swings, velas, correcciones y estructura de mercado.
- [Tendencia adaptativa, multitemporal y especializada (277)](StrategyTypes/adaptive-multitimeframe-specialized-trend_es.md) — Sistemas de tendencia adaptativos, multitemporales, guiados por modelos, híbridos y especializados que no dependen de una sola familia de indicadores.
- [Osciladores y señales de indicadores (203)](StrategyTypes/oscillators-indicator-signals_es.md) — Estrategias cuyo disparador principal procede de osciladores, umbrales, cruces o divergencias de indicadores.
- [Órdenes, riesgo y gestión de posiciones (194)](StrategyTypes/order-risk-position-management_es.md) — Sistemas centrados en gestión de órdenes, tamaño, protección, grids, recuperación, trailing y control de posiciones existentes.
- [Combinaciones de indicadores y lógica de señales (319)](StrategyTypes/indicator-combinations-signal-logic_es.md) — Reglas de entrada compuestas basadas en acuerdo de indicadores, umbrales, cruces, divergencias y selección de señales.
- [Niveles de precio, patrones y estructura de mercado (263)](StrategyTypes/price-levels-patterns-market-structure_es.md) — Sistemas especializados basados en niveles, rangos, pivotes, geometría de Fibonacci, ondas, velas y estructura de mercado.
- [Estrategias cuantitativas, adaptativas y experimentales (25)](StrategyTypes/quantitative-adaptive-experimental_es.md) — Diseños matemáticos, estadísticos, de aprendizaje automático, adaptativos, aleatorios y deliberadamente experimentales.
- [Herramientas, paneles, alertas y plantillas (74)](StrategyTypes/tools-panels-alerts-templates_es.md) — Utilidades de trading, paneles de interfaz, alertas, plantillas, bancos de pruebas, ayudas gráficas, bibliotecas y ejemplos de integración.
- [Fundamental, macro y específico por activo (22)](StrategyTypes/fundamental-macro-asset-specific_es.md) — Lógica especializada ligada a fundamentales, datos macro, informes, clases de activos o instrumentos y mercados concretos.
- [Reglas de tiempo, sesión y eventos (13)](StrategyTypes/time-session-event-rules_es.md) — Estrategias compuestas cuya restricción distintiva es una sesión, ventana horaria, evento de calendario o programación recurrente.
- [Trading direccional y basado en reglas (111)](StrategyTypes/directional-rule-based-trading_es.md) — Sistemas direccionales especializados expresados mediante reglas explícitas de largo/corto, compra/venta, tendencia, giro, entrada o salida.
- [Sistemas expertos compuestos (110)](StrategyTypes/composite-expert-systems_es.md) — Sistemas multicomponente, híbridos, de conjunto, robots, traders y asesores expertos que combinan varios mecanismos.

## Estructura del repositorio

Cada directorio numerado contiene una descripción de la estrategia y las carpetas de implementación `CS` y `PY`. Las páginas de tipos ofrecen tablas con descripciones breves y enlaces directos mediante los logotipos.

## Compatibilidad

Los ejemplos están diseñados para la [API de StockSharp](https://github.com/StockSharp/StockSharp) y pueden adaptarse a los flujos de trabajo de StockSharp Designer, Shell y Runner.
