# Estratégia DSL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia combina Linhas de Sinal Descontinuadas (DSL) com bandas ATR e um oscilador Beluga. Uma posição comprada é aberta quando o preço permanece acima da linha DSL por três barras e o oscilador cruza acima de sua linha DSL inferior. Posições vendidas são abertas nas condições opostas. Cada operação usa a banda DSL correspondente como stop e um alvo de risco-retorno para o take profit.

## Detalhes

- **Critérios de entrada**:
  - Banda superior DSL acima da linha inferior para comprados; banda inferior abaixo da linha superior para vendidos.
  - Abertura e fechamento da vela acima (ou abaixo) da linha DSL por três barras consecutivas.
  - Sinal de cruzamento do oscilador DSL-Beluga.
- **Comprado/Vendido**: Comprado e vendido.
- **Critérios de saída**:
  - Stop loss na banda DSL.
  - Take profit no múltiplo de risco-retorno.
- **Stops**: Sim, baseados em ATR.
- **Valores padrão**:
  - `Length` = 34
  - `Offset` = 30
  - `BandsWidth` = 1
  - `RiskReward` = 1.5
  - `BelugaLength` = 10
  - `DslFastMode` = true
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: DSL, ATR, RSI
  - Stops: Sim
  - Complexidade: Alto
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
