# Estratégia Rollback System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão em C# do consultor especialista MetaTrader 5 **"Rollback system"**. Ela mantém a ideia original de
negociar bem no início de um novo dia de negociação, avaliando as últimas 24 velas horárias para detectar se o mercado entregou
um movimento prolongado que provavelmente retrocederá.

## Lógica de negociação

1. A estratégia funciona em um período horário (`CandleType`, padrão 1 hora).
2. Os sinais são avaliados apenas uma vez por dia quando o novo dia começa (`00:00` – `00:03`). O filtro ignora as sessões de segunda-feira e sexta-feira
   exatamente como a versão MQL.
3. Antes de abrir uma posição, o algoritmo garante que nenhuma outra negociação esteja ativa.
4. Para cada dia de negociação, os seguintes valores são calculados a partir das últimas 24 velas fechadas:
   - `Open_24_minus_Close_1` – distância entre o preço de abertura de 24 barras atrás e o último fechamento.
   - `Close_1_minus_Open_24` – distância inversa mostrando a variação líquida do dia.
   - `Close_1_minus_Lowest` – o quão longe o fechamento está da mínima mais baixa do dia.
   - `Highest_minus_Close_1` – o quão longe o fechamento está da máxima mais alta do dia.
5. Regras de entrada (expressas em unidades de preço convertidas dos parâmetros de pips):
   - **Comprado #1** – o dia anterior caiu (`Open_24_minus_Close_1` acima do limiar `ChannelOpenClosePips`) e o fechamento ainda
     está perto da mínima extrema (`Close_1_minus_Lowest` abaixo de `RollbackPips - ChannelRollbackPips`).
   - **Comprado #2** – o dia anterior subiu (`Close_1_minus_Open_24` acima do limiar do canal) mas o mercado fechou bem abaixo da
     máxima diária (`Highest_minus_Close_1` maior que `RollbackPips + ChannelRollbackPips`).
   - **Vendido #1** – o dia anterior subiu e o fechamento terminou perto da máxima diária (`Highest_minus_Close_1` abaixo de
     `RollbackPips - ChannelRollbackPips`).
   - **Vendido #2** – o dia anterior caiu e o fechamento se recuperou bem acima da mínima diária (`Close_1_minus_Lowest` acima de
     `RollbackPips + ChannelRollbackPips`).
6. As ordens são executadas com `BuyMarket`/`SellMarket` usando o volume de negociação configurado. Os níveis de stop-loss e take-profit são
   derivados de `StopLossPips` e `TakeProfitPips` (ambos zero desabilitam a proteção respectiva).
7. Os níveis de proteção são monitorados em cada vela finalizada. Se o preço violar um nível dentro da barra, a estratégia fecha a posição
   usando uma ordem de mercado, replicando o comportamento do consultor especialista MQL original que enviava stops duros.

## Conversão de pips a parâmetros

O MetaTrader 5 multiplica os valores de pip por 10 em símbolos de 3 e 5 dígitos. A lógica de conversão é preservada: a estratégia pega o
`PriceStep` do instrumento e aplica um multiplicador de dez vezes quando o número detectado de dígitos decimais é igual a 3 ou 5. Isso mantém os
limiares de entrada, as distâncias de stop-loss e take-profit consistentes com a implementação MQL em símbolos FX típicos.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `TradeVolume` | Tamanho da negociação usado para ordens de mercado. |
| `StopLossPips` | Distância de stop-loss em pips. Definir como zero para desabilitar. |
| `TakeProfitPips` | Distância de take-profit em pips. Definir como zero para desabilitar. |
| `RollbackPips` | Requisito de rollback base utilizado por todos os sinais. |
| `ChannelOpenClosePips` | Diferença mínima entre a abertura e o fechamento do dia anterior. |
| `ChannelRollbackPips` | Tolerância adicionada/subtraída da verificação de rollback. |
| `CandleType` | Tipo de vela de trabalho, padrão barras horárias. |

## Notas

- A versão MQL pintou retângulos no gráfico para referência visual. O port do StockSharp mantém apenas a lógica de negociação.
- A gestão de risco é implementada com monitoramento interno da estratégia em vez de ordens de proteção do lado do servidor porque a API de alto nível
  gerencia posições diretamente.
- Ao otimizar, ajuste os limiares de pip e o volume para se adequar ao instrumento alvo e ao tamanho de tick do broker.
