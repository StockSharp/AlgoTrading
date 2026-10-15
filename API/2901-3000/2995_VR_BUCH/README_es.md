# Estrategia de Medias Móviles VR BUCH
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Medias Móviles VR BUCH** es un port directo del asesor experto de MetaTrader *VR---BUCH*. Negocia reversiones de tendencia usando dos medias móviles configurables y un filtro de precio de vela. La versión de StockSharp mantiene el flujo de señal original: la estrategia cierra las posiciones abiertas cuando aparece una configuración opuesta y solo abre una nueva posición después de que la exposición anterior esté completamente cerrada.

La implementación se basa en las suscripciones de velas de alto nivel de StockSharp, indicadores de media móvil nativos y ayudantes de órdenes en tiempo real. Todos los valores de indicadores se procesan en velas terminadas y la estrategia evita los búferes históricos manuales excepto por un pequeño búfer circular que reproduce los parámetros de desplazamiento de MetaTrader.

## Lógica de Trading
1. **Cálculo de indicadores**
   - Una media móvil rápida y una lenta se calculan sobre el tipo de vela seleccionado.
   - Cada media móvil puede usar una fuente de precio y método de suavizado diferentes (simple, exponencial, suavizado, ponderado).
   - Los desplazamientos horizontales opcionales reproducen el parámetro `ma_shift` de MetaTrader referenciando valores de velas pasadas.
2. **Detección de señales**
   - Una configuración de *compra* ocurre cuando la MA rápida desplazada está por encima de la MA lenta desplazada **y** el precio de confirmación seleccionado está por encima de la MA rápida.
   - Una configuración de *venta* ocurre cuando la MA rápida desplazada está por debajo de la MA lenta desplazada **y** el precio de confirmación está por debajo de la MA rápida.
3. **Manejo de posición**
   - Si ya hay una posición abierta, una señal opuesta primero cierra el saldo plano. Las nuevas entradas se evalúan en señales posteriores solo cuando la posición neta vuelve a cero.
   - Cuando no hay posición, la estrategia envía una orden de mercado con el volumen configurado en la dirección de la señal activa.

No se incluyen niveles de stop-loss o take-profit por defecto. Los usuarios pueden combinar la estrategia con bloques de protección de StockSharp (`StartProtection`) o gestores de riesgo externos si es necesario.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| **Fast Period** | Longitud de la media móvil rápida. |
| **Fast Shift** | Número de velas usadas para desplazar el valor de la MA rápida hacia el pasado. |
| **Fast Price** | Componente de precio de la vela utilizado para la MA rápida (cierre, apertura, máximo, mínimo, mediano, típico, ponderado). |
| **Fast Method** | Método de suavizado para la MA rápida (simple, exponencial, suavizado, ponderado). |
| **Slow Period** | Longitud de la media móvil lenta. |
| **Slow Shift** | Número de velas usadas para desplazar el valor de la MA lenta. |
| **Slow Price** | Componente de precio de la vela para la MA lenta. |
| **Slow Method** | Método de suavizado para la MA lenta. |
| **Signal Price** | Precio de la vela usado para confirmar la entrada (por defecto el cierre). |
| **Candle Type** | Marco temporal o tipo de vela personalizado usado para los cálculos. |
| **Volume** | Volumen de la orden para nuevas operaciones. |

## Notas de Uso
- Las señales se evalúan solo en velas terminadas para evitar ruido intra-barra.
- La estrategia espera que el conector de trading proporcione suficientes datos históricos para calentar ambas medias móviles y sus búferes de desplazamiento.
- El precio ponderado usa la fórmula \((High + Low + 2 * Close) / 4\), coincidiendo con la opción `PRICE_WEIGHTED` de MetaTrader.
- El nombre de clase y el espacio de nombres siguen las convenciones del proyecto StockSharp, permitiendo una compilación perfecta dentro de la solución `AlgoTrading`.

## Cómo Ejecutar
1. Coloca la estrategia en un contenedor de estrategias StockSharp o ejecutor de muestra.
2. Configura el instrumento deseado, el marco temporal (`Candle Type`) y el volumen de orden.
3. Ajusta los ajustes de la media móvil para que coincidan con la plantilla original de MetaTrader si es necesario.
4. Inicia la estrategia. Se suscribirá a las velas, dibujará indicadores en los gráficos (si está disponible) y colocará órdenes de mercado según la lógica descrita.

Para el uso en cartera o con múltiples símbolos, duplica la instancia de la estrategia por instrumento y asigna instrumentos dedicados.
