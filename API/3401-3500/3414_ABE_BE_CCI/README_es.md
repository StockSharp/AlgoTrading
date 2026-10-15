# ABE BE CCI Estrategia envolvente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia StockSharp transfiere el asesor experto MetaTrader 5 **Expert_ABE_BE_CCI** (carpeta `MQL/306`). El EA original combina patrones de velas envolventes alcistas/bajistas con un módulo de confirmación del índice de canales de productos básicos (CCI) y gestión de dinero de lote fijo. La implementación de C# mantiene la misma lógica de decisión y al mismo tiempo aprovecha la suscripción de alto nivel y los enlaces de indicadores proporcionados por StockSharp.

El motor observa las velas completadas en el período de tiempo seleccionado, calcula un promedio móvil de cuerpos de velas, un promedio de precios de cierre y un CCI con período configurable. Los patrones envolventes alcistas o bajistas solo se aceptan cuando los cuerpos de las velas exceden el promedio reciente y el punto medio de la vela envuelta está en el lado correcto del promedio móvil, imitando las comprobaciones MQL `CCandlePattern`. Las operaciones largas requieren una envoltura alcista más CCI por debajo del umbral de sobreventa, mientras que las operaciones cortas requieren la condición de espejo con CCI por encima del umbral de sobrecompra. Las salidas de posición reflejan la lógica de "voto" EA: CCI cruces de ±ExitLevel neutralizan las posiciones abiertas independientemente de la dirección.

## Flujo de trabajo

1. Suscríbete al tipo de vela configurado y calcula:
   - Promedio del cuerpo de la vela en `BodyAveragePeriod` barras.
   - Media móvil de precios de cierre en la misma ventana.
   - Índice de canales de productos básicos con longitud `CciPeriod`.
2. Por cada vela terminada:
   - Verifique que la vela anterior forme una barra envuelta de colores opuestos.
   - Comprobar que el cuerpo envolvente sea mayor que el promedio del cuerpo rodante y cierre más allá de la apertura anterior, replicando los filtros MQL.
   - Confirme el contexto de la tendencia comparando el punto medio de la vela anterior con el promedio móvil del precio de cierre.
   - Confirme el impulso con CCI frente a `EntryOversoldLevel` o `EntryOverboughtLevel`.
3. Gestionar operaciones:
   - Si las condiciones alcistas se alinean y no hay ninguna posición larga activa, cierre los cortos y compre el volumen configurado.
   - Si las condiciones bajistas se alinean y no hay posiciones cortas activas, cierre posiciones largas y venda el volumen configurado.
   - Supervise CCI para detectar salidas: cualquier cruce por debajo de `+ExitLevel` o a través de `-ExitLevel` cierra posiciones largas, mientras que los cruces por encima de `-ExitLevel` o por debajo de `+ExitLevel` cierra posiciones cortas, coincidiendo con la lógica de "voto" de 40 puntos de EA.

## Parámetros predeterminados

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CciPeriod` | 49 | Longitud del indicador del índice del canal de productos básicos. |
| `BodyAveragePeriod` | 11 | Ventana para promediar el tamaño del cuerpo de la vela y el precio medio de cierre. |
| `EntryOversoldLevel` | -50 | CCI umbral que confirma configuraciones envolventes alcistas. |
| `EntryOverboughtLevel` | 50 | CCI umbral que confirma configuraciones envolventes bajistas. |
| `ExitLevel` | 80 | Nivel absoluto CCI que desencadena salidas de posición cuando se cruza. |
| `CandleType` | 1 hora | Plazo utilizado para la suscripción de velas. |

## Notas

- El manejo de volúmenes refleja las conversiones típicas de StockSharp: `Volume` define el tamaño base del pedido; Las posiciones opuestas se aplanan antes de invertir.
- Los componentes de seguimiento y administración de dinero (`TrailingNone`, `MoneyFixedLot`) del paquete MQL no se vuelven a crear; El tamaño del pedido de StockSharp ya cubre el comportamiento de lote fijo.
- Todos los comentarios dentro del código están en inglés, las tabulaciones se utilizan para la sangría y no se recuperan valores de indicadores a través de `GetValue`, siguiendo las pautas del repositorio.
