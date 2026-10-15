# Estratégia de Intervalo Estreito
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia negocia rompimentos após uma barra interna onde o intervalo da vela mais recente é mais estreito do que a barra de referência `Length` períodos atrás. Ordens stop são colocadas na máxima e mínima de referência com um take profit igual ao intervalo de referência e um stop loss definido como percentual desse intervalo.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o preço rompe acima da máxima de referência após uma barra de intervalo estreito
  - Vendido: o preço rompe abaixo da mínima de referência após uma barra de intervalo estreito
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Take profit no intervalo de referência
  - Stop loss como percentual do intervalo
- **Stops**: Sim
- **Valores padrão**:
  - `Length` = 4
  - `StopLossPercent` = 0.35m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
