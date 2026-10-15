# Estratégia Exp KWAN NRP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Exp KWAN NRP reproduz o consultor especialista MetaTrader original combinando um oscilador estocástico, índice de força relativa e indicador de momentum em uma única razão. A razão é suavizada com uma média móvel configurável, e a inclinação da linha suavizada determina quando abrir ou fechar posições. A abordagem funciona em qualquer símbolo ou período e é projetada para negociação direcional quando o momentum muda.

## Lógica de trading
1. Construir a razão KWAN multiplicando a linha %D do estocástico pelo valor RSI e dividindo pela leitura do momentum.
2. Suavizar a razão com o método de média móvel selecionado (simples, exponencial, suavizada ou ponderada).
3. Avaliar a inclinação da linha suavizada no deslocamento da barra de sinal configurável.
4. Entrar em posições compradas quando a linha gira para cima e sair de posições vendidas. Entrar em posições vendidas quando a linha gira para baixo e sair de posições compradas.
5. A proteção opcional de stop-loss e take-profit pode fechar automaticamente posições após um movimento de preço predefinido medido em passos de preço.

## Sinais
- **Entrada comprada**: O valor KWAN suavizado na barra de sinal sobe em comparação com a barra anterior e as entradas compradas estão habilitadas.
- **Saída comprada**: O valor KWAN suavizado gira para baixo enquanto uma posição comprada está aberta e as saídas compradas estão habilitadas.
- **Entrada vendida**: O valor KWAN suavizado na barra de sinal cai em comparação com a barra anterior e as entradas vendidas estão habilitadas.
- **Saída vendida**: O valor KWAN suavizado gira para cima enquanto uma posição vendida está aberta e as saídas vendidas estão habilitadas.

## Gerenciamento de risco
- Defina a propriedade `Volume` da estratégia para controlar o tamanho base da ordem. A inversão de posição fecha automaticamente uma posição oposta antes de abrir uma nova.
- Habilite `UseProtection` para aplicar níveis de stop-loss e take-profit medidos em passos de preço do instrumento. Ambas as proteções podem ser usadas juntas ou separadamente.
- A estratégia assina candles definidos por `CandleType` e negocia ao fechamento de candles finalizados.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Período usado para cálculos de indicadores e avaliação de sinais. | Candles de 1 hora |
| `KPeriod` | Período da linha %K do estocástico. | 5 |
| `DPeriod` | Período da linha %D do estocástico. | 3 |
| `SlowingPeriod` | Suavização adicional aplicada à linha %K do estocástico. | 3 |
| `RsiPeriod` | Período do índice de força relativa. | 14 |
| `MomentumPeriod` | Período do indicador de momentum. | 14 |
| `SmoothingMethod` | Tipo de média móvel aplicada à razão KWAN (Simple, Exponential, Smoothed, Weighted). | Simple |
| `SmoothingLength` | Comprimento da média móvel de suavização. | 3 |
| `SignalBar` | Número de barras atrás usado para avaliar a inclinação (0 = barra fechada atual). | 1 |
| `EnableBuyEntries` | Permitir abrir posições compradas em sinais de alta. | true |
| `EnableSellEntries` | Permitir abrir posições vendidas em sinais de baixa. | true |
| `EnableBuyExits` | Permitir fechar posições compradas quando um sinal de baixa aparecer. | true |
| `EnableSellExits` | Permitir fechar posições vendidas quando um sinal de alta aparecer. | true |
| `UseProtection` | Habilitar proteções de stop-loss e take-profit. | true |
| `StopLossSteps` | Distância do stop-loss expressa em passos de preço. | 1000 |
| `TakeProfitSteps` | Distância do take-profit expressa em passos de preço. | 2000 |

## Notas de uso
- A razão KWAN pode se tornar instável quando o indicador de momentum é igual a zero. A estratégia pula automaticamente os sinais para essas barras para evitar divisão por zero.
- O parâmetro `SignalBar` permite alinhar sinais com barras históricas se confirmação atrasada for necessária.
- Combine com controles de risco no nível de corretagem ou filtros adicionais se necessário para negociação em produção.
