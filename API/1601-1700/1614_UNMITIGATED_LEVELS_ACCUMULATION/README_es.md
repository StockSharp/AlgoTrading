# Estrategia de Acumulación en Niveles No Mitigados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Acumula posiciones largas colocando órdenes límite en los mínimos anteriores de día, semana, mes y año que no han sido revisitados recientemente. Las órdenes solo se colocan durante la sesión de Londres y todas las posiciones se cierran en nuevos máximos históricos.

## Detalles

- **Criterios de entrada**:
  - Compras limitadas en mínimos históricos no mitigados durante las horas de sesión.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Cerrar todo en nuevo máximo histórico.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Max Lookback` = 50
  - `Session Start` = 09:00
  - `Session End` = 17:00
  - `Base PDL` = 0.1
  - `Base PWL` = 0.2
  - `Base PML` = 0.4
  - `Base PYL` = 0.8
- **Filtros**:
  - Categoría: Mean Reversion
  - Dirección: Solo largos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: Sí (sesión de Londres)
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
