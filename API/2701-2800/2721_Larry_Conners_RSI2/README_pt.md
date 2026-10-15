# Estratégia Larry Connors RSI-2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um port fiel ao StockSharp do sistema clássico Larry Connors RSI-2. A estratégia combina um oscilador RSI rápido de 2 períodos com filtros de média móvel no período horário para capturar configurações de reversão à média de curto prazo, mantendo-se alinhada com a tendência do período superior. Os níveis opcionais de stop-loss e take-profit, expressos em pips, replicam as regras originais de gestão de capital do MetaTrader.

## Visão Geral do Conceito

- **Tipo**: Reversão à média com filtro de tendência.
- **Mercado**: Desenvolvida para pares Forex no gráfico H1.
- **Direção**: Opera comprado e vendido, mas apenas na direção do filtro de SMA lenta.
- **Indicadores Principais**: SMA de 5 períodos (momento de saída), SMA de 200 períodos (filtro de tendência), RSI de 2 períodos (gatilho de sinal).

## Regras de Trading

### Entradas Comprado
- O valor RSI cai abaixo de `RSI Long Entry` (padrão 6).
- O preço de fechamento da vela completada permanece acima da `Slow SMA` (padrão 200 períodos).
- Nenhuma posição aberta presente.

### Entradas Vendido
- O valor RSI sobe acima de `RSI Short Entry` (padrão 95).
- O preço de fechamento está abaixo da `Slow SMA`.
- Nenhuma posição aberta presente.

### Condições de Saída
- **Posições comprado** fecham quando o fechamento se move acima da `Fast SMA` (padrão 5). Os níveis opcionais de stop-loss e take-profit medidos em pips também podem fechar a operação se habilitados.
- **Posições vendido** fecham quando o fechamento se move abaixo da `Fast SMA`. Os níveis opcionais de stop-loss e take-profit em pips se aplicam simetricamente.

### Gestão de Risco
- `Use Stop Loss` alterna uma distância fixa de stop em pips relativa ao preço de entrada.
- `Use Take Profit` habilita um alvo de lucro simétrico em pips.
- As distâncias em pips são convertidas para preços absolutos via `PriceStep` do instrumento e precisão decimal, espelhando a lógica MT5 para cotações de 4/5 dígitos.

## Valores Padrão

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `Trade Volume` | 1 | Volume base de ordem para cada entrada. |
| `Fast SMA Period` | 5 | Média de temporização de saída. |
| `Slow SMA Period` | 200 | Filtro de direção de tendência. |
| `RSI Period` | 2 | Lookback para o oscilador RSI. |
| `RSI Long Entry` | 6 | Limiar de sobrevenda para operações comprado. |
| `RSI Short Entry` | 95 | Limiar de sobrecompra para operações vendido. |
| `Use Stop Loss` | true | Ativar/desativar stop protetor. |
| `Stop Loss (pips)` | 30 | Distância do stop-loss em pips. |
| `Use Take Profit` | true | Ativar/desativar alvo fixo de lucro. |
| `Take Profit (pips)` | 60 | Distância do alvo de lucro em pips. |
| `Candle Type` | 1 hora | Período das velas de trabalho. |

Todos os parâmetros ajustáveis expõem `.SetCanOptimize(true)` permitindo a otimização em lote dentro do Designer/Tester.

## Notas de Execução

- Os sinais são avaliados em velas fechadas para corresponder à implementação original do MetaTrader.
- Os níveis protetores são rastreados internamente, fechando toda a posição com ordens a mercado quando violados.
- A estratégia redefine o estado interno (`pipSize`, âncoras de entrada) em cada reinício para garantir backtests reproduzíveis.
- Adicione a estratégia a um projeto junto com dados Forex confiáveis para replicar os resultados de desempenho publicados.

## Uso Sugerido

1. Conecte um feed de dados Forex que forneça velas de 1 hora.
2. Adicione a estratégia ao Designer ou execute-a programaticamente através da StockSharp API.
3. Ajuste os parâmetros de risco baseados em pips para corresponder às especificações do contrato do broker, se necessário.
4. Opcionalmente otimize os limiares RSI ou os comprimentos das médias móveis para adaptar o modelo a outros símbolos.

Ao preservar a lógica exata de RSI e médias móveis, este port permite que usuários do MT5 avaliem a metodologia Larry Connors RSI-2 dentro do ecossistema StockSharp.
