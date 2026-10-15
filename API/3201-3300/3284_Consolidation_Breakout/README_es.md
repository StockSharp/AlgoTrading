# Estrategia de ruptura de consolidación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el comportamiento central del asesor experto original **Consolidation Breakout** para MetaTrader. Busca consolidaciones estrechas confirmadas por filtros de momentum y MACD, y luego abre una posición en la dirección de la ruptura. El riesgo se gestiona mediante distancias fijas de take-profit y stop-loss medidas en pasos de precio (pips).

## Funcionamiento

1. El marco temporal principal se define mediante el parámetro `CandleType`. Todas las comprobaciones de tendencia y consolidación se evalúan en estas velas.
2. Dos medias móviles ponderadas lineales (LWMAs), calculadas sobre el precio típico, proporcionan el filtro direccional. Las configuraciones largas requieren que la LWMA rápida permanezca por encima de la LWMA lenta, mientras que las cortas necesitan la alineación opuesta.
3. Se detecta una consolidación cuando el mínimo de la vela de hace dos barras permanece por debajo del máximo de la vela anterior (caso largo), o cuando el mínimo anterior queda por debajo del máximo de hace dos barras (caso corto). Esto refleja la lógica de barras solapadas de la versión MQL.
4. El momentum debe confirmar el movimiento. El valor absoluto de momentum (relativo a cero) debe superar el umbral de compra o venta correspondiente. Esto aproxima el filtro de momentum del experto original alrededor del nivel 100.
5. Un MACD separado calculado en el marco temporal `MacdCandleType` debe coincidir con la dirección de la operación. La estrategia comprueba si la línea MACD lidera a la línea de señal tanto en el lado positivo como en el negativo del eje, reproduciendo la confirmación multitemporal del código fuente.
6. Cuando todos los filtros se alinean y la cuenta está plana o posicionada en la dirección opuesta, la estrategia envía una orden de mercado dimensionada por `TradeVolume`. Los niveles de protección se recalculan inmediatamente en pasos de precio para que los extremos intrabar puedan activar salidas.
7. Cada vela finalizada también supervisa posiciones activas. Si el rango de la vela toca el nivel de stop-loss o take-profit, la estrategia cierra la posición a mercado y reinicia los objetivos de protección.

## Indicadores

- Media móvil ponderada lineal (rápida y lenta, precio típico)
- Momentum
- MACD (con períodos 12/26/9 en un marco temporal superior)

## Parámetros

- `CandleType` - marco temporal principal usado para detectar rupturas.
- `MacdCandleType` - marco temporal usado para el filtro MACD de confirmación.
- `FastMaPeriod` - longitud de la LWMA rápida.
- `SlowMaPeriod` - longitud de la LWMA lenta.
- `MomentumLength` - período retrospectivo del filtro de momentum.
- `MomentumBuyThreshold` - momentum positivo mínimo requerido para operaciones largas.
- `MomentumSellThreshold` - momentum negativo mínimo requerido para operaciones cortas (expresado como valor absoluto).
- `StopLossPips` - distancia del stop de protección en pasos de precio.
- `TakeProfitPips` - distancia del objetivo de ganancia en pasos de precio.
- `TradeVolume` - volumen enviado con cada orden de mercado.

Los valores predeterminados reflejan el asesor experto publicado: períodos LWMA de 6 y 85, momentum length 14, umbrales de compra/venta de 0.3, stop-loss de 20 pips y take-profit de 50 pips. Ajuste las distancias basadas en pips al operar instrumentos con pasos de precio diferentes.

## Notas

- Los trailing stops, movimientos a break-even y módulos de gestión monetaria del script MQL se omiten intencionalmente para mantener la adaptación StockSharp centrada en la lógica principal de ruptura.
- Asegúrese siempre de que los marcos temporales seleccionados sean compatibles con su proveedor de datos. Si el marco temporal superior produce datos escasos, considere cambiar a un `MacdCandleType` menor para mantener el filtro MACD reactivo.
