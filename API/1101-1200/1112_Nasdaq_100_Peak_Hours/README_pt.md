# Estratégia de Horários de Pico do Nasdaq 100
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera o Nasdaq 100 apenas durante as duas primeiras horas e a última hora da sessão. Utiliza confirmação de tendência EMA, filtros RSI, ATR e VWAP com trailing stops e stops de break-even baseados em ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Preço acima da EMA curta, EMA curta acima da EMA longa, ambas as EMAs em ascensão, RSI acima de 50 e preço acima do VWAP durante as horas de pico da sessão.
  - **Vendido**: Condições opostas.
- **Comprado/Vendido**: Comprado e vendido.
- **Critérios de saída**:
  - Trailing stop baseado em ATR ou stop de break-even.
  - Saída temporal após número configurável de barras ou reversão de tendência EMA.
- **Stops**: Trailing ATR com break-even.
- **Valores padrão**:
  - `Long EMA` = 21
  - `Short EMA` = 9
  - `RSI` = 14
  - `ATR` = 14
  - `Trail ATR Mult` = 1.5
  - `Initial SL Mult` = 0.5
  - `Break-even ATR Mult` = 1.5
  - `Time Exit Bars` = 20
- **Filtros**:
  - Categoria: Intradiário
  - Direção: Ambos
  - Indicadores: EMA, RSI, ATR, VWAP
  - Stops: Trailing
  - Complexidade: Avançado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
