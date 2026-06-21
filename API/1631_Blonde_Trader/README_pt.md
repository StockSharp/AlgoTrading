# Estratégia Blonde Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Blonde Trader é uma estratégia de trading em grade convertida do MQL. Ela busca movimentos de preço afastando-se de extremos recentes e abre posições com uma grade de ordens pendentes.

## Conceito

- Calcular o máximo mais alto e o mínimo mais baixo nas últimas **Period X** velas.
- Se o preço atual estiver abaixo da máxima recente por mais de **Limit** ticks, abrir uma posição comprada e colocar uma série de ordens buy limit formando uma grade.
- Se o preço atual estiver acima da mínima recente por mais de **Limit** ticks, abrir uma posição vendida e colocar uma série de ordens sell limit formando uma grade.
- Fechar todas as posições quando o lucro acumulado atingir **Amount**.
- Opcionalmente, após o preço mover **LockDown** ticks em lucro, uma ordem stop é colocada no nível de equilíbrio para proteger a posição.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `PeriodX` | Período de retrocesso para a máxima mais alta e a mínima mais baixa. |
| `Limit` | Distância mínima em ticks do preço atual até um extremo. |
| `Grid` | Passo em ticks entre as ordens pendentes da grade. |
| `Amount` | Meta de lucro na moeda da conta. |
| `LockDown` | Distância em ticks para mover o stop ao ponto de equilíbrio. |
| `CandleType` | Tipo de velas usadas para análise. |

## Indicadores

- `Highest` – rastreia a máxima mais alta durante o período de retrocesso.
- `Lowest` – rastreia a mínima mais baixa durante o período de retrocesso.

## Lógica de Ordens

1. Quando um setup comprado aparece:
   - Comprar a mercado com o volume padrão da estratégia.
   - Colocar quatro ordens buy limit adicionais abaixo da entrada, cada uma separada por **Grid** ticks e dobrando o volume.
2. Quando um setup vendido aparece:
   - Vender a mercado com o volume padrão da estratégia.
   - Colocar quatro ordens sell limit adicionais acima da entrada com as mesmas regras de grade e duplicação de volume.
3. Se `PnL` atingir **Amount**, todas as posições abertas e ordens pendentes são fechadas.
4. Se `LockDown` for maior que zero e o preço tiver movido o número especificado de ticks a favor da posição, uma ordem stop protetora é colocada um tick além do preço de entrada.

## Notas

Esta estratégia demonstra a lógica básica de trading em grade. Usa apenas recursos de API de alto nível: `SubscribeCandles`, vinculação de indicadores e auxiliares de ordens simples como `BuyMarket`, `SellLimit` e `SellStop`.
