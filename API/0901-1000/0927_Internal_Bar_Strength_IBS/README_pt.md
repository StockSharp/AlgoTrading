# Estratégia IBS de Força Interna de Barra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprada quando a força interna de barra (IBS) está abaixo de um limiar inferior e sai quando o IBS sobe acima de um limiar superior dentro de uma janela de tempo especificada.

## Detalhes

- **Critérios de entrada**:
  - IBS < `LowerThreshold`.
  - Tempo entre `StartTime` e `EndTime`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - IBS >= `UpperThreshold`.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `UpperThreshold` = 0.8
  - `LowerThreshold` = 0.2
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Somente comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
