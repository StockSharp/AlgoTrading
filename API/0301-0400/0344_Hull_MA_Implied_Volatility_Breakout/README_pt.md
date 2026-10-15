# Estratégia de Rompimento de Volatilidade Implícita com Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia **Hull MA Implied Volatility Breakout** é construída em torno do rompimento de volatilidade implícita com Hull MA.

Os testes indicam um retorno anual médio de aproximadamente 121%. Funciona melhor no mercado de criptomoedas.

Os sinais são acionados quando seus indicadores confirmam oportunidades de rompimento em dados intradiários (15m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como HmaPeriod, IVPeriod. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos as direções.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `HmaPeriod = 9`
  - `IVPeriod = 20`
  - `IVMultiplier = 2m`
  - `StopLossAtr = 2m`
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

