# Estrategia de filtro iCHO Trend CCIDualOnMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un puerto StockSharp de alto nivel del asesor experto MetaTrader **"iCHO Trend CCIDualOnMA Filter"**. Combina un filtro de régimen de línea cero del oscilador Chaikin con una confirmación dual del índice de canal de productos básicos (CCI) que se calcula sobre una serie de precios suavizada. El resultado es un enfoque de seguimiento de tendencias que reacciona a los cambios de impulso pero aún requiere una confirmación de impulso por parte del par CCI antes de iniciar una operación.

## Lógica comercial

1. **Núcleo del oscilador Chaikin**: la línea de acumulación/distribución se suaviza mediante dos medias móviles configurables. Su diferencia replica el oscilador Chaikin. Los cruces por encima/por debajo de cero indican un cambio en el flujo de capital dominante.
2. **Filtro dual CCI**: ambas instancias CCI utilizan la misma entrada de precio suavizada de promedio móvil pero diferentes períodos retrospectivos. Una configuración larga requiere que el CCI rápido se recupere del territorio negativo y cruce por encima del CCI lento mientras el oscilador Chaikin se mantiene por encima de cero. Una configuración breve refleja estas condiciones.
3. **Inversión opcional**: el EA original proporciona un indicador "inverso" que intercambia señales largas y cortas. El puerto mantiene este comportamiento para que se puedan utilizar las mismas reglas para las pruebas de contratendencia.
4. **Gestión de posiciones**: las banderas opcionales cierran la exposición opuesta antes de abrir una nueva posición y limitan la estrategia a una única posición abierta. Se aplica una regla de una operación por barra para imitar la implementación de MetaTrader.
5. **Filtro de sesión**: las operaciones se pueden restringir a una ventana intradiaria definida por el usuario, incluidas las sesiones envolventes que pasan de la medianoche.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `FastChaikinLength` | Período de media móvil rápida utilizado dentro del oscilador Chaikin. |
| `SlowChaikinLength` | Período de media móvil lenta utilizado dentro del oscilador Chaikin. |
| `ChaikinMethod` | Método de media móvil (simple, exponencial, suavizado, ponderado lineal) aplicado a la línea de acumulación/distribución. |
| `FastCciLength` | Vista retrospectiva del rápido índice del canal de productos básicos. |
| `SlowCciLength` | Vista retrospectiva del lento índice del canal de materias primas. |
| `MaLength` | Longitud de la media móvil de preprocesamiento que alimenta los CCI. |
| `MaMethod` | Método de media móvil utilizado para preprocesar el precio antes de que llegue a los CCI. |
| `MaPrice` | Tipo de precio (cierre, apertura, máximo, mínimo, mediana, típico, ponderado) que se suaviza antes de los CCI. |
| `UseClosedBar` | Procese solo velas completamente terminadas (el valor predeterminado es verdadero, idéntico a `SignalsBarCurrent=bar_1` en EA). |
| `ReverseSignals` | Intercambia lógica larga y corta. |
| `CloseOpposite` | Cierre una posición abierta en la dirección opuesta antes de ingresar a una nueva. |
| `OnlyOnePosition` | Permita solo una posición abierta en cualquier momento. |
| `TradeMode` | Restrinja la ejecución a posiciones largas, cortas o ambas (BuyOnly, SellOnly, BuyAndSell). |
| `UseTimeFilter` | Habilite el filtro de sesión de negociación. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Límites de la sesión (incluido el inicio, excluyendo el final) expresados en tiempo de intercambio. Se admiten sesiones integrales. |
| `CandleType` | Plazo de suscripción de velas que alimenta los indicadores. |

## Notas

- La estrategia utiliza sólo enlaces `SubscribeCandles` de alto nivel e indicadores integrados; no se requieren buffers personalizados ni solicitudes históricas.
- Todos los cálculos basados en precios adoptan el mismo preprocesamiento de promedio móvil que el indicador MetaTrader `CCIDualOnMA` alimentando el CCI con una serie de precios suavizada.
- Los parámetros predeterminados reproducen los valores predeterminados originales de EA: Chaikin 3/10 EMA, CCI períodos 14 y 50, preprocesamiento de SMA de 12 períodos y una ventana de negociación de 10:01 a 15:02.
