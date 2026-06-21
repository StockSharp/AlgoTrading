# Estratégia de ORB do Ouro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia captura a máxima e mínima da sessão asiática e opera rompimentos durante as horas seguintes. Stops e alvos são derivados do tamanho do intervalo com um multiplicador de recompensa.

## Detalhes

- **Critérios de entrada**:
  - Durante a janela de negociação, ir comprado quando o preço fechar acima da máxima asiática registrada.
  - Ir vendido quando o preço fechar abaixo da mínima asiática registrada.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Stop e alvo baseados no tamanho do intervalo e no multiplicador de recompensa.
- **Stops**: Sim
- **Valores padrão**:
  - `AsiaStart` = 00:00
  - `AsiaEnd` = 06:00
  - `TradeStart` = 06:00
  - `TradeEnd` = 10:00
  - `RewardMultiplier` = 2.0
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Baixo
  - Período: 5m
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

