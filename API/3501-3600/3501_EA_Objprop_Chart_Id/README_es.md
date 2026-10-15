# EA Estrategia de ID de gráfico OBJPROP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **EA estrategia de ID de gráfico OBJPROP** recrea el comportamiento centrado en el gráfico del ejemplo original MetaTrader 5 al mostrar Donchian envolventes de canal en tres períodos de tiempo sincronizados. El gráfico principal alberga el período de tiempo de negociación, mientras que dos paneles auxiliares visualizan el contexto H4 y diario. Esta configuración refleja el Asesor Experto original que apilaba múltiples gráficos e indicadores en un único espacio de trabajo para el análisis visual.

## Características clave

- **Visualización de múltiples períodos de tiempo**: se suscribe automáticamente a velas primarias, H4 y diarias para el valor seleccionado.
- **Longitud del canal Donchian unificada**: aplica el mismo período de canal a cada período de tiempo para mantener las envolventes comparables.
- **Integración de gráficos de alto nivel**: se basa en StockSharp áreas del gráfico para representar series de precios, Donchian canales y operaciones ejecutadas, reproduciendo el diseño MQL sin manipulación de objetos de bajo nivel.
- **Base extensible**: almacena los límites de canal más recientes para cada período de tiempo, lo que facilita extender la estrategia con lógica de ruptura o confirmación en el futuro.

## Parámetros

| Parámetro | Descripción | categoría | Predeterminado |
|-----------|-------------|----------|---------|
| `ChannelLength` | Longitud del canal Donchian utilizado en todos los períodos de tiempo suscritos. | Indicadores | 22 |
| `PrimaryCandleType` | Marco de tiempo principal utilizado para el comercio y como panel de gráfico superior. | generales | velas de 30 minutos |
| `H4CandleType` | Periodo de tiempo auxiliar H4 mostrado en un panel secundario. | generales | velas de 4 horas |
| `DailyCandleType` | Auxiliar Horario diario mostrado en un panel terciario. | generales | velas de 1 dia |

Todos los parámetros están disponibles a través de la interfaz de usuario de parámetros StockSharp, admiten optimización y se pueden ajustar sin cambiar el código.

## Lógica de la estrategia

1. Inicializa tres indicadores de canal Donchian con el mismo parámetro de longitud.
2. Se suscribe a las series de velas primarias, H4 y diarias seleccionadas para el valor actual.
3. Vincula cada suscripción a su indicador de canal respectivo utilizando el nivel alto API, asegurando que los valores del indicador se calculen de forma incremental.
4. Crea un área de gráfico principal y hasta dos áreas auxiliares donde se dibujan velas, canales y las operaciones de la estrategia.
5. Almacena los límites de canal superior e inferior más recientes para cada período de tiempo, lo que permite agregar reglas de decisión personalizadas más adelante.

La implementación actual es solo de visualización y no envía pedidos. Esto refleja el código original MetaTrader, que se centraba en componer un panel de gráficos sin lógica comercial automatizada.

## Notas de uso

- Asegúrese de que el valor seleccionado tenga datos históricos para cada período de tiempo utilizado por la estrategia para completar todas las áreas del gráfico.
- Puede cambiar cualquiera de los parámetros del período de tiempo a otros tipos de datos `TimeFrame` (por ejemplo, 15 minutos o velas semanales) si se requieren diferentes paneles de contexto.
- Se puede superponer una lógica comercial adicional en los métodos de procesamiento (`ProcessPrimary`, `ProcessH4`, `ProcessDaily`) reaccionando a los niveles de canal almacenados.

## Notas de conversión

- El ejemplo de MetaTrader creó gráficos secundarios a través de objetos `OBJ_CHART`; la versión StockSharp la reemplaza con áreas de gráficos creadas por el nivel alto API, que está mejor integrado con la plataforma.
- La gestión de indicadores se realiza mediante llamadas `BindEx` en lugar de la creación manual de identificadores, lo que garantiza que los valores estén sincronizados con las velas entrantes.
- Las rutinas de eliminación de objetos no son necesarias porque StockSharp elimina automáticamente las suscripciones y los enlaces de gráficos cuando se detiene la estrategia.
