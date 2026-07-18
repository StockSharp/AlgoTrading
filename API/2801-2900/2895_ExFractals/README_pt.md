# Estratégia ExFractals
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A estratégia ExFractals é um sistema de rompimento que combina níveis de fractal ao estilo Williams com o filtro de momentum do corpo médio ExVol. O algoritmo monitora continuamente os máximos e mínimos fractais confirmados mais recentes, os promedia em pares e abre operações quando o preço fecha além desses níveis promediados enquanto a leitura ExVol confirma a direção do movimento.

## Lógica de Trading

1. **Detecção de fractais**
   - As velas são processadas quando fecham.
   - Fractais ascendentes (de baixa) e descendentes (de alta) são detectados quando a vela central em uma janela de cinco velas é um extremo estrito comparado com seus vizinhos.
   - A estratégia armazena os dois fractais confirmados mais recentes por lado junto com seus timestamps.
   - Cada lado produz um nível acionável igual à média dos últimos dois preços de fractal. Timestamps duplicados são ignorados para evitar usar o mesmo fractal duas vezes.
2. **Filtro ExVol**
   - O valor ExVol é igual à média simples do corpo da vela (fechamento menos abertura) expresso em passos de preço durante o período de lookback selecionado.
   - Um ExVol negativo indica velas de alta persistentes (fechamento positivo em relação à abertura), e um ExVol positivo indica velas de baixa persistentes.
3. **Condições de entrada**
   - **Comprado:** o último fechamento está acima do nível fractal superior promediado e ExVol é negativo. Qualquer posição vendida ativa é fechada e uma nova posição comprada é aberta.
   - **Vendido:** o último fechamento está abaixo do nível fractal inferior promediado e ExVol é positivo. Qualquer posição comprada ativa é fechada e uma nova posição vendida é aberta.
4. **Regras de risco e saída**
   - Alvos fixos de stop-loss e take-profit são colocados a distâncias configuráveis de pips do preço de entrada.
   - Trailing stops opcionais se movem apenas depois que a operação ganhar pelo menos `trailing stop + trailing step` pips. O stop é puxado para cima/baixo para manter uma distância de trailing constante enquanto respeita o passo mínimo de trailing.
   - Se o preço atingir o stop-loss ou take-profit, a posição inteira é fechada.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | --------- | ------ |
| `Candle Type` | Tipo/período de dados de vela usado pela estratégia. | Período de 1 hora |
| `ExVol Period` | Número de velas fechadas usadas para calcular a média do corpo da vela (ExVol). | 15 |
| `Stop Loss` | Distância de stop-loss em pips do preço de entrada. Definir como `0` para desabilitar. | 40 |
| `Take Profit` | Distância de take-profit em pips do preço de entrada. Definir como `0` para desabilitar. | 100 |
| `Trailing Stop` | Distância de trailing stop em pips. Definir como `0` para desabilitar o trailing. | 30 |
| `Trailing Step` | Movimento de preço adicional (em pips) necessário antes de mover o trailing stop. Deve ser positivo quando o trailing está habilitado. | 5 |
| `Volume` | Volume de ordem padrão herdado da classe base `Strategy`. | 1 |

## Notas Adicionais

- A lógica de trailing espelha a implementação MetaTrader: o stop não é ajustado até que a posição esteja pelo menos `TrailingStop + TrailingStep` pips no lucro.
- Os cálculos ExVol dependem do `PriceStep` do instrumento; se o passo não estiver disponível, um valor padrão de 0.0001 é usado.
- A estratégia emite ordens a mercado via `BuyMarket` e `SellMarket`, revertendo automaticamente qualquer posição existente antes de abrir uma nova.
- Garantir que o feed de dados forneça velas históricas suficientes para formar os pares iniciais de fractais (pelo menos cinco velas fechadas).
