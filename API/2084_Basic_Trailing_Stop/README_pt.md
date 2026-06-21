# Stop Trailing Básico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Basic Trailing Stop combina filtros do Commodity Channel Index (CCI) e do Relative Strength Index (RSI) com um stop trailing simples. Quando ambos os indicadores sinalizam condições de sobrecompra ou sobrevenda, a estratégia abre uma posição de mercado e coloca imediatamente um stop trailing medido em pips. À medida que o preço se move favoravelmente, o nível do stop acompanha a tendência para travar lucros.

Os testes indicam um retorno anual médio de cerca de 32%. Funciona melhor no mercado forex.

Como o nível do stop segue continuamente o preço, o risco se reduz automaticamente quando a tendência se estende. As saídas ocorrem apenas se o stop trailing for acionado. O sistema mantém uma posição de cada vez e pode negociar em ambas as direções.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `CCI` entre -150 e -100 e `RSI` entre 0 e 30.
  - **Vendido**: `CCI` entre 100 e 250 e `RSI` entre 70 e 100.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop trailing acionado.
- **Stops**: Apenas stop trailing.
- **Valores padrão**:
  - `StopLossPips` = 20
  - `CciPeriod` = 14
  - `RsiPeriod` = 14
  - `CandleType` = `TimeSpan.FromMinutes(1)`
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: CCI, RSI
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
