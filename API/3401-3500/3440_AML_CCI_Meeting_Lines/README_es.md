# Estrategia de líneas de encuentro AML CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce los MetaTrader 5 expertos "Expert_AML_CCI" dentro del marco de alto nivel StockSharp. El robot original
combina el patrón de velas japonesas "Meeting Lines" con un filtro de índice de canales de productos básicos (CCI) y utiliza el Asesor Experto
motor para ponderar los votos alcistas y bajistas. El puerto StockSharp mantiene la misma lógica de confirmación, traduce la vela
detección de patrones en aritmética pura de velas y expone todos los umbrales como parámetros fáciles de optimizar.

## como funciona
* **Fuente de datos**: la estrategia se suscribe a una serie de velas de marco temporal configurable (velas de 30 minutos de forma predeterminada) usando
`SubscribeCandles`. Cada vela terminada se envía junto con el valor CCI sincronizado a través del `Bind` de alto nivel.
pipeline, por lo que no se requiere gestión manual de indicadores.
* **Indicador central**: un único `CommodityChannelIndex` con período `CciPeriod` refleja el oscilador MetaTrader. Sus valores son
almacenado en caché internamente para comparar las dos lecturas completadas más recientes, replicando las llamadas `CCI(1)` y `CCI(2)` de MQL.
* **Lógica de vela**: los métodos auxiliares reconstruyen las comprobaciones de "Líneas de encuentro alcistas" y "Líneas de encuentro bajistas". ellos calculan
el promedio móvil de las longitudes del cuerpo sobre `AverageBodyPeriod` velas (predeterminado 3) y aplica el cuerpo largo y el cierre igual
requisitos del filtro original `CML_CCI`. Debido a que StockSharp entrega velas completas, el patrón se evalúa exactamente
cuando se cierra la segunda vela del patrón, el mismo momento en que el experto MQL emite su voto de 80 puntos.
* **Reglas de entrada** –
  * Las posiciones largas requieren una formación de líneas de reunión alcistas y el último valor CCI completado para permanecer por debajo o igual a
`LongEntryCciLevel` (-50 por defecto). Si hay un corto opuesto abierto, el tamaño de la orden incluye automáticamente el valor absoluto
de la posición actual para invertir la dirección, coincidiendo con el comportamiento EA.
  * Las posiciones cortas reflejan la lógica: un patrón bajista de Líneas de Encuentro más un valor de CCI superior o igual a `ShortEntryCciLevel`
(+50 por defecto).
* **Reglas de salida**: en lugar de las ponderaciones de votación del Asesor Experto, el puerto utiliza órdenes de aplanamiento explícitas. Posiciones cerradas
cuando el CCI cruza la banda extrema definida por `ExtremeCciLevel` (80 por defecto):
  * Los cortos salen cuando el CCI salta hacia arriba a través de −Extreme o vuelve a caer por debajo de +Extreme.
  * Los largos salen cuando el CCI cae por debajo de +Extreme o atraviesa −Extreme.
Estas reglas reflejan la rama de voto `40` dentro de `LongCondition` y `ShortCondition` en la clase de señal MQL.
* **Gestión de riesgos**: la estrategia deja paradas de protección a quien llama. Es compatible con StockSharp's `StartProtection`
ayuda si es necesario adjuntar externamente un stop-loss o take-profit.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Marco temporal de las velas fuente. | plazo de 30 minutos |
| `CciPeriod` | Longitud del índice del canal de productos básicos. | 18 |
| `AverageBodyPeriod` | Número de velas utilizadas para calcular el tamaño corporal promedio para la validación del patrón. | 3 |
| `LongEntryCciLevel` | Nivel de sobreventa que confirma Líneas de Encuentro alcistas. | −50 |
| `ShortEntryCciLevel` | Nivel de sobrecompra que confirma Líneas de Encuentro bajistas. | +50 |
| `ExtremeCciLevel` | Banda extrema absoluta para cruces de salida CCI. | 80 |

Todos los parámetros numéricos exponen rangos de optimizador idénticos a los valores predeterminados de EA para que la estrategia se pueda ajustar a través de StockSharp
herramientas de optimización.

## Notas de uso
1. Adjunte la estrategia a un valor y establezca el `Volume` deseado antes de comenzar.
2. Opcionalmente, modifique los umbrales para que coincidan con el perfil de administración de dinero original o para ajustar la sensibilidad.
3. La integración del gráfico dibuja velas, la curva CCI y ejecuta operaciones para una validación visual rápida de la detección de patrones.

Al centrarse en la misma combinación de vela+CCI, esta implementación StockSharp ofrece una versión fiel del Experto
Advisor mientras se mantiene dentro del estilo recomendado de alto nivel API.
