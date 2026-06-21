# Estratégia Adaptive Cyber Cycle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o oscilador Adaptive Cyber Cycle de John Ehlers. Ela calcula um ciclo de preço suavizado e usa o valor anterior como linha de gatilho. Uma posição comprada é aberta quando o ciclo cruza acima da linha de gatilho, e uma posição vendida quando cruza abaixo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: ciclo > ciclo anterior.
  - **Vendido**: ciclo < ciclo anterior.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O sinal oposto fecha e reverte a posição.
- **Stops**: Nenhum por padrão; a proteção pode ser habilitada separadamente.
- **Valores padrão**:
  - `Alpha` = 0.07
  - `Candle Type` = período de 1 minuto
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Adaptive Cyber Cycle
  - Stops: Opcional
  - Complexidade: Moderado
  - Período: Intradiário
