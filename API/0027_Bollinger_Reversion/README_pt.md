# Bollinger Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada na reversão à média das Bandas de Bollinger

Os testes indicam um retorno anual médio de aproximadamente 118%. Funciona melhor no mercado de ações.

Bollinger Reversion opera contra os movimentos fora das Bandas de Bollinger. As operações abrem contra fechamentos além das bandas e fecham quando o preço retorna ao interior ou atinge um stop.

As bandas de desvio padrão oferecem uma visão estatística da sobreextensão. Entrar após fechamentos extremos visa lucrar com o retrocesso em direção à banda média.


## Detalhes

- **Critérios de entrada**: Sinais baseados em RSI, ATR, Bollinger.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: RSI, ATR, Bollinger
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

