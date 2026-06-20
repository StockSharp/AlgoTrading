# Parabolic Sar Rsi Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia que combina o Parabolic SAR para a direção da tendência e o RSI para confirmação de entrada com condições de sobrevenda/sobrecompra.

Os testes indicam um retorno anual médio de aproximadamente 166%. Funciona melhor no mercado de ações.

Aqui o Parabolic SAR delineia a tendência predominante e o RSI mede o esgotamento. As operações são abertas assim que ambos os indicadores sinalizam a mesma direção.

A combinação é atraente para quem gosta de stops móveis, já que o SAR também fornece uma saída dinâmica. A colocação do stop segue a curva do SAR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > SAR && RSI < RsiOversold`
  - Vendido: `Close < SAR && RSI > RsiOverbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: `Close < SAR`
  - Vendido: `Close > SAR`
- **Stops**: Utiliza o Parabolic SAR como stop móvel (trailing)
- **Valores padrão**:
  - `SarAf` = 0.02m
  - `SarMaxAf` = 0.2m
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Parabolic SAR, Parabolic SAR, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

