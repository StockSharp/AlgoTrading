# Estrategia de Scalping de 5 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Conversión del expert advisor de MT4 **"5MIN SCALPING" (MQL ID 22828)** a la API de alto nivel de StockSharp. La estrategia busca configuraciones de rompimiento rápido en el marco temporal principal y las confirma con momentum de marco temporal superior y dirección MACD mensual antes de entrar al mercado.

- **Categoría:** Scalping de rompimiento / Momentum
- **Plataforma original:** MetaTrader 4
- **Requisitos de datos:** Feed de ticks o velas para los marcos temporales configurados (predeterminado 5 minutos, 30 minutos, 1 mes)

## Lógica de trading

1. **Filtro de tendencia.** Dos medias móviles lineales ponderadas (LWMA) con longitudes configurables (predeterminado 6 y 85) definen la tendencia prevaleciente. Los largos requieren que la LWMA rápida permanezca por encima de la LWMA lenta, los cortos requieren la relación opuesta.
2. **Filtro de estructura multi-barra.** El triplete LWMA interno (longitudes 8, 13, 21) se evalúa en las últimas 20 velas completadas. El algoritmo imita la función `scalper()` de la versión MQL:
   - Configuración alcista: cada barra dentro del bucle debe satisfacer `LWMA8 > LWMA13 > LWMA21`, el mínimo de la vela retrocede hacia la pila de medias móviles, y el cierre actual rompe por encima del máximo más alto de las 5 velas anteriores.
   - Configuración bajista: lógica espejo usando máximos penetrando la pila LWMA y el cierre actual rompiendo por debajo del mínimo más bajo de las 5 velas anteriores.
3. **Guardia de superposición de velas.** Una condición de superposición menor (`Low[2] < High[1]` para largos, `Low[1] < High[2]` para cortos) previene entradas en picos aislados.
4. **Confirmación de momentum.** Un indicador `Momentum` de marco temporal superior (predeterminado velas de 30 minutos, longitud 14) debe mostrar que al menos uno de los últimos tres valores se desvía de la línea base de 100 más que los umbrales configurados (0.3 por defecto).
5. **Alineación del MACD macro.** Un histograma mensual `MACD(12, 26, 9)` se calcula via `MovingAverageConvergenceDivergenceSignal`. Los trades largos requieren que la línea MACD esté por encima de la línea de señal, los trades cortos requieren lo opuesto.
6. **Agregación de posición.** Entrar en la dirección opuesta cierra la exposición existente primero e inmediatamente abre el nuevo trade con el volumen configurado.

## Gestión del riesgo

- **Objetivos estáticos.** Niveles opcionales de take-profit y stop-loss en pips (convertidos internamente usando el `PriceStep` del instrumento).
- **Módulo de break-even.** Cuando está habilitado, el stop se mueve a la entrada ± offset una vez que el precio viaja una cantidad configurable de pips.
- **Trailing stop.** Trailing stop opcional que sigue la posición a una distancia fija en pips una vez que el mercado avanza.
- **Salidas manuales.** Todas las salidas se manejan dentro de la estrategia sin colocar órdenes protectoras, lo que refleja el comportamiento original del EA.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `CandleType` | Marco temporal de 5 minutos | Marco temporal principal donde se detecta el rompimiento. |
| `MomentumCandleType` | Marco temporal de 30 minutos | Tipo de vela usado para el filtro de momentum de marco temporal superior. |
| `MacroMacdCandleType` | Marco temporal de 1 mes | Tipo de vela usado para la confirmación MACD a largo plazo. |
| `FastMaLength` | 6 | Longitud del filtro de tendencia LWMA rápido. |
| `SlowMaLength` | 85 | Longitud del filtro de tendencia LWMA lento. |
| `MomentumLength` | 14 | Lookback para el indicador de momentum. |
| `MomentumBuyThreshold` | 0.3 | Desviación mínima |Momentum-100| necesaria para confirmar trades largos. |
| `MomentumSellThreshold` | 0.3 | Desviación mínima |Momentum-100| necesaria para confirmar trades cortos. |
| `TakeProfitPips` | 50 | Distancia del take-profit expresada en pips. Establecer en 0 para deshabilitar. |
| `StopLossPips` | 20 | Distancia del stop-loss expresada en pips. Establecer en 0 para deshabilitar. |
| `TrailingStopPips` | 40 | Distancia del trailing stop en pips. Efectivo solo cuando `EnableTrailing` es verdadero. |
| `EnableTrailing` | true | Activa o desactiva la lógica del trailing stop. |
| `EnableBreakEven` | true | Habilita la gestión automática del break-even. |
| `BreakEvenTriggerPips` | 30 | Beneficio en pips necesario antes de que el stop se mueva al break-even. |
| `BreakEvenOffsetPips` | 30 | Buffer extra (en pips) añadido cuando el stop se desplaza al break-even. |
| `TradeVolume` | 1 | Volumen de orden usado para las entradas. |

## Uso

1. Agregar la estrategia al proyecto StockSharp y vincularla al instrumento deseado.
2. Asegurarse de que los datos históricos para todos los tipos de velas configurados estén disponibles antes de iniciar la estrategia.
3. Configurar el volumen, marcos temporales y umbrales según la volatilidad del instrumento negociado.
4. Iniciar la estrategia. Se suscribirá a todas las series de velas requeridas, dibujará indicadores en el gráfico (cuando el gráfico esté disponible) y gestionará entradas/salidas automáticamente.

## Notas y diferencias vs. el EA original

- Los módulos de trailing basados en dinero (`Take_Profit_In_Money`, `TRAIL_PROFIT_IN_MONEY2`) y el stop de capital de la versión MQL no están portados. El riesgo se maneja mediante distancias en pips.
- El escalado de lote estilo martingala (`Lots * MathPow(LotExponent, CountTrades())`) no está implementado. Ajustar `TradeVolume` manualmente si se necesita dimensionamiento de posición.
- Las alertas de email/notificación presentes en el código original se omiten. Usar la infraestructura de notificaciones de StockSharp si es necesario.
- La estrategia depende del `PriceStep` del instrumento para convertir distancias en pips. Validar que los metadatos del instrumento estén correctamente poblados en el entorno.
