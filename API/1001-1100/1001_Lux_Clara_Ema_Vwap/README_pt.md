# Estratégia Lux Clara EMA + VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Lux Clara EMA + VWAP opera o cruzamento de uma EMA rápida e uma lenta, filtrado pelo VWAP e uma janela de tempo. Uma posição comprada é aberta quando a EMA rápida cruza acima da EMA lenta enquanto a EMA lenta está acima do VWAP durante a sessão. Uma posição vendida é aberta nas condições opostas. As posições são encerradas quando as EMA cruzam na direção oposta.

## Detalhes

- **Critérios de entrada**:
  - EMA rápida cruza acima da EMA lenta, EMA lenta acima do VWAP e hora atual dentro da sessão.
  - Vendido: EMA rápida cruza abaixo da EMA lenta, EMA lenta abaixo do VWAP e hora atual dentro da sessão.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Cruzamento oposto de EMA.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `FastEmaLength` = 8
  - `SlowEmaLength` = 50
  - `StartTime` = 07:30
  - `EndTime` = 14:30
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: EMA, VWAP
  - Stops: Nenhum
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
