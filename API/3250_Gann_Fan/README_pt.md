# Estratégia de Gann Fan
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia StockSharp reproduz o especialista MetaTrader **GANN_FAN** usando a API de alto nível. Combina filtros de tendência de médias móveis ponderadas linearmente com confirmação de Momentum, uma porta de direção MACD e uma reconstrução baseada em fractais do Gann Fan para determinar o viés altista ou baixista. O gerenciamento de risco espelha o robô original com entradas empilhadas no estilo martingale, stops fixos, proteção de trailing e movimentos de ponto de equilíbrio opcionais.

## Lógica de trading

1. **Filtro de tendência** – Duas médias móveis ponderadas linealmente (LWMA) construídas sobre o preço típico (H+L+C)/3 definem a tendência rápida e lenta. Trades longos requerem que a LWMA rápida permaneça acima da LWMA lenta; trades curtos precisam do cruzamento inverso.
2. **Confirmação de Momentum** – A estratégia calcula o oscilador de Momentum clássico como `100 * Close / Close(n)` e avalia o desvio do nível neutro 100 ao longo dos últimos três candles fechados. Pelo menos um desvio deve exceder o limiar configurado para confirmar a força na direção do trade.
3. **Direção MACD** – Um sinal MACD configurável (períodos de EMA rápida, lenta e de sinal) deve concordar com a tendência. Entradas longas requerem que a linha MACD seja maior que a linha de sinal, enquanto os curtos precisam que a linha MACD permaneça abaixo da linha de sinal.
4. **Orientação do Gann Fan** – Fractais confirmados de Bill Williams reconstroem os raios do Gann Fan altista e baixista. Os dois fractais descendentes mais recentes formam o raio altista; sua inclinação deve ser positiva para permitir posições longas. Os dois últimos fractais ascendentes definem o raio baixista; sua inclinação deve ser negativa para autorizar vendas a descoberto.
5. **Empilhamento de posições** – Quando um novo sinal chega, a estratégia pode adicionar a uma posição existente até o máximo configurado. Cada ordem adicional aumenta o volume multiplicando o lote base pelo expoente de lote, emulando o dimensionamento martingale usado na versão MQL.

## Gestão de risco

- **Stop-loss e take-profit fixos** – Expressos em passos de preço do instrumento, convertidos automaticamente pela estratégia usando `Security.PriceStep`.
- **Controle de ponto de equilíbrio** – Quando habilitado, assim que o lucro alcança a distância de acionamento, o stop é avançado para a entrada mais/menos o offset configurado.
- **Trailing stop** – Ativa após atingir a distância de acionamento. O stop pode seguir o mercado por uma distância fixa do fechamento ou bloqueando o valor mais baixo (para longos) / mais alto (para curtos) dos candles mais recentes mais um fator de preenchimento.
- **Interruptor de saída forçada** – Definir `Force Exit` como `true` liquida imediatamente qualquer exposição aberta no próximo candle terminado.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| **Volume** | Tamanho base da ordem usado para a primeira entrada. |
| **Fast LWMA / Slow LWMA** | Períodos das médias móveis ponderadas linearmente para o filtro de tendência. |
| **Momentum Period / Threshold** | Retrospectiva do cálculo de Momentum e desvio mínimo de 100 necessário para operar. |
| **MACD Fast / Slow / Signal** | Períodos de EMA para o filtro de confirmação MACD. |
| **Fractal History** | Número máximo de pontos de fractal confirmados armazenados para construir os raios do Gann Fan. |
| **Max Trades** | Número máximo de entradas empilhadas permitidas em uma única direção. |
| **Lot Exponent** | Multiplicador aplicado ao volume base para cada entrada adicional. |
| **Stop Loss / Take Profit** | Distâncias de proteção em passos de preço. |
| **Enable Trailing** | Habilita o gerenciamento do trailing stop. |
| **Trail Trigger / Distance / Padding** | Acionador de lucro, distância de trailing e preenchimento extra (em passos de preço) usado ao fazer trailing via extremos de candle. |
| **Use Candle Trail** | Habilita trailing baseado em candles além do trailing de distância fixa. |
| **Trailing Candles** | Número de candles terminados recentes considerados ao calcular os níveis de trailing baseados em candles. |
| **Enable Break-even** | Liga ou desliga a lógica de ponto de equilíbrio. |
| **Break-even Trigger / Offset** | Acionador de lucro e offset (em passos de preço) para mover o stop para o ponto de equilíbrio. |
| **Use Gann Filter** | Impõe a orientação altista/baixista do Gann Fan para entradas. |
| **Force Exit** | Força a estratégia a fechar todas as posições na próxima barra. |
| **Candle Type** | Série de candles usada para cálculos e geração de ordens. |

## Notas

- Todos os cálculos de indicadores funcionam exclusivamente em candles terminados fornecidos por `SubscribeCandles` e `Bind`, conforme as melhores práticas da API de alto nível do StockSharp.
- As distâncias de trailing e ponto de equilíbrio se adaptam automaticamente ao tamanho do tick do instrumento. Quando `PriceStep` não está disponível, os recursos de proteção permanecem inativos até que o conector o forneça.
- A estratégia mantém estados separados para posições longas e curtas, garantindo que os níveis de trailing e ponto de equilíbrio sejam redefinidos quando a exposição muda de direção.
- Para imitar de perto o especialista MetaTrader, alertas, notificações e objetos de gráfico explícitos do código original são substituídos pela reconstrução nativa do Gann Fan do StockSharp usando fractais.
