# Estrategia Gridder EA (portada desde MQL4)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
El GridderEA original es un asesor experto de trading grid multisímbolo diseñado para MetaTrader 4. Este port StockSharp conserva los conceptos centrales — espaciado progresivo, tamaño de lote adaptativo, take-profit de cesta y cobertura de emergencia — mientras se centra en un solo instrumento gestionado por la estrategia anfitriona. La estrategia se suscribe a un flujo de velas configurable, observa barras terminadas y abre operaciones de promediado cuando el precio se aleja del último nivel de referencia por una distancia definida en pips.

## Lógica de negociación
1. **Progresión del grid:** un paso base (en pips) define el movimiento mínimo de precio requerido antes de colocar una nueva operación. Cada orden adicional puede escalar este paso geométrica o exponencialmente para expandir el grid cuando aumenta la volatilidad.
2. **Progresión de lote:** la primera orden usa el volumen inicial. Las órdenes posteriores multiplican el volumen anterior según el modo configurado de progresión de lote (estático, geométrico o exponencial).
3. **Objetivos de cesta:** el beneficio y pérdida no realizados se miden en moneda de cuenta combinando la desviación de precio de cada operación abierta con el valor de paso del instrumento. Cuando el beneficio total supera el objetivo por lote, todas las posiciones se cierran. Del mismo modo, un objetivo de pérdida por lote puede liquidar la cesta como stop protector.
4. **Modo de emergencia:** cuando el número de operaciones de un lado alcanza el disparador de emergencia, la estrategia puede abrir una cobertura con tamaño fraccional del volumen acumulado. Esto imita el "Emergency Mode" de MQL y ayuda a limitar drawdowns.
5. **Protección de posición:** `StartProtection()` se invoca al inicio para asegurar que la estrategia base monitorice cambios inesperados de posición y se resincronice con el estado de bolsa.

La implementación StockSharp evita manipular grandes colecciones históricas y procesa solo velas terminadas, reflejando el comportamiento del experto original sobre barras completadas.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| **Initial Volume** | Volumen de la primera orden del grid. |
| **Volume Multiplier** | Factor aplicado para calcular el siguiente volumen cuando la progresión de lote es geométrica o exponencial. |
| **Grid Step (pips)** | Distancia base (en pips) entre entradas sucesivas. |
| **Step Multiplier** | Factor de escalado del espaciado del grid cuando la progresión de paso es geométrica o exponencial. |
| **Target Profit / Lot** | Objetivo de beneficio no realizado expresado por lote. Alcanzarlo cierra todas las operaciones. |
| **Target Loss / Lot** | Umbral de pérdida no realizada por lote. Al alcanzarse, todas las operaciones se cierran para contener drawdown. |
| **Max Orders Per Side** | Limita el número de operaciones de promediado permitidas en cada lado del mercado. `0` desactiva el límite. |
| **Allow Long / Allow Short** | Activa o desactiva patas de compra/venta de forma independiente. |
| **Step Mode** | Determina cómo crece el paso: estático, geométrico o exponencial. |
| **Lot Mode** | Determina cómo crece el volumen de orden: estático, geométrico o exponencial. |
| **Use Emergency Mode** | Activa la lógica de cobertura que protege contra cestas sobredimensionadas. |
| **Emergency Trigger** | Número de órdenes en un lado que activa la cobertura. |
| **Hedge Volume Factor** | Fracción del volumen total del lado que se coloca como cobertura cuando se activa el modo de emergencia. |
| **Candle Type** | Marco temporal de la suscripción de velas usado para cálculos del grid. |

## Diferencias frente al EA original
- El port gestiona una sola seguridad a la vez; adjunte múltiples instancias de estrategia para operar varios instrumentos y replicar el comportamiento multisímbolo del experto MQL.
- No se reproducen paneles de pantalla ni anotaciones de gráfico de MetaTrader; use áreas de gráfico StockSharp para visualizar velas y operaciones propias si lo desea.
- Los presets de gestión monetaria y perfiles detallados de cierre parcial se simplifican en la lógica unificada de beneficio/pérdida de cesta.

## Notas de uso
1. Configure el tipo de vela, volumen y espaciado del grid en los parámetros del constructor (mediante UI o interfaz de optimización).
2. Inicie la estrategia cuando la seguridad esté conectada a una placa real o simulada. La estrategia se suscribe automáticamente a las velas seleccionadas.
3. Supervise el disparador de emergencia y el factor de cobertura para ajustar la agresividad de la fase de recuperación. Un factor más alto devuelve la posición neta a neutral más rápido, pero reduce rentabilidad.
4. Combine con controles de riesgo StockSharp (protección de cartera, vigilante de posición máxima, etc.) para mayor seguridad.

## Ejemplo de cobertura de emergencia
Suponga que la estrategia ha abierto cinco órdenes de compra de promediado con volúmenes crecientes. Si el disparador de emergencia está en cinco y el factor de cobertura en 0.5, en cuanto la quinta compra se ejecuta la estrategia enviará una venta automática a mercado por la mitad del volumen largo total. Esto refleja la lógica MQL que bloquea parcialmente la cesta y espera una salida por reversión a la media.

## Consejos de optimización
- Optimice **Grid Step (pips)** y **Volume Multiplier** juntos; pasos pequeños requieren multiplicadores conservadores para evitar exposición descontrolada.
- Use **Target Profit / Lot** para trasladar objetivos en dólares de MetaTrader al entorno StockSharp sin depender de historial de operaciones cerradas.
- Ajuste **Emergency Trigger** y **Hedge Volume Factor** según la volatilidad del instrumento. Mayor volatilidad suele beneficiarse de cobertura más temprana.

## Recomendaciones de seguridad
- Pruebe extensamente en simulador antes de desplegar en producción.
- Supervise tamaños contractuales específicos del broker para asegurar que el volumen redondeado coincida con la granularidad real del lote.
- Combine con reglas de stop-out (por ejemplo, mediante el robot anfitrión) para evitar pérdidas catastróficas en mercados tendenciales donde los grids pueden acumular posiciones grandes.
