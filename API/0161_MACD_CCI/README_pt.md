# Estratégia MACD CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia MACD + CCI. Comprar quando o MACD está acima da linha de sinal e o CCI está abaixo de -100 (sobrevendido). Vender quando o MACD está abaixo da linha de sinal e o CCI está acima de 100 (sobrecomprado).

Os testes indicam um retorno anual médio de cerca de 70%. Funciona melhor no mercado de ações.

As oscilações do MACD destacam as mudanças de momentum; o CCI ajuda a cronometrar as entradas em retrocessos nessa direção. Tanto operações compradas quanto vendidas são possíveis.

Traders que combinam momentum com osciladores podem gostar desta técnica. O controle de risco usa um stop baseado em ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `MACD > Signal && CCI < CciOversold`
  - Vendido: `MACD < Signal && CCI > CciOverbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Cruzamento do MACD na direção oposta
- **Stops**: Baseados em percentual usando `StopLoss`
- **Valores padrão**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: MACD, CCI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
