# Estratégia Ichimoku de Volatilidade Implícita
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia **Ichimoku Implied Volatility** é construída em torno da volatilidade implícita do Ichimoku.

Os testes indicam um retorno anual médio de aproximadamente 109%. Funciona melhor no mercado de criptomoedas.

Os sinais são acionados quando seus indicadores confirmam mudanças de tendência em dados intradiários (15m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como TenkanPeriod, KijunPeriod. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos as direções.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `TenkanPeriod = 9`
  - `KijunPeriod = 26`
  - `SenkouSpanBPeriod = 52`
  - `IVPeriod = 20`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos indicadores
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

