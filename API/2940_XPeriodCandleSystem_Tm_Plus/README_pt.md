# Estratégia XPeriod Candle System TM Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é um port StockSharp do consultor especialista MetaTrader `Exp_XPeriodCandleSystem_Tm_Plus`. O robô original baseia-se no indicador personalizado *XPeriod Candle System*, que suaviza dados de candles e colore as barras de acordo com rompimentos das Bandas de Bollinger. A versão traduzida reproduz este comportamento aplicando suavização exponencial às séries OHLC, mapeando os mesmos modos de preço aplicado e conduzindo operações a partir dos estados de cor resultantes. Uma saída baseada em tempo e ordens de proteção configuráveis complementam a lógica de rompimento.

## Lógica de negociação

1. **Candles suavizados** – Médias móveis exponenciais com comprimento configurável constroem valores sintéticos de abertura, máxima, mínima e fechamento que aproximam o indicador fonte.
2. **Preço aplicado** – O usuário pode selecionar qualquer uma das doze fórmulas de preço (fechamento, abertura, mediana, variações de seguidor de tendência, Demark, etc.) antes de alimentar os dados nas Bandas de Bollinger.
3. **Análise de bandas** – Um indicador de Bandas de Bollinger (comprimento e desvio configuráveis) processa a série de preços suavizados. Bandas finalizadas são necessárias antes que os sinais sejam avaliados.
4. **Estados de cor** –
   - Barra altista acima da banda superior → cor `0` (rompimento para cima).
   - Barra baixista abaixo da banda inferior → cor `4` (rompimento para baixo).
   - Outras barras altistas → cor `1`; outras barras baixistas → cor `3`.
   - Um deslocamento de rompimento configurável (convertido para unidades de preço usando o tamanho de tick do símbolo quando possível) evita falsos gatilhos.
5. **Entradas** – A estratégia analisa a vela definida por `SignalBar` e sua predecessora:
   - Abrir comprado quando a barra anterior foi um rompimento altista (`0`) e a barra de sinal não é.
   - Abrir vendido quando a barra anterior foi um rompimento baixista (`4`) e a barra de sinal não é.
6. **Saídas** –
   - Fechar comprados quando a barra de referência é baixista (`> 2`).
   - Fechar vendidos quando a barra de referência é altista (`< 2`).
   - Um temporizador de manutenção opcional (`TimeTrade` e `HoldingMinutes`) fecha posições após os minutos especificados.
7. **Risco** – `StartProtection` implanta distâncias absolutas opcionais de take-profit e stop-loss para cada operação.

## Parâmetros

| Parâmetro | Descrição | Valor padrão |
|-----------|-----------|--------------|
| `OrderVolume` | Tamanho base de ordem utilizado para entradas de mercado. | 0.1 |
| `BuyPosOpen` / `SellPosOpen` | Habilitar/desabilitar entradas compradas ou vendidas. | `true` |
| `BuyPosClose` / `SellPosClose` | Permitir saídas de posições compradas ou vendidas. | `true` |
| `TimeTrade` | Ativa o filtro de saída baseado em tempo. | `true` |
| `HoldingMinutes` | Tempo máximo de manutenção antes que o filtro de tempo feche uma posição. | 960 |
| `CandleType` | Tipo de dados de candle (período) solicitado do mercado. | 4 horas |
| `Period` | Comprimento das médias móveis exponenciais de suavização. | 5 |
| `BollingerLength` | Número de barras suavizadas dentro da janela de cálculo de Bollinger. | 20 |
| `BandsDeviation` | Multiplicador da largura de banda. | 1.001 |
| `AppliedPriceMode` | Transformação de preço usada antes do indicador Bollinger (fechamento, abertura, mediana, seguidor de tendência, Demark, etc.). | Close |
| `SignalBar` | Índice da barra usada para avaliação de sinais (1 = última barra fechada). | 1 |
| `StopLoss` / `TakeProfit` | Distâncias absolutas (em unidades de preço) usadas pelo motor de proteção. | 1000 / 2000 |
| `Deviation` | Deslocamento de rompimento extra adicionado acima/abaixo das Bandas de Bollinger. | 10 |

## Notas de uso

- O passo de suavização usa médias móveis exponenciais para replicar o cálculo proprietário do XPeriod. Períodos menores mantêm os candles sintéticos mais próximos dos preços de mercado, enquanto períodos maiores enfatizam a estrutura de tendência.
- `SignalBar` deve permanecer dentro do histórico armazenado (até 14 posições após a barra atual). Valores maiores que o histórico disponível ignorarão automaticamente o trading.
- O deslocamento de rompimento é multiplicado por `PriceStep` quando o ativo expõe um tamanho de tick. Isso mantém o comportamento similar à versão MetaTrader onde `Deviation` é definido em pontos.
- `StopLoss` e `TakeProfit` são especificados em unidades absolutas de preço. Defina-os como zero para desabilitar as ordens de proteção mantendo a infraestrutura de gestão ativa.
- Nenhuma tradução Python foi fornecida ainda; esta pasta contém apenas a implementação em C#.
