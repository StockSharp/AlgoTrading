# Estrategia Improve MA & RSI con Cobertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el experto original de MetaTrader "Improve" a StockSharp usando la API de alto nivel. Opera simultáneamente dos instrumentos: el símbolo principal seleccionado para la estrategia y un símbolo de cobertura. La dirección de la operación se define por la relación entre dos medias móviles suavizadas en el instrumento principal y el índice de fuerza relativa (RSI). La pata de cobertura refleja la dirección de la pata principal, creando una exposición pareada que busca beneficiarse de movimientos de momentum sincronizados mientras limita el riesgo de un solo instrumento.

## Lógica de la estrategia

- Calcular dos Medias Móviles Suavizadas (SMMA) en el símbolo primario con períodos rápido y lento configurables.
- Calcular RSI en las mismas velas y monitorear los umbrales de sobreventa/sobrecompra.
- Entrar **largo** en ambos instrumentos cuando la SMMA lenta está por encima de la SMMA rápida y el RSI está en o por debajo del umbral de sobreventa.
- Entrar **corto** en ambos instrumentos cuando la SMMA lenta está por debajo de la SMMA rápida y el RSI está en o por encima del umbral de sobrecompra.
- Las posiciones permanecen abiertas hasta que el beneficio abierto combinado de ambas patas supera el objetivo monetario configurado, en cuyo punto la estrategia liquida ambos lados.

El algoritmo lleva un registro de los precios de cierre más recientes de cada instrumento. El beneficio combinado se estima a partir de la diferencia entre el cierre actual y el precio de entrada almacenado de cada pata. Dado que no se aplica stop-loss, las posiciones pueden permanecer abiertas por períodos prolongados cuando el precio no alcanza el objetivo de beneficio.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| **Volume** | Cantidad de orden para ambos instrumentos, el primario y el de cobertura. |
| **Profit Target** | Objetivo monetario compartido por ambas patas; cuando se alcanza, la estrategia cierra cada posición abierta. |
| **Hedge Security** | Instrumento secundario que se opera junto con el instrumento principal. |
| **Fast MA** | Período de la Media Móvil Suavizada rápida (predeterminado 8). |
| **Slow MA** | Período de la Media Móvil Suavizada lenta (predeterminado 21). Debe ser mayor que el período de la MA rápida. |
| **RSI Period** | Longitud usada para calcular el RSI (predeterminado 21). |
| **Oversold** | Nivel RSI que activa entradas largas junto con la condición de MA (predeterminado 30). |
| **Overbought** | Nivel RSI que activa entradas cortas junto con la condición de MA (predeterminado 70). |
| **Candle Type** | Marco temporal para cálculos; por defecto velas de 1 hora pero puede ajustarse. |

## Indicadores

- **Media Móvil Suavizada (SMMA)** – usada dos veces para definir los componentes de tendencia rápida y lenta.
- **Índice de Fuerza Relativa (RSI)** – determina condiciones de sobreventa/sobrecompra para confirmación.

## Reglas de entrada y salida

1. **Entrada larga**
   - SMMA lenta &gt; SMMA rápida en el símbolo primario.
   - RSI ≤ Sobreventa.
   - Ambas patas se abren con órdenes a mercado en la misma dirección (compra/compra).
2. **Entrada corta**
   - SMMA lenta &lt; SMMA rápida en el símbolo primario.
   - RSI ≥ Sobrecompra.
   - Ambas patas se abren con órdenes a mercado en la misma dirección (venta/venta).
3. **Salida**
   - Cuando `(beneficio primario + beneficio de cobertura) ≥ Profit Target`, la estrategia cierra ambas posiciones usando órdenes a mercado.
   - No se aplica lógica adicional de stop-loss o trailing; la gestión de riesgos debe agregarse externamente si se requiere.

## Notas de uso

- Asegurarse de que tanto el instrumento principal como el de cobertura estén asignados antes de iniciar la estrategia; de lo contrario lanzará una excepción.
- La estimación de beneficio combinado depende de los precios de cierre de velas. El deslizamiento y las diferencias de ejecución entre las dos patas pueden afectar el beneficio real realizado.
- Dado que la estrategia abre ambas patas simultáneamente, es adecuada para instrumentos correlacionados (por ejemplo, pares de divisas o futuros relacionados) donde se espera que se muevan en tándem.
- Considerar añadir controles de riesgo a nivel de portafolio cuando se opere en vivo, ya que el algoritmo original usa solo el objetivo de beneficio virtual para las salidas.
