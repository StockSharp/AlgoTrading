# Estratégia Eliot Wave (portada do MQL4 "Eliot Wave I")
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **estratégia Eliot Wave** é uma versão para a API do StockSharp do expert advisor original do MetaTrader 4 "Eliot Wave I". O sistema combina um cruzamento rápido/lento de média móvel ponderada linear (LWMA) com confirmação de momentum multitemporal e um filtro MACD muito lento. O objetivo é identificar movimentos impulsivos na direção da tendência predominante, mantendo o risco limitado por regras de proteção incorporadas.

## Indicadores centrais

- **LWMA rápida (padrão 6)** - acompanha a direção de curto prazo usando o preço típico `(High + Low + Close) / 3`.
- **LWMA lenta (padrão 85)** - mede a tendência mais ampla no mesmo período.
- **Momentum (período padrão 14)** - avaliado em um período mais alto e convertido em desvio relativo ao nível neutro `100`. Uma leitura acima do limite configurado indica um impulso suficientemente forte.
- **MACD (12, 26, 9)** - calculado em um período muito lento (mensal por padrão) e usado como filtro de longo prazo. A estratégia só compra quando a linha principal MACD está acima da linha de sinal e vende quando está abaixo.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | --------- | ------ |
| `Base Candle` | Período primário para processamento da LWMA. | Candles de 15 minutos |
| `Momentum Candle` | Período mais alto usado para confirmação de momentum. | Candles de 1 hora |
| `MACD Candle` | Período muito lento para o filtro de tendência MACD. | Candles de 30 dias |
| `Fast LWMA` | Comprimento da média móvel ponderada linear rápida. | 6 |
| `Slow LWMA` | Comprimento da média móvel ponderada linear lenta. | 85 |
| `Momentum Period` | Retrospectiva do indicador de momentum no período de confirmação. | 14 |
| `Momentum Buy Threshold` | Desvio mínimo acima de 100 necessário para validar uma configuração comprada. | 0.3 |
| `Momentum Sell Threshold` | Desvio mínimo acima de 100 necessário para validar uma configuração vendida. | 0.3 |
| `Stop Loss (pts)` | Distância do stop de proteção expressa em pontos do instrumento. | 20 |
| `Take Profit (pts)` | Distância do alvo expressa em pontos do instrumento. | 50 |
| `Trade Volume` | Tamanho da ordem para cada entrada. | 1 lote |
| `Max Position` | Exposição líquida absoluta permitida; impede que a estratégia ultrapasse o limite `Max_Trades` do EA MQL. | 10 lotes |

Todos os parâmetros são implementados como `StrategyParam<T>`, para que possam ser otimizados diretamente no Designer ou Runner.

## Regras de negociação

1. **Filtro de tendência e estrutura**
   - A LWMA rápida deve permanecer acima da LWMA lenta para considerar operações compradas.
   - A LWMA rápida deve permanecer abaixo da LWMA lenta para considerar vendidas.
   - Os dois últimos candles concluídos devem se sobrepor (`Low[2] < High[1]` para compras, `Low[1] < High[2]` para vendas), replicando o requisito de consolidação do EA.
2. **Confirmação de momentum**
   - O momentum do período mais alto é transformado em valores `abs(momentum - 100)`.
   - Se qualquer um dos três últimos valores exceder o limite configurado, o impulso é considerado válido.
3. **Filtro de tendência macro**
   - Compras exigem que a linha principal MACD esteja acima da linha de sinal no período lento.
   - Vendas exigem que a linha principal MACD esteja abaixo da linha de sinal.
4. **Execução de ordens**
   - Quando todas as condições se alinham, a estratégia envia uma ordem a mercado dimensionada para inverter a posição atual e adicionar o volume de negociação configurado.
   - Viradas de posição são suportadas para que o comportamento corresponda à lógica de preço médio do EA original.

## Gestão de risco

- `StartProtection` aplica automaticamente distâncias de stop-loss e take-profit em pontos do instrumento.
- Lógica adicional de saída fecha posições compradas quando a LWMA rápida cai abaixo da LWMA lenta ou quando o filtro MACD fica baixista (e vice-versa para vendidas). Isso espelha os blocos de saída MQL.
- O parâmetro `Max Position` impede que a estratégia acumule exposição além do limite configurado, respeitando a restrição `Max_Trades` do EA.

## Diferenças em relação ao EA original

- Verificações gráficas de linhas de tendência e notificações manuais de operação foram removidas porque são específicas do MetaTrader e não têm equivalente no StockSharp.
- Variantes de break-even e trailing-stop complexo do script MQL são substituídas pelo mecanismo mais simples `StartProtection`. Usuários podem estender a estratégia se esses comportamentos forem necessários.
- Proteção de equity baseada em dinheiro não é implementada; o risco é controlado por stops fixos e pelo limite de posição.

## Notas de uso

1. Anexe a estratégia a um instrumento líquido e garanta que os três fluxos de candles estejam disponíveis.
2. Ajuste `Trade Volume`, distâncias de stop/alvo e limites de acordo com a volatilidade do mercado negociado.
3. Otimize limites separadamente para impulsos altistas e baixistas se o instrumento apresentar comportamento assimétrico.
4. Considere habilitar os visuais de gráfico incorporados (candles, LWMAs, marcadores de operação) para facilitar a depuração.

Esta versão foca em reproduzir a lógica de sinais do EA original usando a API de alto nível do StockSharp, mantendo a implementação idiomática e fácil de manter.
