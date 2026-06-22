# Estrategia Omni Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Omni Tendencia es un port directo del experto MetaTrader "Exp_Omni_Trend". Combina una media móvil con un canal basado en ATR para detectar la tendencia dominante y cambiar entre exposición larga y corta. La versión StockSharp mantiene el comportamiento original, incluyendo el retraso entre la detección de señales y la ejecución de órdenes, así como la capacidad de deshabilitar tramos individuales de entrada o salida.

La estrategia se suscribe a la serie de velas configurada y alimenta cada barra finalizada a la lógica Omni Tendencia. La media móvil sirve como estimación de la tendencia central, mientras que los multiplicadores ATR construyen sobres de volatilidad. Los sobres se comportan como stops trailing: el precio que cierra más allá del límite del sobre anterior cambia la tendencia, genera una nueva señal de entrada en la nueva dirección, y cierra inmediatamente cualquier exposición contraria.

Si los umbrales opcionales de stop-loss y take-profit están habilitados, actúan del lado del broker en pasos de precio, complementando las salidas basadas en indicadores. El tamaño de posición se controla a través de la propiedad integrada `Volume` de la estrategia (por defecto `1`).

## Lógica de Trading

1. Calcular la media móvil elegida (`MaType`, `MaLength`, `AppliedPrice`) en el flujo de velas.
2. Calcular ATR (`AtrLength`) y derivar dos bandas adaptativas usando `VolatilityFactor` y `MoneyRisk`. La banda superior protege las posiciones cortas, la banda inferior protege las posiciones largas.
3. Cuando el precio supera la banda protectora de la barra anterior, la tendencia cambia:
   - Un breakout alcista (`HighPrice` por encima de la banda superior anterior) convierte la tendencia en "arriba", cierra cualquier posición corta si se permite, y abre una posición larga después de `SignalBar` velas completadas.
   - Un breakout bajista (`LowPrice` por debajo de la banda inferior anterior) convierte la tendencia en "abajo", cierra cualquier posición larga si se permite, y abre una posición corta después del retraso configurado.
4. Mientras la tendencia se mantiene alcista, la estrategia continúa solicitando salidas cortas; la regla simétrica se aplica para una tendencia bajista y salidas largas. Esto refleja el comportamiento del experto MetaTrader, donde la banda opuesta fuerza constantemente exposición plana contra la dirección dominante.
5. La gestión de riesgo opcional monitorea cada vela finalizada. Si la barra actual alcanza el precio de stop u objetivo (expresado en pasos de precio), la posición se cierra inmediatamente, reiniciando el precio de entrada almacenado.

Las señales se programan a través de una cola FIFO. Cuando `SignalBar` es cero, se ejecutan al cierre de la misma vela. De lo contrario, se activan en la apertura de la vela que completa el retraso, lo que replica el estilo de ejecución de "barra anterior" del experto fuente.

## Parámetros

| Nombre | Descripción | Por defecto |
|------|-------------|---------|
| `CandleType` | Tipo de vela (marco temporal) utilizado para los cálculos. | Marco temporal de 4 horas |
| `MaLength` | Período de la media móvil. | 13 |
| `MaType` | Método de media móvil: simple, exponencial, suavizada o ponderada linealmente. | Exponencial |
| `AppliedPrice` | Campo de precio pasado a la media móvil (cierre, apertura, alto, bajo, mediano, típico, ponderado). | Cierre |
| `AtrLength` | Período ATR utilizado por el canal de volatilidad. | 11 |
| `VolatilityFactor` | Multiplicador aplicado al ATR al construir el canal bruto. | 1.3 |
| `MoneyRisk` | Factor de desplazamiento que aleja el canal de la media móvil, idéntico a la entrada MQL. | 0.15 |
| `SignalBar` | Número de velas completadas a esperar antes de actuar sobre una señal. | 1 |
| `EnableBuyOpen` | Permitir abrir posiciones largas. | true |
| `EnableSellOpen` | Permitir abrir posiciones cortas. | true |
| `EnableBuyClose` | Permitir cerrar posiciones largas cuando se detecta una tendencia bajista. | true |
| `EnableSellClose` | Permitir cerrar posiciones cortas cuando se detecta una tendencia alcista. | true |
| `StopLossPoints` | Distancia de stop protector opcional en pasos de precio. Establecer en `0` para deshabilitar. | 1000 |
| `TakeProfitPoints` | Distancia del objetivo de beneficio opcional en pasos de precio. Establecer en `0` para deshabilitar. | 2000 |
| `Volume` | Propiedad de la estrategia que controla el tamaño de la operación. | 1 |

## Notas y Recomendaciones

- La implementación StockSharp alimenta los mismos valores de indicadores que el original y reproduce sus cambios de tendencia. Sin embargo, las ejecuciones precisas dependen de la fuente de datos y la latencia de ejecución.
- Establezca `SignalBar = 1` para imitar el valor predeterminado del asesor experto, donde las órdenes se ejecutan en la apertura de la siguiente vela después de que una señal esté disponible. Valores más grandes retrasan más la ejecución; establecer `0` ejecuta en el cierre actual.
- Los umbrales de stop-loss y take-profit se expresan en puntos (pasos de precio). Asegúrese de que el valor conectado exponga un `PriceStep` válido.
- El gráfico integrado dibuja la serie de velas, la media móvil seleccionada y las operaciones propias de la estrategia para validación visual rápida.
- Deshabilite tramos específicos de entrada o salida para restringir la estrategia a operación unilateral o para manejar salidas manualmente.
- La estrategia no crea órdenes pendientes; emite órdenes de mercado usando `BuyMarket` y `SellMarket` exactamente como el placement de órdenes directo del experto fuente.

## Archivos

- `CS/OmniTrendStrategy.cs` — Implementación en C# de la estrategia.
- `README.md`, `README_ru.md`, `README_zh.md` — Documentación en inglés, ruso y chino.

El soporte de Python se omite intencionalmente según lo solicitado.
