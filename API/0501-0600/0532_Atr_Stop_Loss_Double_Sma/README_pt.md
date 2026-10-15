# Estratégia ATR Stop Loss com SMA Duplo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprado quando uma Média Móvel Simples (SMA) rápida cruza acima de uma SMA lenta e entra vendido no cruzamento oposto.
Um stop-loss opcional usa o Average True Range (ATR) multiplicado por um fator definido pelo usuário para determinar os níveis de saída.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: SMA rápido cruza acima do SMA lento.
  - **Vendido**: SMA rápido cruza abaixo do SMA lento.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Stop-loss baseado em ATR se habilitado.
- **Stops**: Múltiplo de ATR a partir do preço de entrada.
- **Valores padrão**:
  - `FastLength` = 15
  - `SlowLength` = 45
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, ATR
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
