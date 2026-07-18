# Estratégia Jpalonso Modoki
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Jpalonso Modoki opera um canal de preço construído a partir de uma média móvel simples.
Os envelopes superior e inferior são calculados aplicando um desvio percentual à média móvel.
O sistema vai comprado quando o preço toca a banda inferior ou quando permanece na metade superior do canal.
Vai vendido nas situações opostas. Take-profit e stop-loss fixos protegem a posição.

## Detalhes

- **Critérios de entrada**: Preço abaixo do envelope inferior ou entre a linha central e a banda superior para comprado; preço acima do envelope superior ou entre a linha central e a banda inferior para vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou níveis de stop.
- **Stops**: Sim, take-profit e stop-loss em pontos.
- **Valores padrão**:
  - `CandleType` = 1 minuto
  - `SmaPeriod` = 200
  - `Deviation` = 0.35%
  - `TakeProfit` = 127 pontos
  - `StopLoss` = 77 pontos
- **Filtros**:
  - Categoria: Canal
  - Direção: Ambos
  - Indicadores: SMA, Envelopes
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
