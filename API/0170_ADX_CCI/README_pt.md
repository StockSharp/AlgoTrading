# Adx Cci Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada nos indicadores ADX e CCI. Entra comprado quando ADX > 25 e o CCI está sobrevendido (< -100). Entra vendido quando ADX > 25 e o CCI está sobrecomprado (> 100).

Os testes indicam um retorno anual médio de aproximadamente 97%. Funciona melhor no mercado de criptomoedas.

O ADX avalia se uma tendência tem força e o CCI identifica o momento de entrada após recuos. Posições compradas e vendidas seguem a direção do ADX.

Voltado para traders de momentum que entram em retrações. Múltiplos de ATR gerenciam o risco.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `ADX > 25 && CCI < -100`
  - Vendido: `ADX > 25 && CCI > 100`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: A tendência enfraquece ou o CCI cruza o zero
- **Stops**: Baseados em porcentagem usando `StopLossPercent`
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `CciPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: ADX, CCI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

