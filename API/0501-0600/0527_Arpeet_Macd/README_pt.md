# Estratégia Arpeet MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Arpeet MACD opera cruzamentos de MACD com um filtro de linha zero. Um sinal comprado aparece quando a linha MACD cruza acima da linha de sinal enquanto permanece abaixo de zero. Um sinal vendido ocorre quando o MACD cruza abaixo da linha de sinal acima de zero.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: MACD cruza acima da sinal e MACD < 0.
  - **Vendido**: MACD cruza abaixo da sinal e MACD > 0.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
- **Filtros**:
  - Categoria: Indicador
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
