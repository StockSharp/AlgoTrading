# Estratégia GRIM309 CallPut
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia GRIM309 CallPut opera com base no alinhamento de múltiplas EMAs com um sistema de aviso. Posições compradas entram quando as EMAs de curto prazo confirmam uma tendência de alta e a EMA5 sobe acima da EMA10. Posições vendidas entram nas condições opostas. Um período de resfriamento impede a reentrada imediata após um fechamento. Um aviso adicional aciona saídas antecipadas quando o spread EMA5-EMA10 se contrai rapidamente.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: EMA10 acima de EMA20, preço acima de EMA50, EMA5 subindo acima de EMA10, sem posição e período de resfriamento satisfeito.
  - **Vendido**: EMA10 abaixo de EMA20, preço abaixo de EMA50, EMA5 caindo abaixo de EMA10, sem posição e período de resfriamento satisfeito.
- **Critérios de saída**: Preço cruzando EMA15 ou sinal de aviso.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Ema5Length` = 5
  - `Ema10Length` = 10
  - `Ema15Length` = 15
  - `Ema20Length` = 20
  - `Ema50Length` = 50
  - `Ema200Length` = 200
  - `CooldownBars` = 2
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: EMA
  - Complexidade: Moderado
  - Nível de risco: Médio
