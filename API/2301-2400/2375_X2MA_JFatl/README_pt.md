# Estratégia de Cruzamento X2MA JFatl
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma adaptação do StockSharp do expert MetaTrader `Exp_X2MA_JFatl`. Ela combina uma Média Móvel Simples (SMA) rápida com uma Média Móvel Jurik (JMA) lenta e um filtro JMA adicional para confirmar a direção da tendência. As operações são abertas quando a média rápida cruza a lenta e o preço está do mesmo lado do filtro. As posições são fechadas quando o preço se move contra o filtro ou ocorre um cruzamento oposto.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `SMA_fast` cruza acima de `JMA_slow` e `Close` > `JMA_filter`.
  - **Vendido**: `SMA_fast` cruza abaixo de `JMA_slow` e `Close` < `JMA_filter`.
- **Critérios de saída**:
  - O preço move-se para o lado oposto do filtro.
  - Cruzamento oposto das médias.
- **Comprado/Vendido**: Ambos os lados.
- **Stops**: Não utilizados por padrão.
- **Valores padrão**:
  - `Fast MA Length` = 5.
  - `Slow MA Length` = 12.
  - `Filter Length` = 20.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos (SMA, JMA)
  - Stops: Não
  - Complexidade: Moderado
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
