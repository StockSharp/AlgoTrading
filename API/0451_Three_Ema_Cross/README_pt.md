# Estratégia de Cruzamento de Três EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Three EMA Cross combina um clássico cruzamento de média móvel rápida/lenta
com um filtro de tendência mais longo. Após a EMA rápida cruzar acima da EMA lenta, a
estratégia aguarda um pullback para a média rápida enquanto o preço de fechamento
permanece acima de uma EMA de tendência mais ampla. Esta configuração tenta capturar
movimentos de continuação após uma breve correção dentro da tendência prevalente.

As posições são encerradas quando o momentum se esvai e a EMA rápida cai novamente
abaixo da EMA lenta. Um stop loss baseado em porcentagem protege a posição se o preço
se mover contra a operação. A técnica funciona bem em mercados com tendências persistentes
e tende a evitar ranges laterais.

## Detalhes

- **Critérios de entrada**:
  - Cruzamento recente da EMA rápida acima da EMA lenta dentro das últimas *N* barras.
  - Fechamento atual ≥ EMA rápida e mínimo de sessão ≤ EMA rápida.
  - EMA de tendência ≤ fechamento atual.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - EMA rápida cai abaixo da EMA lenta.
- **Stops**: Stop loss em `stop_loss_percent` do preço de entrada.
- **Valores padrão**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 20
  - `TrendEmaLength` = 100
  - `StopLossPercent` = 2.0
  - `CrossBackBars` = 10
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
