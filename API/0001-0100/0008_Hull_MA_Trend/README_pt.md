# Tendência Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada na tendência da Hull Moving Average.

Os testes indicam um retorno anual médio de aproximadamente 61%. Funciona melhor no mercado de criptomoedas.

A estratégia de Tendência Hull MA monitora a inclinação da Hull Moving Average. Inclinações crescentes provocam posições compradas e inclinações decrescentes provocam posições vendidas, com um stop trailing de ATR protegendo cada operação.

Seu cálculo responsivo reduz a defasagem em comparação com as médias móveis tradicionais, permitindo ao sistema reagir rapidamente ao novo momentum. O stop de ATR ajuda a evitar grandes drawdowns se a inclinação mudar abruptamente.


## Detalhes

- **Critérios de entrada**: Sinais baseados em MA, ATR.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `HmaPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

