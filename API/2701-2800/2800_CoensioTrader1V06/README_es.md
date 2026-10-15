# Estrategia CoensioTrader1 V06
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
CoensioTrader1 V06 es una estrategia de seguimiento de tendencia con rompimiento, distribuida originalmente como un Expert Advisor de MetaTrader. La versión para StockSharp conserva la lógica de reconocimiento de patrones discrecional y elimina las funciones específicas del broker y de internet presentes en la implementación MQL. La estrategia opera sobre un único instrumento y marco temporal, utilizando Bollinger Bands y una media móvil exponencial doble (DEMA) para identificar movimientos de agotamiento seguidos de reanudación de tendencia.

El robot original permitía operar con hasta seis pares de divisas con conjuntos de parámetros individuales, admitía licencias basadas en DLL y reportaba resultados de optimización a un servidor remoto. Esos servicios auxiliares se omiten intencionalmente en esta versión. El foco está en el flujo central de entradas y salidas que reacciona a los rechazos de las Bollinger Bands confirmados por la estructura de swings y la pendiente del DEMA.

## Lógica de la estrategia
1. **Suscripción a datos** – la estrategia se suscribe al tipo de vela configurado (por defecto: 1 hora) y vincula Bollinger Bands junto con un DEMA.
2. **Rechazo de Bollinger Bands** – las señales se evalúan en la última vela completamente cerrada.
   - **Configuración Largo**
     - La vela abrió por debajo de la Bollinger Band inferior anterior y cerró de nuevo por encima de ella (ruptura fallida hacia abajo).
     - La vela creó un mínimo más alto comparado con la barra anterior, mientras que esa barra anterior hizo un mínimo más bajo comparado con su predecesora (estructura tipo doble suelo).
     - El DEMA sube estrictamente a lo largo de las últimas tres observaciones (valor actual > anterior > segundo anterior).
   - **Configuración Corto**
     - La vela abrió por encima de la Bollinger Band superior anterior y cerró de nuevo por debajo de ella (ruptura fallida hacia arriba).
     - La vela hizo un máximo más bajo comparado con la barra anterior, mientras que esa barra anterior hizo un máximo más alto comparado con su predecesora (estructura de doble techo).
     - El DEMA cae estrictamente a lo largo de las últimas tres observaciones.
3. **Ejecución de órdenes** – las órdenes de mercado se envían inmediatamente después de que la señal se confirma en una vela terminada. Se puede habilitar el aplanamiento opcional de posición en señales opuestas.
4. **Gestión de riesgos** – las distancias opcionales de stop-loss y take-profit se proporcionan a través de `StartProtection`. Ambas son compensaciones de precio absoluto; la funcionalidad de trailing stop del expert original no se reproduce.

## Parámetros
| Nombre | Descripción | Por defecto |
| ------ | ----------- | ----------- |
| `BollingerPeriod` | Período para el cálculo de las Bollinger Bands. | 30 |
| `BollingerDeviation` | Multiplicador de desviación estándar para las bandas. | 1.5 |
| `DemaPeriod` | Longitud de la media móvil exponencial doble utilizada para confirmación de tendencia. | 20 |
| `StopLossDistance` | Compensación absoluta de stop-loss pasada a `StartProtection`. Establecer en cero para deshabilitar. | `0 (absolute)` |
| `TakeProfitDistance` | Compensación absoluta de take-profit pasada a `StartProtection`. Establecer en cero para deshabilitar. | `0 (absolute)` |
| `CloseOnSignal` | Cerrar la posición actual antes de abrir una nueva en la dirección opuesta. | `false` |
| `CandleType` | Tipo de datos de vela o marco temporal. Por defecto es 1 hora. | `1h` |

## Notas de uso
- La versión de StockSharp opera únicamente en el `Strategy.Security` principal. Para imitar el comportamiento multi-símbolo del expert original, lance instancias de estrategia separadas con conjuntos de parámetros distintos.
- La lógica de dimensionamiento de lotes MQL (`RiskMax`, `LotSize`, `LotBalanceDivider`) no fue traducida. Configure `Volume` en la estrategia o mediante el gestor de riesgos según las reglas de su cartera.
- La activación basada en DLL, el registro remoto de optimización y las rutinas de dibujo UI presentes en los archivos MQL fueron eliminadas intencionalmente.
- Los valores de stop-loss y take-profit son distancias de precio absolutas. Adáptelos al tamaño de tick o valor de pip del instrumento al configurar la estrategia.
- El mecanismo de paso de trailing-stop original no está implementado. Si se requiere gestión de trailing, añada un módulo de riesgo dedicado sobre esta estrategia.
- Todos los comentarios del código y las lógicas se mantienen en inglés según lo solicitado; las traducciones del README se proporcionan por separado.

## Diferencias con la versión MQL
- **Gestión multi-símbolo**: reemplazada con un diseño de instrumento único para mayor claridad y facilidad de prueba.
- **Redes y licencias**: eliminadas; no se realizan solicitudes HTTP externas ni llamadas a DLL.
- **Dimensionamiento de órdenes**: simplificado para depender del manejo estándar de `Volume` de StockSharp.
- **Objetos visuales**: las anotaciones de gráfico de MetaTrader (flechas, etiquetas, temas de color) no se recrean. Use los helpers de gráficos de StockSharp si se requiere visualización.
- **Trailing stop**: no portado; solo se configuran las órdenes protectoras iniciales.

Esta documentación pretende ser exhaustiva para que la versión portada pueda ser revisada, probada y extendida sin necesidad de leer el código fuente MQL original.
