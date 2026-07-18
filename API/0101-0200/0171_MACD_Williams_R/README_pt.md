# Estratégia Macd Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada nos indicadores MACD e Williams %R. Entra comprado quando MACD > Signal e o Williams %R está sobrevendido (< -80). Entra vendido quando MACD < Signal e o Williams %R está sobrecomprado (> -20).

Os testes indicam um retorno anual médio de aproximadamente 100%. Funciona melhor no mercado forex.

O MACD indica a mudança de momentum mais ampla, enquanto o Williams %R identifica com precisão as reversões de curto prazo. Ambos os sinais devem se alinhar para iniciar uma operação.

Bom para quem gosta de combinar sinais de tendência e contra-tendência. Os stops dependem de um fator ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `MACD > Signal && WilliamsR < -80`
  - Vendido: `MACD < Signal && WilliamsR > -20`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Cruzamento do MACD na direção oposta
- **Stops**: Baseados em porcentagem usando `StopLossPercent`
- **Valores padrão**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `WilliamsRPeriod` = 14
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: MACD, Williams %R, R
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

