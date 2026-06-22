# Estratégia EMA Crossover com Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port do StockSharp do assessor especializado MQL5 **"Intersection 2 iMA"**. Opera em duas médias móveis exponenciais (EMAs) e reage a cruzamentos que ocorrem em velas completamente formadas. O especialista original foi projetado para MetaTrader 5 e gerenciava o volume de negociação dinamicamente; nesta conversão o tamanho da ordem é controlado por um parâmetro configurável enquanto se preserva a lógica de cruzamento e trailing.

## Lógica de trading
1. **Geração de sinais**
   - Calcular EMAs rápida e lenta na série de velas selecionada.
   - Um **cruzamento altista** (EMA rápida cruzando acima da EMA lenta) aciona um sinal de compra quando a vela anterior fechou com a EMA rápida abaixo ou igual à EMA lenta e os valores atuais mostram a EMA rápida acima da EMA lenta.
   - Um **cruzamento baixista** (EMA rápida cruzando abaixo da EMA lenta) espelha a regra acima e produz um sinal de venda.
2. **Execução de ordens**
   - Quando um sinal de compra é produzido e não existe posição comprada, a estratégia envia uma ordem de compra a mercado.
   - Quando um sinal de venda é produzido e não existe posição vendida, a estratégia envia uma ordem de venda a mercado.
   - Se houver uma posição oposta, o volume da ordem é aumentado para fechar a posição existente antes de estabelecer a nova, correspondendo ao comportamento do EA fonte que primeiro fechava operações opostas.
3. **Gestão do trailing stop**
   - Um trailing stop escalonado mantém uma distância fixa (em passos de preço) do preço mais favorável.
   - O stop só se move quando o preço avançou um passo definido pelo usuário, prevenindo modificações constantes de ordens.
   - Se o preço violar o nível de trailing, a posição é fechada com uma ordem de mercado.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `FastPeriod` | Comprimento da EMA rápida. | 4 |
| `SlowPeriod` | Comprimento da EMA lenta. | 18 |
| `TrailingStopPoints` | Distância entre preço de mercado e trailing stop em passos de preço (pontos). Um valor de `0` desabilita o trailing. | 20 |
| `TrailingStepPoints` | Progresso mínimo em passos de preço antes de o trailing stop avançar. | 5 |
| `CandleType` | Série de dados de velas usada para cálculos (período). | Velas de 15 minutos |
| `TradeVolume` | Tamanho da ordem para entradas a mercado. | 1 |

## Notas de implementação
- A estratégia usa a API de alto nível `SubscribeCandles().Bind(...)` para conectar dados de velas com indicadores EMA, garantindo que nenhum gerenciamento manual de buffer seja necessário.
- As distâncias de trailing são calculadas multiplicando o número configurado de pontos pelo `PriceStep` do instrumento, replicando a lógica de ajuste de dígitos encontrada na versão MQL.
- Os trailing stops são implementados internamente usando saídas de mercado, porque o StockSharp não expõe o mesmo helper `PositionModify` usado no MetaTrader. O comportamento permanece equivalente: uma vez que o nível de trailing é violado, a posição é encerrada imediatamente.
- Os parâmetros são expostos através de `StrategyParam<T>` para que possam ser otimizados no designer ou ajustados a partir da UI.

## Dicas de uso
- Alinhar o `CandleType` com o período usado em backtests ou trading ao vivo para manter os valores do indicador consistentes.
- Ao negociar instrumentos com tamanhos de tick pequenos, ajustar `TrailingStopPoints` e `TrailingStepPoints` de acordo; a distância de preço efetiva equivale a *pontos × PriceStep*.
- Definir `TradeVolume` para corresponder ao contrato ou tamanho de lote desejado. A estratégia aumenta automaticamente o valor da ordem para fechar uma posição oposta quando um novo sinal aparece.

## Diferenças em relação ao Assessor Especializado original
- O gerenciamento de capital no MetaTrader usava `MoneyFixedMargin`; a versão StockSharp expõe um parâmetro de volume de ordem fixo, deixando o dimensionamento avançado de posições para configuração externa.
- O EA oferecia uma entrada `InpCloseHalf` não utilizada. Não tinha efeito no código-fonte e foi omitida.
- O trailing stop é gerenciado internamente em vez de modificar ordens de stop-loss, pois isso simplifica a execução dentro do StockSharp mantendo a lógica de saída idêntica.
