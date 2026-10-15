# Parabolic SAR Multitemporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Parabolic SAR Multitemporal usa cuatro indicadores Parabolic SAR diferentes de marcos temporales superiores
para confirmar una tendencia antes de entrar en una operación. La estrategia procesa velas de 15 minutos y comprueba el
estado del SAR en gráficos de 30 minutos, 1 hora y 4 horas. Solo se abre una posición larga cuando el precio está
por encima de todos los valores SAR; se abre una posición corta cuando el precio está por debajo de todos los SAR.

El método intenta filtrar el ruido requiriendo alineación en múltiples marcos temporales. La posición
se cierra cuando aparece la condición opuesta.

## Detalles

- **Criterios de entrada**: Precio relativo al Parabolic SAR en marcos temporales de 15m/30m/1h/4h.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta de todos los indicadores SAR.
- **Stops**: Usa `StartProtection` para protección básica, sin valores de stop explícitos.
- **Valores predeterminados**:
  - `Step15` = 0.062
  - `Step30` = 0.058
  - `Step60` = 0.058
  - `Step240` = 0.058
  - `MaxStep` = 0.1
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía (base 15m con confirmaciones superiores)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Uso

1. Adjunte la estrategia a un instrumento.
2. Ajuste los parámetros de paso SAR si es necesario.
3. Inicie la estrategia; se suscribirá automáticamente a velas de 15m, 30m, 1h y 4h.
