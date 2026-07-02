# Estratégia Bollinger Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia combina as Bandas de Bollinger com o indicador Supertrend para identificar entradas durante movimentos direcionais fortes. As Bandas de Bollinger medem a expansão da volatilidade enquanto a linha Supertrend acompanha a tendência geral e atua como trailing stop.

Os testes indicam um retorno anual médio de aproximadamente 79%. Funciona melhor no mercado de ações.

Um trade comprado é acionado quando o preço fecha acima da Banda de Bollinger superior e permanece acima da linha Supertrend, confirmando o alinhamento do momentum e da tendência. Um trade vendido ocorre quando o preço fecha abaixo da banda inferior enquanto permanece abaixo do nível Supertrend. Os trades são encerrados assim que o preço cruza de volta pelo Supertrend, indicando que o momentum desapareceu.

Como o sistema aguarda rompimentos além da volatilidade normal, é adequado para traders que buscam capturar movimentos sustentados em vez de reversões rápidas. O stop Supertrend se ajusta dinamicamente aos movimentos do mercado, ajudando a gerenciar o risco sem intervenção manual.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Close > upper Bollinger Band && Close > Supertrend
  - **Vendido**: Close < lower Bollinger Band && Close < Supertrend
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando o preço cruzar abaixo do Supertrend
  - **Vendido**: Sair quando o preço cruzar acima do Supertrend
- **Stops**: Sim, via trailing stop Supertrend.
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Supertrend
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

