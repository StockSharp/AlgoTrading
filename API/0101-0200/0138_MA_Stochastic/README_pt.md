# Estratégia MA Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
MA Stochastic usa um filtro de tendência de média móvel com pullbacks do oscilador estocástico.
Quando o preço está em tendência de alta acima da média e o estocástico cai para a zona de sobrevenda, o sistema se prepara para comprar na próxima reversão de alta.

Os testes indicam um retorno anual médio de aproximadamente 151%. Funciona melhor no mercado de ações.

As operações vendidas espelham essa lógica para tendências de baixa, vendendo rallies quando o estocástico atinge a sobrecompra.

Stops percentuais fixos ajudam a evitar grandes perdas caso a tendência reverta subitamente.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Moving Average, Stochastic
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

