# Estrategia de Exp XPVT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Exp XPVT** es una conversión del asesor experto de MetaTrader 5 *Exp_XPVT*. El sistema opera cruzamientos entre el indicador Price and Volume Trend (PVT) y una media móvil configurable aplicada a la serie PVT. Cuando la línea PVT cruda cae por debajo de su variante suavizada, la estrategia abre posiciones largas, mientras que los cruces al alza activan entradas cortas. Las distancias opcionales de stop-loss y take-profit emulan el comportamiento del asesor experto original.

## Lógica del indicador
- El Price and Volume Trend acumula cambios de precio ponderados por volumen usando el precio aplicado seleccionado (cierre, apertura, mediano, etc.).
- Un filtro de suavizado (SMA, EMA, MA suavizado, LWMA, Jurik, T3, aproximación VIDYA o Kaufman AMA) produce la línea de señal.
- Un desplazamiento histórico (`Signal Bar`) recrea la lógica MT5: la estrategia compara los valores suavizados y crudos de uno y dos barras atrás para detectar cruzamientos y condiciones de salida.
- Se puede usar volumen de tick o real para la ponderación. Si el tipo de volumen solicitado no está disponible, la estrategia recurre automáticamente a la otra fuente.

## Reglas de trading
1. En cada vela terminada, calcular el valor PVT del precio aplicado configurado y el tipo de volumen.
2. Actualizar el indicador de suavizado y almacenar los valores más recientes según `Signal Bar`.
3. Si la barra anterior mostró PVT por encima de la línea de señal, cerrar cualquier posición corta. Si, además, el PVT almacenado más reciente está por debajo o igual a la línea de señal, abrir una posición larga (si las entradas largas están habilitadas).
4. Si la barra anterior mostró PVT por debajo de la línea de señal, cerrar cualquier posición larga. Si, además, el PVT almacenado más reciente está por encima o igual a la línea de señal, abrir una posición corta (si las entradas cortas están habilitadas).
5. Después de entrar en una operación, se adjuntan órdenes opcionales de stop-loss y take-profit usando las distancias configuradas (expresadas en pasos de precio).
6. La gestión de dinero imita al asesor experto original: las nuevas órdenes usan el `Order Volume` base configurado e incluyen la exposición opuesta para invertir completamente al cambiar de dirección.

## Parámetros
- **Order Volume** – volumen base para nuevas órdenes e inversiones.
- **Stop Loss** – distancia en pasos de precio para el stop protector (0 lo deshabilita).
- **Take Profit** – distancia en pasos de precio para el objetivo de beneficio (0 lo deshabilita).
- **Allow Buy Entry / Allow Sell Entry** – habilitar la apertura de posiciones largas o cortas.
- **Allow Buy Exit / Allow Sell Exit** – habilitar el cierre automático de posiciones existentes cuando aparece la configuración opuesta.
- **Candle Type** – marco temporal usado para los cálculos del indicador.
- **Volume Source** – elegir volumen de tick o real para la ponderación PVT.
- **Smoothing Method / Length / Phase** – media móvil aplicada a la línea PVT. El parámetro de fase se usa solo en métodos estilo Jurik.
- **Applied Price** – fórmula de precio que alimenta el PVT (cierre, apertura, seguimiento de tendencia, DeMark, etc.).
- **Signal Bar** – desplazamiento histórico (en barras) usado para evaluar el cruzamiento, reproduciendo la implementación MT5.

## Notas
- La estrategia procesa solo velas terminadas para asegurar la estabilidad del indicador.
- El suavizado estilo Jurik usa reflexión para reenviar el parámetro de fase cuando el indicador lo expone.
- Cuando no hay volumen de tick ni real disponible, la estrategia recurre a volumen cero, previniendo acumulaciones espurias.
- La llamada opcional `StartProtection` activa el monitoreo de posición integrado de StockSharp, coincidiendo con la invocación única en el asesor experto original.
