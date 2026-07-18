# Estrategia ScalpWiz 9001
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
ScalpWiz 9001 es un sistema de escalado de ruptura por capas que replica el comportamiento del experto MetaTrader del mismo nombre. La estrategia mide cuánto cierra la última vela más allá del envolvente de las Bandas de Bollinger y, cuando la volatilidad se expande bruscamente, despliega una cuadrícula de órdenes stop pendientes por encima o por debajo del mercado. El módulo de gestión de dinero original se preserva: cada orden pendiente puede usar un lote fijo o arriesgar un porcentaje configurable del capital de la cuenta.

Una vez que una de las órdenes stop se ejecuta, las órdenes restantes se cancelan, mientras que la posición activa está protegida con un stop loss tradicional, take-profit y un componente de trailing que solo comienza a seguir después de que se logra un buffer adicional. La estrategia está pensada para escalado de alta frecuencia en marcos temporales más bajos, pero puede ejecutarse en cualquier instrumento compatible con StockSharp.

## Lógica de señal
1. Suscribirse al marco temporal configurado y calcular las Bandas de Bollinger de 20 períodos con factor de desviación `BandsDeviation` (predeterminado 2).
2. Comprobar cuánto se aleja el precio de cierre de la banda superior o inferior. Cuando el cierre supera la banda al menos la distancia del cuarto nivel (`Level3Pips` convertido a precio), la estrategia se prepara para desvanecerse:
   - Cierre por encima de la banda superior → colocar órdenes sell-stop por debajo del mercado.
   - Cierre por debajo de la banda inferior → colocar órdenes buy-stop por encima del mercado.
3. Se colocan cuatro órdenes pendientes a distancias crecientes (`Level0Pips` … `Level3Pips`). Cada orden usa el volumen fijo o el porcentaje de riesgo asignado a ese nivel. Las órdenes expiran después de `ExpirationMinutes` si no se tocan.
4. Cuando una orden de entrada se ejecuta, todas las órdenes pendientes se cancelan. La posición ejecutada se gestiona con el stop loss (`StopLossPips`), take profit (`TakeProfitPips`) y parámetros de trailing (`TrailingStopPips`, `TrailingStepPips`). El trailing solo mueve el stop de protección cuando el precio viaja al menos `TrailingStopPips + TrailingStepPips` desde la entrada.
5. Las salidas se ejecutan con órdenes a mercado una vez que el stop de trailing o el objetivo de beneficio se toca en una vela completada.

## Parámetros
- **Candle Type** – marco temporal para los cálculos de Bollinger.
- **Bands Period / Bands Deviation** – configuración de Bollinger.
- **Stop Loss (pips)** – distancia del stop de protección en pips.
- **Take Profit (pips)** – distancia del objetivo de beneficio en pips.
- **Trailing Stop (pips)** – distancia del trailing stop que sigue al movimiento tras el buffer extra.
- **Trailing Step (pips)** – distancia adicional requerida antes de que se active el trailing.
- **Expiration (minutes)** – vida útil de las órdenes stop pendientes. Establecer en 0 para mantener las órdenes indefinidamente.
- **Management Mode** – elegir entre `FixedVolume` y `RiskPercent`.
- **Level 0-3 Value** – lote fijo o porcentaje de riesgo para cada capa pendiente.
- **Level 0-3 Pips** – desplazamientos de entrada para cada capa pendiente.

## Gestión del Dinero
Cuando `ManagementMode` es `RiskPercent`, la estrategia calcula el volumen de la orden a partir del capital de la cuenta y la distancia de stop loss configurada:

```
volumen de orden = (equity × riskPercent / 100) / (stopOffset / priceStep × stepPrice)
```

Si los metadatos del mercado (paso de precio, precio de paso o paso de volumen) no están disponibles, el tamaño de la orden retrocede a cero por seguridad. Con `FixedVolume`, los valores de la capa se usan directamente y se redondean al paso de volumen e intervalos del instrumento.

## Trailing y Protección
- Stop loss y take profit se inicializan usando distancias en pips relativas al precio de ejecución real.
- La lógica de trailing refleja la implementación de MetaTrader: el stop no se mueve hasta que el precio avanza `TrailingStop + TrailingStep`, y a partir de ahí mantiene una brecha de `TrailingStop`.
- Las salidas se emiten como órdenes a mercado, asegurando compatibilidad con plataformas que no soportan órdenes de protección del lado del servidor.

## Notas Prácticas
- Configurar las distancias en pips de acuerdo con el tamaño de tick del instrumento. Para símbolos FX de cinco dígitos, cada pip corresponde a diez pasos de precio y la estrategia se ajusta automáticamente inspeccionando los decimales del instrumento.
- Como la estrategia depende de órdenes stop, verificar los requisitos de nivel de stop específicos del bróker y ajustar las distancias de nivel si es necesario.
- El dimensionamiento por porcentaje de riesgo requiere una valoración válida del portafolio y metadatos de paso del instrumento; de lo contrario, el volumen de la orden evaluará a cero.
- La estrategia opera en velas completadas y por tanto reacciona una vez por barra, lo que suaviza el ruido comparado con el experto original basado en ticks.
