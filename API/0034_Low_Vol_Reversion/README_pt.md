# Low Volatility Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia de reversão à média é ativada apenas durante mercados calmos. Ela mede o ATR ao longo de uma janela de retrospectiva e entra quando a volatilidade cai abaixo de uma porcentagem dessa média e o preço se desvia da sua média móvel.

Os testes indicam um retorno anual médio de aproximadamente 139%. Funciona melhor no mercado de ações.

Ao operar contra pequenas movimentações em condições calmas, visa capturar recuos sem perseguir grandes tendências.

As posições fecham assim que o preço toca a média móvel ou o stop-loss baseado em ATR é atingido.

## Detalhes

- **Critérios de entrada**: Preço afastado da média móvel enquanto o ATR está abaixo do limiar.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: O preço retorna à MA ou o stop é ativado.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrLookbackPeriod` = 20
  - `AtrThresholdPercent` = 50m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: ATR, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

