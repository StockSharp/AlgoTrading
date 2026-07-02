# Estrategia de prueba de patrones de velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de prueba de patrones de velas** es una StockSharp conversión de alto nivel del MetaTrader 5 asesor experto original *CandlePatternsTest EA*. La estrategia escanea velas completadas en busca de una lista seleccionada de formaciones de velas japonesas clásicas y reacciona ingresando posiciones largas o cortas cuando aparecen estructuras alcistas o bajistas. La conversión se centra en la lógica de patrón discrecional del robot de origen y al mismo tiempo aprovecha StockSharp los controles de riesgo y la suscripción de datos API.

## Lógica de trading

1. **Suscripción de vela**: la estrategia se suscribe al tipo de vela configurado y espera las barras terminadas antes de ejecutar el reconocimiento de patrones.
2. **Filtro de cuerpo promedio**: un promedio móvil simple de cuerpos de velas actúa como normalización dinámica. Solo los patrones cuyas velas constituyentes superan este promedio se consideran válidos, lo que refleja la función `AvgBody` de la implementación MQL.
3. **Reconocimiento de patrones**: el detector comprueba:
   - Tres soldados blancos / Tres cuervos negros
   - Línea perforadora / Cubierta de nubes oscuras
   - Estrella Doji matutina / Estrella Doji vespertina
   - Envolvente alcista y bajista
   - Harami alcista y bajista
   - Líneas de reunión
4. **Gestión de entrada** – una vez que se confirma un patrón alcista, la estrategia abre una orden de compra en el mercado; Los patrones bajistas desencadenan una orden de venta en el mercado. Las señales opuestas invierten automáticamente la posición actual.
5. **Gestión de salida**: los niveles protectores de stop-loss y take-profit se derivan del cuerpo promedio de la vela y se rastrean en cada vela terminada. Si el precio toca cualquiera de los umbrales, la posición se cierra.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Tipo de datos de velas a las que suscribirse (predeterminado: período de 1 hora). |
| `AverageBodyPeriod` | Número de velas utilizadas para la longitud corporal promedio. Controla la normalización de patrones. |
| `EnableBullishPatterns` | Habilita o deshabilita las entradas largas. |
| `EnableBearishPatterns` | Habilita o deshabilita las entradas cortas. |
| `StopLossFactor` | Multiplicador aplicado al cuerpo promedio para la distancia de stop-loss. |
| `TakeProfitFactor` | Multiplicador aplicado al cuerpo promedio por distancia de obtención de beneficios. |

Todos los parámetros están expuestos a través de `StrategyParam<T>` para admitir la configuración de GUI y las ejecuciones del optimizador.

## Trazar

Cuando un área del gráfico está disponible, la estrategia se traza:

- Las velas suscritas
- El promedio móvil de precio de cierre utilizado para el contexto de tendencia
- Operaciones ejecutadas para verificación visual.

## Diferencias con el original EA

- Los filtros de noticias, las ventanas de tiempo, los conmutadores de cobertura y la gestión de la cuadrícula final presentes en el archivo MQ5 original se omiten intencionalmente para centrarse en el núcleo del patrón de velas.
- La gestión de riesgos se simplifica a un modelo simétrico de parada/objetivo derivado de la volatilidad de las velas.
- La versión StockSharp utiliza la gestión de posiciones del marco y los ayudantes `BuyMarket`/`SellMarket` en lugar de tickets de pedidos manuales.

## Notas de uso

- Establezca el parámetro `CandleType` para alinearlo con la sesión de mercado que desea analizar; marcos temporales más altos producen menos señales pero más fuertes.
- Ajuste `AverageBodyPeriod` para que el cuerpo promedio se acerque a la volatilidad reciente. Un valor menor reacciona más rápido pero puede aumentar el ruido.
- `StopLossFactor` y `TakeProfitFactor` se pueden optimizar para que coincidan con el perfil de riesgo del instrumento.

## Requisitos

- StockSharp entorno con fuente de datos de mercado capaz de generar el tipo de vela configurado.
- La estrategia espera series de velas secuenciales y que no se superpongan. Asegúrese de que el tablero seleccionado admita actualizaciones periódicas de la barra.
