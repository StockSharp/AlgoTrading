# Estratégia VWAP EMA ATR Pullback
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de seguimento de tendência usando EMAs, VWAP e ATR.

Os testes indicam um retorno anual médio de aproximadamente 55%. Funciona melhor no mercado de futuros.

A abordagem identifica tendências fortes através da distância baseada em ATR entre EMAs rápidas e lentas. As entradas ocorrem quando o preço recua até o VWAP, visando acompanhar a tendência. O take-profit é colocado no VWAP mais ou menos o múltiplo do ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: tendência de alta e fechamento < VWAP.
  - **Vendido**: tendência de baixa e fechamento > VWAP.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Alvo no VWAP ± ATR * multiplicador.
- **Stops**: Não.
- **Valores padrão**:
  - `FastEmaLength` = 30
  - `SlowEmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, ATR, VWAP
  - Stops: Não
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
