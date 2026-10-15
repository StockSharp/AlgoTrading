# Estratégia do Sistema Very Blonde
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de contra-tendência baseada em grade inspirada no "Very Blonde System" original para MetaTrader. A estratégia procura uma grande distância entre o preço atual e as extremidades recentes e opera na direção oposta.

## Lógica da estratégia
1. Calcular o máximo mais alto e o mínimo mais baixo nas últimas *Count Bars* velas.
2. Quando não há posições abertas:
   - Se a distância do máximo recente ao preço atual superar *Limit* ticks, comprar a mercado.
   - Se a distância do preço atual ao mínimo recente superar *Limit* ticks, vender a mercado.
   - Após entrar em uma posição, colocar quatro ordens limite adicionais a cada *Grid* ticks, dobrando o volume em cada nível.
3. Quando uma posição existe:
   - Se o lucro total superar *Amount* unidades de moeda, fechar a posição e cancelar todas as ordens pendentes.
   - Se *Lock Down* for maior que zero, uma vez que o preço se mova a favor por esse número de ticks, a estratégia ativa uma proteção de breakeven. Se o preço retornar ao nível de entrada, todas as posições são fechadas.

## Parâmetros
| Nome | Descrição |
|------|-----------|
| `CountBars` | Número de velas para buscar máximos e mínimos. |
| `Limit` | Distância mínima do extremo em ticks para abrir uma operação. |
| `Grid` | Distância em ticks entre ordens de grade adicionais. |
| `Amount` | Lucro alvo em moeda para fechar todas as posições. |
| `LockDown` | Distância em ticks para ativar a proteção de breakeven. |
| `CandleType` | Tipo de vela usado para cálculos. |

A estratégia usa ordens de mercado para entradas iniciais e ordens limite para os níveis da grade. Todos os comentários no código estão escritos em inglês.
