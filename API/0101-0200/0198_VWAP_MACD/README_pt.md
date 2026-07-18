# Estratégia Vwap Macd
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada em VWAP e MACD. Entra comprado quando o preço está acima do VWAP e MACD > Sinal. Entra vendido quando o preço está abaixo do VWAP e MACD < Sinal. Sai quando o MACD cruza sua linha de sinal na direção oposta.

Os testes indicam um retorno anual médio de aproximadamente 181%. Funciona melhor no mercado de criptomoedas.

VWAP orienta o valor intradiário, e os cruzamentos do MACD revelam mudanças de momentum. As operações são iniciadas quando o MACD vira próximo ao nível VWAP.

Adequado para traders de momentum de curto prazo. As regras de stop ATR evitam risco excessivo.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > VWAP && MACD > Signal`
  - Vendido: `Close < VWAP && MACD < Signal`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: cruzamento do MACD em direção oposta
- **Stops**: Baseados em percentual usando `StopLossPercent`
- **Valores padrão**:
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: VWAP, MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

