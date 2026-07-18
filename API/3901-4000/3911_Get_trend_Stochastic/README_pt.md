# Obtenha tendência Stochastic Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia é uma versão StockSharp de alto nível do consultor especialista MetaTrader 4 **Get trend.mq4**. Ele avalia o gráfico M15 para
entradas, valida a tendência mais ampla no H1 e se baseia em duas médias móveis suavizadas juntamente com um par de
osciladores para detectar rompimentos de reversão à média próximos à tendência de longo prazo. A implementação mantém a gestão de dinheiro original
regras baseadas em distâncias fixas de take-profit, stop-loss e trailing-stop expressas em pontos de preço.

## Lógica de negociação

1. **Indicadores e dados**
   - As velas M15 alimentam uma média móvel suavizada (SMMA, preço mediano) com período `M15MaPeriod` e dois osciladores estocásticos.
   - As velas H1 alimentam outro SMMA (preço médio) com período `H1MaPeriod`.
   - O estocástico rápido (`FastStochasticPeriod`, 3, 3) fornece a linha %K e seu valor anterior. O estocástico lento (`SlowStochasticPeriod`, 3, 3) fornece a linha de sinal %D.
2. **Configuração longa**
   - O fechamento atual do M15 está abaixo do seu SMMA e o fechamento do H1 está abaixo do seu próprio SMMA.
   - A distância entre o M15 SMMA e o fechamento está dentro de `ThresholdPoints` etapas de preço.
   - Ambas as linhas estocásticas estão abaixo de 20. A linha rápida cruza acima da linha lenta durante a última vela (`fast` > `slow` enquanto o valor rápido anterior estava abaixo de `slow`).
   - Se existir uma posição curta, a estratégia primeiro compra volume suficiente para achatá-la e depois abre uma nova posição longa com `TradeVolume`.
3. **Configuração curta** reflete a lógica longa:
   - Ambos os fechamentos estão acima de seus SMMAs, a distância está dentro de `ThresholdPoints`, os valores estocásticos estão acima de 80 e o rápido
linha cruza abaixo da linha lenta. A estratégia vende, fechando uma posição comprada existente, se necessário.
4. **Gerenciamento de riscos**
   - Após cada entrada, as ordens de proteção são colocadas em `StopLossPoints` e `TakeProfitPoints` (convertidas em preço absoluto
distâncias usando a etapa de preço do instrumento).
   - Um trailing stop realinha a ordem de stop loss quando a negociação ganha pelo menos `TrailingStopPoints` pontos. A nova parada é
posicionado no fechamento atual menos/mais a distância de fuga para posições compradas/vendidas, respectivamente.
   - Quando a posição retornar à estabilidade, todas as ordens de proteção serão canceladas.

## Diferenças em relação ao original EA

- O SMMA de MetaTrader usa uma mudança de indicador de oito barras; Os indicadores StockSharp não expõem uma configuração de mudança direta. O porto
avalia o valor finalizado mais recente. Isso mantém o tempo de cruzamento, evitando buffers personalizados adicionais.
- O EA original usou as cotações de compra/venda de MQL para rastreamento. A porta usa o fechamento da vela finalizada que acionou o trailing
atualização, que é o análogo mais próximo disponível no API de alto nível.
- O gerenciamento de dinheiro depende dos ajudantes de registro de pedidos de StockSharp (`BuyMarket`, `SellMarket`, `SellStop`, etc.) em vez de
`OrderSend` e `OrderModify`.

## Parâmetros

| Grupo | Nome | Descrição | Padrão |
|-------|------|-------------|---------|
| Dados | `M15 Candle Type` | Tipo/período de vela utilizado para os principais cálculos. | Prazo M15 |
| Dados | `H1 Candle Type` | Tipo/prazo de vela usado para confirmação. | Período H1 |
| Indicadores | `M15 SMMA Period` | Comprimento da média móvel suavizada na série M15. | 200 |
| Indicadores | `H1 SMMA Period` | Comprimento da média móvel suavizada na série H1. | 200 |
| Indicadores | `Slow Stochastic Period` | Comprimento %K para o oscilador estocástico lento que fornece a linha %D. | 14 |
| Indicadores | `Fast Stochastic Period` | Comprimento %K para o oscilador estocástico rápido que fornece a linha %K principal. | 14 |
| Sinais | `Threshold (points)` | Distância máxima entre o SMMA M15 e o fechamento atual para permitir entradas. | 50 |
| Risco | `Take Profit (points)` | Distância de lucro expressa em etapas de preço. | 570 |
| Risco | `Stop Loss (points)` | Distância de stop-loss expressa em etapas de preço. | 30 |
| Risco | `Trailing Stop (points)` | Distância do trailing-stop expressa em etapas de preço. | 200 |
| Negociação | `Trade Volume` | Volume enviado com cada ordem de mercado. | 0,1 |

## Notas para uso

- Certifique-se de que o título negociado exponha `PriceStep`; caso contrário, as distâncias baseadas em pontos cairão para `1`, o que pode levar a grandes
ordens de proteção sobre instrumentos cotados em unidades fracionárias.
- A estratégia cancela e recria ordens de stop assim que um melhor nível de trailing for detectado. Corretores que não permitem
modificações podem exigir limitação.
- Como a porta opera apenas com velas finalizadas, o sistema foi projetado para backtests e execução de fim de barra. Executando
os dados do live tick requerem a correspondência das configurações de construção da vela entre o terminal e StockSharp.
