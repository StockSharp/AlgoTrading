# Estrategia AMS ES RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
La estrategia AMS ES RSI replica el comportamiento del MetaTrader experto `Expert_AMS_ES_RSI` dentro de StockSharp. Combina formaciones de velas de estrella matutina y vespertina con un filtro de confirmación del índice de fuerza relativa (RSI). Las operaciones largas se abren cuando aparece una estrella de la mañana alcista, mientras que RSI indica condiciones de sobreventa. Se realizan operaciones cortas cuando se forma una estrella vespertina bajista junto con una sobrecompra RSI. Las posiciones se cierran cuando RSI vuelve a cruzar los niveles de umbral configurables.

## Supuestos del mercado
- Funciona en cualquier instrumento que produzca velas OHLC regulares. Los futuros sobre índices y divisas al contado fueron los objetivos originales del experto MQL.
- La estrategia espera una acción del precio fluida donde los patrones de velas japonesas sean significativos. Es posible que los gráficos de ticks extremadamente ruidosos no produzcan señales confiables.

## Lógica de entrada
1. Suscríbase al período de tiempo configurado (predeterminado: 1 hora) y espere tres velas completamente cerradas.
2. Calcule el tamaño corporal promedio en las últimas velas *BodyAveragePeriod* (predeterminado: 3).
3. Detecta una **estrella de la mañana** cuando:
   - La vela 3 es fuertemente bajista (`Open - Close` más grande que el tamaño promedio del cuerpo).
   - La vela 2 tiene un cuerpo real pequeño (menos de la mitad del promedio) y huecos por debajo de la vela 3.
   - La vela 1 cierra por encima del punto medio de la vela 3.
4. Detecte una **estrella vespertina** con condiciones bajistas simétricas.
5. Confirme las entradas largas cuando el valor actual de RSI esté por debajo de *LongEntryRsi* (predeterminado: 40). Confirme las entradas cortas cuando RSI esté por encima de *ShortEntryRsi* (predeterminado: 60).
6. Ejecutar órdenes de mercado utilizando la estrategia `Volume`.

## Salir de la lógica
- Cierre posiciones largas cuando RSI cruce hacia abajo a través de *UpperExitRsi* (predeterminado: 70) o *LowerExitRsi* (predeterminado: 30).
- Cierre posiciones cortas cuando RSI cruce hacia arriba por los mismos niveles.
- No se aplica ningún límite de pérdidas ni toma de ganancias estricto. La gestión de riesgos debe realizarse externamente o ajustando los umbrales.

## Parámetros
| Nombre | Descripción | Predeterminado | Rango |
| ---- | ----------- | ------- | ----- |
| `CandleType` | Tipo de datos que representa la serie de velas a suscribir. | plazo de 1 hora | Cualquier tipo de vela admitida |
| `RsiPeriod` | RSI longitud del cálculo. | 47 | Optimizable (10–70) |
| `BodyAveragePeriod` | Número de velas utilizadas para calcular el tamaño corporal promedio requerido para la validación del patrón. | 3 | Optimizable (2–6) |
| `LongEntryRsi` | Valor máximo RSI que permite entradas largas. | 40 | Optimizable (20–50) |
| `ShortEntryRsi` | Valor mínimo de RSI que permite entradas cortas. | 60 | Optimizable (50–80) |
| `LowerExitRsi` | Límite inferior que desencadena salidas cuando se cruza hacia arriba. | 30 | Optimizable (20–40) |
| `UpperExitRsi` | Límite superior que desencadena salidas cuando se cruza hacia abajo. | 70 | Optimizable (60–80) |

## Notas de implementación
- Utiliza la API de alto nivel de StockSharp con suscripciones automáticas de velas.
- Se basa únicamente en los valores de los indicadores proporcionados por `Bind`, evitando llamadas manuales a `GetValue` de acuerdo con las pautas del proyecto.
- Mantiene solo un historial mínimo en memoria (tres velas recientes) para la validación del patrón.
- La estrategia llama automáticamente a `StartProtection()` al iniciarse para habilitar mecanismos de seguridad integrados.

## Consejos de uso
1. Adjunte la estrategia a un par de instrumento/cartera y asegúrese de que la serie de velas esté disponible en su conector.
2. Ajuste los niveles de RSI según la volatilidad de los activos. Los umbrales más amplios reducen el número de operaciones pero aumentan la calidad de la confirmación.
3. Combínelo con módulos de dimensionamiento de posiciones externos (por ejemplo, volumen basado en riesgo) para emular el comportamiento del lote fijo del EA original.
4. Al realizar una prueba retrospectiva, asegúrese de que los datos de las velas contengan espacios para que los patrones de estrellas puedan identificarse correctamente.
