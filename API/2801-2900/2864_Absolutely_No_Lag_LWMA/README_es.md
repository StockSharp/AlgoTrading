# Estrategia LWMA Absolutamente Sin Retraso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia replica el asesor experto de MetaTrader **Exp_AbsolutelyNoLagLwma** aplicando una doble media móvil ponderada (LWMA) a los datos de velas. La salida del indicador está codificada por colores: verde (2) para una pendiente ascendente, gris (1) para plano, y magenta (0) para una pendiente descendente. Las decisiones de trading se basan en transiciones entre estos estados de color. La implementación en StockSharp usa la API de alto nivel, se suscribe a velas de timeframe y envía órdenes de mercado según la dirección de tendencia detectada.

## Lógica de trading
### Pipeline del indicador
1. Seleccionar la serie de precios deseada definida por el parámetro *Tipo de precio*.
2. Aplicar una media móvil ponderada (LWMA) con la *Longitud LWMA* configurada.
3. Suavizar el resultado con una segunda LWMA de la misma longitud.
4. Comparar el valor LWMA suavizado con el valor previo para clasificar la dirección de la pendiente:
   - **2 (tendencia alcista)** – el valor actual es mayor que el valor previo.
   - **1 (neutral)** – el valor actual es igual al valor previo.
   - **0 (tendencia bajista)** – el valor actual es menor que el valor previo.

### Evaluación de señales
- Solo se procesan velas completadas. El parámetro *Barra de señal* desplaza la evaluación de la señal a velas históricas (1 = vela terminada anterior, 2 = la vela antes de esa, etc.). La estrategia también recuerda el color de la barra que precede a la vela de señal seleccionada para evitar entradas duplicadas.
- **Transición alcista**: la vela de señal seleccionada es color **2** y la vela anterior no es **2**. Esto abre largos (si está habilitado) y cierra cortos existentes.
- **Transición bajista**: la vela de señal seleccionada es color **0** y la vela anterior no es **0**. Esto abre cortos (si está habilitado) y cierra largos existentes.

### Gestión de posiciones
- Las órdenes se ejecutan con órdenes de mercado. El volumen solicitado es `Volume + |Position|` cuando se invierte la dirección para que la posición opuesta se cierre automáticamente.
- Las señales de salida se pueden activar independientemente de las entradas, permitiendo comportamiento solo de señal o solo de salida.
- `StartProtection()` se activa para activar la lógica de protección común de StockSharp una vez que la estrategia comienza.

## Parámetros
- **Longitud LWMA** – longitud de las dos LWMAs usadas para el suavizado.
- **Tipo de precio** – fuente de precio que alimenta la LWMA (cierre, apertura, máximo, mínimo, mediano, típico, ponderado, simplificado, cuarto, variaciones de seguimiento de tendencia, o precio Demark).
- **Barra de señal** – número de velas terminadas atrás usadas para la evaluación de señales.
- **Habilitar entradas largas** – permite abrir posiciones largas en transiciones alcistas.
- **Habilitar entradas cortas** – permite abrir posiciones cortas en transiciones bajistas.
- **Habilitar salidas largas** – cierra posiciones largas cuando el indicador se vuelve bajista.
- **Habilitar salidas cortas** – cierra posiciones cortas cuando el indicador se vuelve alcista.
- **Tipo de vela** – timeframe de la suscripción de velas usada por el indicador.
- **Volumen** (propiedad de Strategy incorporada) – tamaño de operación para nuevas entradas.

## Notas
- El timeframe predeterminado es 4 horas, coincidiendo con la configuración del asesor experto original, pero puede ajustarse mediante el parámetro *Tipo de vela*.
- No se colocan órdenes de take-profit ni stop-loss automáticamente; los usuarios pueden combinar la estrategia con los componentes de gestión de riesgo de StockSharp si se requiere.
- El port de Python se omite intencionalmente según lo solicitado.
