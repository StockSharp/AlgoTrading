# Estrategia de cobertura MO Bidir
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia MO Bidir Hedge** es una StockSharp versión del MetaTrader 4 asesores expertos `mo_bidir_v0_1`. El robot original fue diseñado para el gráfico de cinco minutos y siempre mantuvo una exposición al mercado cubierta: cada nueva barra abría una posición larga y corta con distancias predefinidas de stop-loss y take-profit. La versión StockSharp reproduce este comportamiento utilizando velas terminadas, asistentes de órdenes de alto nivel y parámetros de riesgo explícitos medidos en puntos de instrumentos.

## Lógica de trading
1. Suscríbase al tipo de vela configurado (período de tiempo de cinco minutos de forma predeterminada) y procese solo velas terminadas.
2. Tan pronto como se cierre una vela, inspeccione las patas internas del seto. Si algún tramo permanece abierto, la estrategia espera a que se activen las órdenes de protección y no abre posiciones adicionales.
3. Cuando no haya tramos activos, envíe una orden de **compra de mercado** y una orden de **venta de mercado** del mismo tamaño. Cada orden ejecutada se convierte en un tramo de cobertura independiente rastreado por la estrategia.
4. Después de completar cada entrada, los umbrales de stop-loss y take-profit se calculan multiplicando las distancias de puntos configuradas por el paso de precio del instrumento (o el incremento mínimo de precio cuando el paso no está disponible).
5. En cada vela finalizada posterior, la estrategia verifica los máximos y mínimos de las velas:
   - Los tramos largos se cierran mediante una venta de mercado cuando el mínimo supera el nivel de parada; si no se detiene, un máximo que alcanza el objetivo cierra el tramo para obtener ganancias.
   - Los tramos cortos se cierran mediante una compra de mercado cuando el máximo toca el stop; de lo contrario, un mínimo que alcance el objetivo genera ganancias.
   - Cuando ambos umbrales caen dentro de la misma vela, se prioriza el stop-loss porque su toque habría cerrado la posición primero en la implementación de MetaTrader.
6. Una vez que todos los tramos están cerrados por sus niveles de protección, la estrategia prepara inmediatamente el siguiente par cubierto en el siguiente cierre de vela.

Este flujo de trabajo mantiene la paridad con la lógica MT4 y se basa exclusivamente en las API StockSharp de alto nivel (`BuyMarket`/`SellMarket`) y el procesamiento basado en velas exigido por las pautas de conversión.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Tamaño del pedido aplicado a ambos lados de la cobertura. Debe ser positivo. |
| `StopLossPoints` | Distancia desde el precio de entrada hasta el stop de protección medida en puntos del instrumento. Utilice `0` para desactivar la parada. |
| `TakeProfitPoints` | Distancia objetivo desde el precio de entrada en puntos del instrumento. Utilice `0` para desactivar el objetivo de ganancias. |
| `CandleType` | Plazo utilizado para detectar nuevas barras. El valor predeterminado es un período de tiempo de cinco minutos. |

Todas las distancias basadas en puntos se convierten a precios absolutos multiplicando el valor configurado por el instrumento `PriceStep`. Si el paso no está definido, se utiliza el incremento de precio mínimo; cuando ninguno de los valores está disponible, los niveles de protección permanecen inactivos.

## Gestión del riesgo
- Ambos lados de la cobertura utilizan el mismo volumen fijo y dependen de órdenes de protección simétricas.
- Las distancias de stop-loss y take-profit reflejan los valores predeterminados de MetaTrader (80 y 750 puntos respectivamente), preservando la relación "8 pips frente a 75 pips" en un símbolo de divisas de 5 dígitos.
- Cada tramo se cierra con una orden de mercado, liberando instantáneamente el margen y permitiendo que el tramo restante continúe sin gestión hasta que se alcance su propio nivel de protección.

## Notas de implementación
- La estrategia procesa estrictamente **velas terminadas** para cumplir con las reglas de conversión de todo el proyecto. Los toques de parada o objetivo dentro de la barra se infieren a partir de los extremos de las velas, por lo que las pruebas retrospectivas sin datos de ticks asumirán que la parada se activó antes del objetivo cuando ambos precios aparecieron dentro de la misma barra.
- El libro de cobertura interno realiza un seguimiento de las coberturas independientemente de la posición neta de la cartera. Esto refleja el comportamiento de MetaTrader donde las posiciones largas y cortas coexisten simultáneamente.
- No se introduce ninguna lógica de seguimiento automatizada, filtros de sesión ni indicadores adicionales: la versión StockSharp sigue siendo intencionalmente tan minimalista como el asesor experto original.

## Consejos de uso
- Ajuste `TradeVolume` para que coincida con los tamaños de los contratos de los corredores y asegúrese de que el instrumento admita cobertura de compra/venta simultánea si el entorno lo requiere.
- Si necesita valores basados en pips (por ejemplo, 8 pips), multiplique por la cantidad de puntos que representan un pip para el símbolo actual antes de asignar el parámetro.
- Combine la estrategia con StockSharp módulos de riesgo o `StartProtection` si se requieren salvaguardias adicionales a nivel de cartera.
