# Estrategia del revendedor AK-47
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión del asesor experto MetaTrader 5 **"AK-47 Scalper EA" (compilación 44883)**. Recrea el comportamiento original dentro del marco estratégico de alto nivel StockSharp.

El algoritmo mantiene activa una única orden de *parada de venta* durante las horas de negociación permitidas. Una vez que se activa la orden, la estrategia adjunta inmediatamente órdenes protectoras de limitación de pérdidas y toma de ganancias. Tanto el precio de la orden pendiente como el stop de protección se ajustan dinámicamente a medida que se mueve el mercado.

## Lógica principal

1. Calcule el tamaño del pip a partir del tamaño del tick del instrumento (los símbolos de 5 dígitos usan pasos de 0,1 pip como en MetaTrader).
2. Determinar la ventana de negociación. Cuando el filtro de tiempo está habilitado, se permiten entradas solo entre las horas de inicio y finalización configuradas (incluido el inicio, excluyendo el final). Las sesiones nocturnas se apoyan en finalizar alrededor de la medianoche.
3. Asegúrese de que el diferencial actual en puntos no exceda el límite configurado antes de realizar nuevos pedidos.
4. Dimensione la posición:
   - Utilice el lote fijo (parámetro `Base Lot`) o
   - Convierta el `Risk Percent` configurado del valor de la cartera en lotes (imitando la fórmula MT5) y alinéelo con las restricciones de volumen de intercambio.
5. Coloque una orden de parada de venta `SL/2` pips por debajo de la oferta. La parada de protección está prevista `SL/2` pips por encima de la demanda y la toma de ganancias se sitúa `TP` pips por debajo de la entrada.
6. Mientras la orden está pendiente, la estrategia la vuelve a registrar continuamente para mantener la brecha de SL/2 pips con respecto a la oferta y actualiza los precios de protección planificados.
7. Después de la ejecución:
   - Registre una orden buy-stop-stop-loss y una orden buy-limit-take-profit utilizando los precios planificados.
   - En cada cierre de vela, la estrategia sigue el stop manteniéndolo exactamente `SL` pips por encima de la oferta actual (sin aflojarla nunca).
   - El precio de obtención de beneficios permanece fijo una vez establecido.
8. Si la posición es plana, todas las órdenes de protección se cancelan y puede comenzar un nuevo ciclo.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **Porcentaje de riesgo de uso** | Cambie entre lotes fijos y dimensionamiento basado en acciones. |
| **Porcentaje de riesgo** | Porcentaje aplicado al valor de la cartera al calcular el volumen comercial. |
| **Lote base** | Tamaño de lote fijo y paso de redondeo para el tamaño de posición. |
| **Detener pérdidas (pips)** | Distancia entre el precio de entrada y el stop de protección. La compensación de la orden pendiente utiliza la mitad de esta distancia. |
| **Obtener ganancias (pips)** | Distancia objetivo de beneficio. Establezca en cero para desactivar el objetivo. |
| **Difusión máxima (puntos)** | Spread máximo permitido (en MetaTrader puntos) para ingresar al mercado. |
| **Usar filtro de tiempo** | Habilite o deshabilite la restricción de la ventana de negociación. |
| **Hora de inicio/minuto** | Inicio de la ventana de negociación. |
| **Fin Hora/Minuto** | Fin de la ventana de negociación. |
| **Tipo de vela** | Suscripción de vela utilizada para actualizaciones de tiempos y precios. |

## Notas

- La estrategia utiliza sólo entradas cortas como el EA original.
- El seguimiento se realiza en la vela cercana para permanecer sincronizado con la API de alto nivel de StockSharp.
- Las órdenes de protección se reemplazan mediante llamadas `ReRegisterOrder`, por lo que el intercambio o el simulador deben admitir el reemplazo de órdenes.
- Los comentarios gráficos originales de MetaTrader no se reproducen porque las estrategias de StockSharp se basan en el registro en lugar de en los comentarios del terminal.
